/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.Cache.Redis
{
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Net;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Dictionary;
    using Extensions;
    using Interfaces;
    using log4net;
    using MessagePack;
    using ResilientRedisConnection;
    using Serialization;
    using Serialization.DictionaryNoBoxing.MessagePack;
    using StackExchange.Redis;
    using TaskCancellation.TaskHelper;
    using LocalCacheInstanceKey=Models.LocalCacheInstanceKey;

    public class CachePayloadRepository : ICachePayloadRepository
    {
        private const int HotKeysBatchSize = 100;
        private const int JournalBatchSize = 400;
        private readonly bool activationRuleIdempotency;
        private readonly ConnectionMultiplexer connectionMultiplexer;
        private readonly bool fill;
        private readonly bool localCache;
        private readonly long localCacheBytes;
        private readonly ILog log;
        private readonly TimeSpan maxLruAge;
        private readonly MessagePackSerializerOptions messagePackSerializerOptions;
        private readonly string postgresConnectionString;
        private readonly bool publishSubscribe;
        private readonly IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase;
        private readonly bool storePayloadCountsAndBytes;
        private readonly SemaphoreSlim timerSemaphore = new SemaphoreSlim(1, 1);
        // ReSharper disable once UnusedMember.Global
        public Task FillTask;
        private LocalCacheInstance localCacheInstance;
        private string localCacheInstanceGuidString;
        private ConcurrentDictionary<string, LocalCacheInstanceKey> localCacheInstanceKeys;
        private LruCacheConcurrentSizedDictionary<string, byte[]> lruCacheConcurrentSizedDictionary;
        private Timer timer;

        private CachePayloadRepository(ConnectionMultiplexer connectionMultiplexer, IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
            string postgresConnectionString, ILog log,
            bool fill, bool localCache, long localCacheBytes, bool messagePackCompression, bool storePayloadCountsAndBytes,
            bool publishSubscribe, TimeSpan maxLruAge, bool activationRuleIdempotency, CancellationToken token = default)
        {
            this.connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
            this.resilientRedisResilientRedisDatabase = resilientRedisResilientRedisDatabase ?? throw new ArgumentNullException(nameof(resilientRedisResilientRedisDatabase));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.fill = fill;
            this.localCache = localCache;
            this.localCacheBytes = localCacheBytes;
            this.maxLruAge = maxLruAge;
            this.activationRuleIdempotency = activationRuleIdempotency;
            this.storePayloadCountsAndBytes = storePayloadCountsAndBytes;
            this.publishSubscribe = publishSubscribe;
            this.postgresConnectionString = postgresConnectionString;

            messagePackSerializerOptions = MessagePackSerializerOptionsHelper.EnveloperMessagePackSerializerWithCompressionOptions(messagePackCompression);

            InstantiateLruCacheConcurrentSizedDictionary();
            SubscribeToRedisHashEvents();
            InstantiateLocalCacheInstanceCountersTimer(token);
        }

        public async Task InsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            DictionaryNoBoxing<int> payload,
            DateTime referenceDate,
            Guid entityAnalysisModelInstanceEntryGuid)
        {
            try
            {
                var keyPayload = $"Payload:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
                var localCacheForPayloadKey = GetLocalCacheEntry(keyPayload);

                var ms = new MemoryStream();

                var dictionaryNoBoxingWrapper = new EnvelopeDictionaryNoBoxing<int>
                {
                    Version = 1,
                    Data = payload
                };

                await MessagePackSerializer.SerializeAsync(ms, dictionaryNoBoxingWrapper,
                    messagePackSerializerOptions).ConfigureAwait(false);

                var hSetKey = $"{entityAnalysisModelInstanceEntryGuid:N}";
                var keyReferenceDate = $"ReferenceDate:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
                var sortedSet = $"{entityAnalysisModelInstanceEntryGuid:N}";

                var bytes = ms.ToArray();
                var tasks = new List<Task>
                {
                    resilientRedisResilientRedisDatabase.HashSetAsync(keyPayload, hSetKey, bytes),
                    resilientRedisResilientRedisDatabase.SortedSetAddAsync(keyReferenceDate, sortedSet, referenceDate.ToUnixTimeMilliSeconds())
                };

                if (publishSubscribe)
                {
                    tasks.Add(resilientRedisResilientRedisDatabase.PublishAsync(
                        RedisChannel.Pattern($"HashSet:{Dns.GetHostName()}:{localCacheInstanceGuidString}:{keyPayload}:{hSetKey}"),
                        bytes));
                }

                if (storePayloadCountsAndBytes)
                {
                    var redisKeyPayloadCount = $"PayloadCount:{tenantRegistryId}";
                    var redisKeyPayloadBytes = $"PayloadBytes:{tenantRegistryId}";
                    tasks.Add(resilientRedisResilientRedisDatabase.HashIncrementAsync(redisKeyPayloadCount, entityAnalysisModelGuid.ToString("N"), 1));
                    tasks.Add(resilientRedisResilientRedisDatabase.HashIncrementAsync(redisKeyPayloadBytes, entityAnalysisModelGuid.ToString("N"), bytes.Length));
                }

                AddToLruCacheConcurrentSizedDictionaryForLocalCacheInstanceKey(localCacheForPayloadKey, hSetKey, bytes);

                await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }

        public async Task UpsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            DictionaryNoBoxing<int> payload,
            DateTime referenceDate,
            Guid entityAnalysisModelInstanceEntryGuid)
        {
            try
            {
                await InsertAsync(tenantRegistryId, entityAnalysisModelGuid, payload, referenceDate,
                    entityAnalysisModelInstanceEntryGuid).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (log.IsInfoEnabled)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }
            }
        }

        public async Task DeleteByReferenceDatePreferReplicaAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            DateTime referenceDate, int limit, CancellationToken token = default)
        {
            var referenceDateTimestampThreshold = referenceDate.ToUnixTimeMilliSeconds();
            var redisKeyReferenceDate = $"ReferenceDate:{tenantRegistryId}:{entityAnalysisModelGuid:N}";

            while (!token.IsCancellationRequested)
            {
#pragma warning disable CA2016
                var expiredSortedSetEntries = (await resilientRedisResilientRedisDatabase.SortedSetRangeByScoreWithScoresAsync(
                    redisKeyReferenceDate,
                    Int64.MinValue,
                    referenceDateTimestampThreshold,
                    Exclude.Stop,
                    skip: 0,
                    take: limit,
                    flags: CommandFlags.PreferReplica
                ).ConfigureAwait(false)).ToList();

                if (expiredSortedSetEntries.Count == 0)
                {
                    return;
                }

                var tasks = new List<Task<TimedTaskResult>>();
                var sortedSetExpiredCount = expiredSortedSetEntries.Count;
                var expiredSortedSetMinTimestamp = (long)expiredSortedSetEntries.FirstOrDefault().Score;
                var expiredSortedSetMaxTimestamp = (long)expiredSortedSetEntries.LastOrDefault().Score;

                var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(postgresConnectionString, log);
                try
                {
                    var cachePayloadRemovalBatchRepository = new CachePayloadRemovalBatchRepository(dbContext);

                    // ReSharper disable once MethodSupportsCancellation
                    var cachePayloadRemovalBatch = await InsertCachePayloadRemovalBatchAsync(cachePayloadRemovalBatchRepository,
                        entityAnalysisModelGuid, referenceDate,
                        sortedSetExpiredCount,
                        expiredSortedSetMinTimestamp.FromUnixTimeMilliSeconds(),
                        expiredSortedSetMaxTimestamp.FromUnixTimeMilliSeconds());

                    var redisValuesToDelete = new List<RedisValue>();
                    var bulkInsertEntries = new List<CachePayloadRemovalBatchEntry>();

                    foreach (var expiredSortedSetEntry in expiredSortedSetEntries.TakeWhile(_ => !token.IsCancellationRequested))
                    {
                        redisValuesToDelete.Add(new RedisValue(expiredSortedSetEntry.Element.ToString()));
                        bulkInsertEntries.Add(new CachePayloadRemovalBatchEntry
                        {
                            CachePayloadRemovalBatchId = cachePayloadRemovalBatch.Id,
                            EntityAnalysisModelGuid = Guid.Parse(expiredSortedSetEntry.Element.ToString()),
                            ReferenceDate = ((long)expiredSortedSetEntry.Score).FromUnixTimeMilliSeconds()
                        });

                        await AppendDeletionTasksAsync(tasks, tenantRegistryId, entityAnalysisModelGuid, expiredSortedSetEntry).ConfigureAwait(false);
                    }

                    if (redisValuesToDelete.Count <= 0)
                    {
                        continue;
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);

                    var cachePayloadRemovalBatchEntryRepository = new CachePayloadRemovalBatchEntryRepository(dbContext);
                    // ReSharper disable once MethodSupportsCancellation
                    await TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.AppendBulkCleanupOfPayloadGuids, async () => await AppendBulkCleanupOfPayloadGuidsAsync(tasks, tenantRegistryId, entityAnalysisModelGuid, redisKeyReferenceDate, redisValuesToDelete));

                    // ReSharper disable once MethodSupportsCancellation
                    await TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.BulkInsertCachePayloadRemovalBatchEntry, async () => await cachePayloadRemovalBatchEntryRepository.BulkCopyAsync(bulkInsertEntries));

                    // ReSharper disable once MethodSupportsCancellation
                    if (activationRuleIdempotency)
                    {
                        await TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.BulkTtlCounterIdempotencyRemovalBatchEntry, async () => await CleanupIdempotencyAsync(tenantRegistryId, entityAnalysisModelGuid, redisValuesToDelete));
                    }

                    var completedTasks = await Task.WhenAll(tasks).ConfigureAwait(false);
                    var cachePayloadRemovalBatchResponseTimeRepository = new CachePayloadRemovalBatchResponseTimeRepository(dbContext);
                    // ReSharper disable once MethodSupportsCancellation
                    await cachePayloadRemovalBatchResponseTimeRepository.BulkCopyAsync(AggregateResponseTimesForBulkInsert(completedTasks, cachePayloadRemovalBatch));
                    // ReSharper disable once MethodSupportsCancellation
                    await UpdateCachePayloadRemovalBatchAsync(cachePayloadRemovalBatchRepository, cachePayloadRemovalBatch.Id);
    #pragma warning restore CA2016
                }
                catch (Exception ex)
                {
                    log.Error($"DeleteByReferenceDateAsync has created an error {ex}");
                }
                finally
                {
                    await dbContext.CloseAsync();
                    await dbContext.DisposeAsync();
                }
            }
        }

        public async Task<List<RedisValue>> GetSortedSetKeysAsync(
            int tenantRegistryId,
            Guid entityAnalysisModelGuid,
            string key,
            string value,
            int limit,
            Guid excludeGuid)
        {
            var redisKey = $"Journal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{key}:{value}";
            var lruJournalKey = $"LruJournal:{tenantRegistryId}:{entityAnalysisModelGuid:N}";

            await resilientRedisResilientRedisDatabase.SortedSetAddAsync(
                lruJournalKey, redisKey, DateTime.UtcNow.ToUnixTimeMilliSeconds());

            var sortedSetEntries =
                (await resilientRedisResilientRedisDatabase
                    .SortedSetRangeByRankWithScoresAsync(redisKey, 0, limit, Order.Descending)
                    .ConfigureAwait(false))
                .Reverse().ToList();

            return sortedSetEntries
                .Where(entry => !entry.Element.IsNull &&
                                entry.Element.ToString() != excludeGuid.ToString("N"))
                .Select(entry => entry.Element)
                .ToList();
        }

        public async Task<Dictionary<string, DictionaryNoBoxing<int>>> GetPayloadBatchAsync(
            int tenantRegistryId,
            Guid entityAnalysisModelGuid,
            IReadOnlyCollection<RedisValue> keys,
            Guid entityAnalysisModelInstanceEntryGuid)
        {
            var keyPayload = $"Payload:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
            var localCacheForPayloadKey = GetLocalCacheEntry(keyPayload);
            var result = new Dictionary<string, DictionaryNoBoxing<int>>(keys.Count);
            var missedKeys = new List<RedisValue>();
            var swUnpack = new Stopwatch();

            foreach (var key in keys)
            {
                var keyStr = key.ToString();
                Interlocked.Increment(ref localCacheForPayloadKey.Requests);

                if (localCache &&
                    localCacheForPayloadKey.LruCacheConcurrentSizedDictionary
                        .TryGetValue(keyStr, out var cached) && cached != null)
                {
                    swUnpack.Restart();
                    result[keyStr] = Unpack(cached);
                    swUnpack.Stop();
                    Interlocked.Add(ref localCacheForPayloadKey.UnpackResponseTime,
                        (int)(swUnpack.ElapsedTicks * 1000000 / Stopwatch.Frequency));
                    continue;
                }

                Interlocked.Increment(ref localCacheForPayloadKey.Misses);
                missedKeys.Add(key);
            }

            if (missedKeys.Count <= 0)
            {
                return result;
            }

            var swRedis = Stopwatch.StartNew();
            var fetched = await resilientRedisResilientRedisDatabase
                .HashGetAsync(keyPayload, missedKeys.ToArray())
                .ConfigureAwait(false);
            swRedis.Stop();

            if (log.IsInfoEnabled)
            {
                var totalBytes = fetched
                    .Select(v => (byte[])v)
                    .Where(b => b != null)
                    .Sum(b => b.Length);

                log.Info($"Batch payload fetch: GUID {entityAnalysisModelInstanceEntryGuid} fetched {fetched.Length} keys " +
                         $"in {swRedis.ElapsedMilliseconds}ms, totalBytes={totalBytes}.");
            }

            var unpackTime = 0L;
            for (var i = 0; i < missedKeys.Count; i++)
            {
                if (!fetched[i].HasValue)
                {
                    Interlocked.Increment(ref localCacheForPayloadKey.DualMiss);
                    continue;
                }

                swUnpack.Restart();
                var unpacked = Unpack(fetched[i]);
                swUnpack.Stop();

                result[missedKeys[i].ToString()] = unpacked;
                unpackTime += swUnpack.ElapsedTicks * 1000000 / Stopwatch.Frequency;
            }

            Interlocked.Add(ref localCacheForPayloadKey.UnpackResponseTime, (int)unpackTime);

            return result;
        }

        private static List<CachePayloadRemovalBatchResponseTime> AggregateResponseTimesForBulkInsert(TimedTaskResult[] tasks, CachePayloadRemovalBatch cachePayloadRemovalBatch)
        {
            var groupByComputeTime = tasks.GroupBy(g => g.TaskType).Select(s => new CachePayloadRemovalBatchResponseTime
            {
                TaskTypeId = (int)s.Key,
                ResponseTime = s.Sum(a => a.ComputeTime),
                CachePayloadRemovalBatchId = cachePayloadRemovalBatch.Id
            }).ToList();
            return groupByComputeTime;
        }

        private static Task UpdateCachePayloadRemovalBatchAsync(CachePayloadRemovalBatchRepository cachePayloadRemovalBatchRepository,
            long cachePayloadRemovalBatchId,
            CancellationToken token = default)
        {
            return cachePayloadRemovalBatchRepository.FinishAsync(cachePayloadRemovalBatchId, token);
        }

        private Task<CachePayloadRemovalBatch> InsertCachePayloadRemovalBatchAsync(CachePayloadRemovalBatchRepository cachePayloadRemovalBatchRepository,
            Guid entityAnalysisModelGuid,
            DateTime referenceDate,
            int expiredSortedSetCount,
            DateTime firstExpiredSortedSetReferenceDate,
            DateTime lastExpiredSortedSetReferenceDate,
            CancellationToken token = default)
        {
            var cachePayloadRemovalBatch = new CachePayloadRemovalBatch
            {
                EntityAnalysisModelGuid = entityAnalysisModelGuid,
                ReferenceDate = referenceDate,
                ExpiredSortedSetCount = expiredSortedSetCount,
                FirstExpiredSortedSetReferenceDate = firstExpiredSortedSetReferenceDate,
                LastExpiredSortedSetReferenceDate = lastExpiredSortedSetReferenceDate
            };

            return cachePayloadRemovalBatchRepository.InsertAsync(cachePayloadRemovalBatch, token);
        }

        public static async Task<CachePayloadRepository> CreateAsync(
            ConnectionMultiplexer connectionMultiplexer,
            IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
            string postgresConnectionString,
            ILog log,
            bool localCacheFill,
            bool localCache,
            long localCacheBytes,
            bool messagePackCompression,
            bool storePayloadCountsAndBytes,
            bool publishSubscribe,
            TimeSpan maxLruAge,
            bool activationRuleIdempotency,
            CancellationToken token = default)
        {
            var repository = new CachePayloadRepository(connectionMultiplexer, resilientRedisResilientRedisDatabase,
                postgresConnectionString, log, localCacheFill, localCache, localCacheBytes,
                messagePackCompression, storePayloadCountsAndBytes, publishSubscribe, maxLruAge, activationRuleIdempotency);
            await repository.FullyInitializeAsync(token).ConfigureAwait(false);

            return repository;
        }

        private async Task FullyInitializeAsync(CancellationToken token = default)
        {
            await CreateLocalCacheInstanceAsync(token);

            if (fill && localCache)
            {
                await Task.Delay(Random.Shared.Next(0, 5000), token);
                FillTask = FillPreferReplicaAsync(maxLruAge, token);
            }
        }

        private void InstantiateLocalCacheInstanceCountersTimer(CancellationToken token = default)
        {
            timer = new Timer(OnTimerElapsed, token, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            if (token.CanBeCanceled)
            {
                token.Register(() =>
                {
                    try
                    {
                        timer?.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        // ignore
                    }
                });
            }
        }

        private void OnTimerElapsed(object state)
        {
            var token = state is CancellationToken t ? t : CancellationToken.None;

            _ = Task.Run(() => ExecuteTimerWorkAsync(token), token)
                .ContinueWith(c =>
                    {
                        if (c.Exception != null && log.IsInfoEnabled)
                        {
                            log.Info($"Local cache timer faulted: {c.Exception}");
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default);
        }

        private async Task ExecuteTimerWorkAsync(CancellationToken token = default)
        {
            if (!await timerSemaphore.WaitAsync(0, token).ConfigureAwait(false))
            {
                return;
            }

            try
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(postgresConnectionString, log);

                try
                {
                    var localCacheInstanceKeyRepository = new LocalCacheInstanceKeyRepository(dbContext);
                    await UpdateAllLocalCacheInstanceKeysAsync(localCacheInstanceKeyRepository, token).ConfigureAwait(false);

                    var localCacheInstanceLruRepository = new LocalCacheInstanceLruRepository(dbContext);
                    await InsertLocalCacheInstanceLruAsync(localCacheInstanceLruRepository, token).ConfigureAwait(false);

                    var localCacheInstanceRepository = new LocalCacheInstanceRepository(dbContext);
                    await UpdateLocalCacheInstanceAsync(localCacheInstanceRepository, token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"ExecuteTimerWorkAsync: hgas produced and error {ex}.");
                }
                finally
                {
                    // ReSharper disable once MethodSupportsCancellation
                    await dbContext.CloseAsync();
                    // ReSharper disable once MethodSupportsCancellation
                    await dbContext.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info($"Failed to store cache instance with {ex}");
                }
            }
            finally
            {
                timerSemaphore.Release();
            }
        }

        private async Task UpdateAllLocalCacheInstanceKeysAsync(LocalCacheInstanceKeyRepository localCacheInstanceKeyRepository, CancellationToken token = default)
        {

            foreach (var entry in localCacheInstanceKeys)
            {
                try
                {
                    await InsertLocalCacheInstanceKeyAsync(localCacheInstanceKeyRepository, entry, token);
                }
                catch (Exception ex)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Failed to store cache instance key for {entry.Key} with {ex}");
                    }
                }
                finally
                {
                    ResetLocalCacheInstanceKeyCounters(entry);
                }
            }
        }

        private void ResetLocalCacheInstanceKeyCounters(KeyValuePair<string, LocalCacheInstanceKey> entry)
        {

            Interlocked.Exchange(ref entry.Value.Requests, 0);
            Interlocked.Exchange(ref entry.Value.Misses, 0);
            Interlocked.Exchange(ref entry.Value.MissRemoteResponseTime, 0);
            Interlocked.Exchange(ref entry.Value.UnpackResponseTime, 0);
            Interlocked.Exchange(ref entry.Value.HashSetSubscriptions, 0);
            Interlocked.Exchange(ref entry.Value.HashRemove, 0);
            Interlocked.Exchange(ref entry.Value.HashRemoveMiss, 0);
            Interlocked.Exchange(ref entry.Value.HashRemoveSubscription, 0);
            Interlocked.Exchange(ref entry.Value.HashRemoveSubscriptionMiss, 0);
            Interlocked.Exchange(ref entry.Value.DualMiss, 0);
        }

        private Task UpdateLocalCacheInstanceAsync(LocalCacheInstanceRepository localCacheInstanceRepository, CancellationToken token = default)
        {
            var info = GC.GetGCMemoryInfo();

            return localCacheInstanceRepository.UpdateCountAndBytesAsync(localCacheInstance.Id,
                lruCacheConcurrentSizedDictionary.Count,
                lruCacheConcurrentSizedDictionary.TotalSize,
                info.HeapSizeBytes,
                info.TotalCommittedBytes, token);
        }

        private async Task InsertLocalCacheInstanceLruAsync(LocalCacheInstanceLruRepository localCacheInstanceLruRepository, CancellationToken token = default)
        {
            try
            {
                var localCacheInstanceLru = new LocalCacheInstanceLru
                {
                    LocalCacheInstanceId = localCacheInstance?.Id,
                    CreatedDate = DateTime.UtcNow,
                    Bytes = lruCacheConcurrentSizedDictionary.TotalSize,
                    Count = lruCacheConcurrentSizedDictionary.Count,
                    RequestBytes = lruCacheConcurrentSizedDictionary.RequestBytes,
                    RequestCount = lruCacheConcurrentSizedDictionary.RequestCount,
                    AddBytes = lruCacheConcurrentSizedDictionary.AddBytes,
                    AddCount = lruCacheConcurrentSizedDictionary.AddCounter,
                    RemoveBytes = lruCacheConcurrentSizedDictionary.RemoveBytes,
                    RemoveCount = lruCacheConcurrentSizedDictionary.RemoveCount,
                    EvictionBytes = lruCacheConcurrentSizedDictionary.EvictionBytes,
                    EvictionCount = lruCacheConcurrentSizedDictionary.EvictionCount,
                    UpdateBytes = lruCacheConcurrentSizedDictionary.UpdateBytes,
                    UpdateCount = lruCacheConcurrentSizedDictionary.UpdateCount
                };

                await localCacheInstanceLruRepository.InsertAsync(localCacheInstanceLru, token);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            finally
            {
                lruCacheConcurrentSizedDictionary.ResetCounters();
            }
        }

        private Task InsertLocalCacheInstanceKeyAsync(LocalCacheInstanceKeyRepository localCacheInstanceKeyRepository, KeyValuePair<string, LocalCacheInstanceKey> entry, CancellationToken token = default)
        {
            var localCacheInstanceKey = new Data.Poco.LocalCacheInstanceKey
            {
                Key = entry.Key,
                LocalCacheInstanceId = localCacheInstance?.Id,
                Requests = entry.Value.Requests,
                Misses = entry.Value.Misses,
                MissRemoteResponseTime = entry.Value.MissRemoteResponseTime,
                UnpackResponseTime = entry.Value.UnpackResponseTime,
                HashSetSubscriptions = entry.Value.HashSetSubscriptions,
                HashRemove = entry.Value.HashRemove,
                HashRemoveMiss = entry.Value.HashRemoveMiss,
                HashRemoveSubscription = entry.Value.HashRemoveSubscription,
                HashRemoveSubscriptionMiss = entry.Value.HashRemoveSubscriptionMiss,
                DualMiss = entry.Value.DualMiss
            };

            return localCacheInstanceKeyRepository.InsertAsync(localCacheInstanceKey, token);
        }

        private void SubscribeToRedisHashEvents()
        {
            {
                if (!publishSubscribe)
                {
                    return;
                }

                SubscribeToHashSet();
                SubscribeToHashRemove();
            }
            return;

            void SubscribeToHashSet()
            {
                var subscriber = connectionMultiplexer.GetSubscriber();
                subscriber.Subscribe(RedisChannel.Pattern("HashSet:*:*:Payload*"), (channel, value) =>
                {
                    var splits = channel.ToString().Split(":");

                    if (CheckThatTheEventDidNotComeFromThisHost(splits[2], splits[1]))
                    {
                        return;
                    }

                    var key = String.Join(":", splits[3..^1]);
                    AddToLocalCacheIfNotExistsWithCounters(key, splits, value);
                });
            }

            void SubscribeToHashRemove()
            {
                var subscriber = connectionMultiplexer.GetSubscriber();
                subscriber.Subscribe(RedisChannel.Pattern("HashRemove:*:*:Payload*"), (channel, value) =>
                {
                    var splits = channel.ToString().Split(":");

                    if (localCacheInstanceGuidString == splits[2] && Dns.GetHostName() == splits[1])
                    {
                        return;
                    }

                    var hashSetKeyEntry = GetLocalCacheEntry(String.Join(":", splits[3..]));

                    Interlocked.Increment(ref hashSetKeyEntry.HashRemoveSubscription);

                    DeleteFromLocalCache(hashSetKeyEntry, value.ToString(), true);
                });
            }
        }

        private void AddToLocalCacheIfNotExistsWithCounters(string key, string[] splits, RedisValue value)
        {
            var localCacheForPayloadKey = GetLocalCacheEntry(key);

            Interlocked.Increment(ref localCacheForPayloadKey.HashSetSubscriptions);

            var sw = new Stopwatch();
            sw.Start();

            AddToLruCacheConcurrentSizedDictionaryForLocalCacheInstanceKey(localCacheForPayloadKey, splits[^1], value);

            Interlocked.Add(ref localCacheForPayloadKey.UnpackResponseTime, (int)(sw.ElapsedTicks * 1000000 / Stopwatch.Frequency));

            sw.Stop();
        }

        private bool CheckThatTheEventDidNotComeFromThisHost(string cacheInstanceGuidString, string cacheHostName)
        {
            return localCacheInstanceGuidString == cacheInstanceGuidString && Dns.GetHostName() == cacheHostName;
        }

        private async Task CreateLocalCacheInstanceAsync(CancellationToken token = default)
        {
            var localCacheInstanceRepository = new LocalCacheInstanceRepository(DataConnectionDbContext.GetResilientDbContextDataConnection(postgresConnectionString, log));

            localCacheInstance = await localCacheInstanceRepository.InsertAsync(new LocalCacheInstance
            {
                Instance = Dns.GetHostName(),
                Guid = Guid.NewGuid()
            }, token);

            localCacheInstanceGuidString = localCacheInstance.Guid.ToString("N");
        }

        private void InstantiateLruCacheConcurrentSizedDictionary()
        {

            lruCacheConcurrentSizedDictionary = new LruCacheConcurrentSizedDictionary<string, byte[]>(obj => obj.Length,
                localCacheBytes, 0.85);

            localCacheInstanceKeys = new ConcurrentDictionary<string, LocalCacheInstanceKey>();
        }

        private async Task FillPreferReplicaAsync(TimeSpan maxAge, CancellationToken token = default)
        {
            try
            {
                var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(postgresConnectionString, log);
                try
                {
                    var localCacheInstanceRepository = new LocalCacheInstanceRepository(dbContext);

                    if (!fill)
                    {
                        await localCacheInstanceRepository.FinishFillAsync(localCacheInstance.Id, 0, 0, lruCacheConcurrentSizedDictionary.Count, lruCacheConcurrentSizedDictionary.TotalSize, token);
                        return;
                    }

                    token.ThrowIfCancellationRequested();

                    var count = 0;
                    var bytes = 0L;
                    var cutoff = DateTime.UtcNow.Add(-maxAge).ToUnixTimeMilliSeconds();

                    await localCacheInstanceRepository.StartFillAsync(localCacheInstance.Id, token);

                    var entityAnalysisModelRepository = new EntityAnalysisModelRepository(dbContext);
                    foreach (var entityAnalysisModel in await entityAnalysisModelRepository.GetAsync(token).ConfigureAwait(false))
                    {
                        token.ThrowIfCancellationRequested();

                        var lruJournalKey = $"LruJournal:{entityAnalysisModel.TenantRegistryId}:{entityAnalysisModel.Guid:N}";
                        var payloadKey = $"Payload:{entityAnalysisModel.TenantRegistryId}:{entityAnalysisModel.Guid:N}";
                        var localCacheForPayloadKey = GetLocalCacheEntry(lruJournalKey);

                        try
                        {
                            long hotKeyPosition = 0;
                            var olderThanCutoff = false;
                            while (true)
                            {
                                token.ThrowIfCancellationRequested();

                                var hotKeyEntries = await resilientRedisResilientRedisDatabase.SortedSetRangeByRankWithScoresAsync(
                                    lruJournalKey,
                                    hotKeyPosition,
                                    hotKeyPosition + HotKeysBatchSize - 1,
                                    Order.Descending,
                                    CommandFlags.PreferReplica
                                );

                                if (hotKeyEntries.Length == 0)
                                {
                                    break;
                                }

                                foreach (var hotKeyEntry in hotKeyEntries)
                                {
                                    if (hotKeyEntry.Score < cutoff)
                                    {
                                        olderThanCutoff = true;
                                        break;
                                    }

                                    long journalPosition = 0;

                                    while (true)
                                    {
                                        token.ThrowIfCancellationRequested();

                                        var journalEntries = await resilientRedisResilientRedisDatabase.SortedSetRangeByRankWithScoresAsync(
                                            hotKeyEntry.Element.ToString(),
                                            journalPosition,
                                            journalPosition + JournalBatchSize - 1,
                                            Order.Descending,
                                            CommandFlags.PreferReplica
                                        );

                                        if (journalEntries.Length == 0)
                                        {
                                            break;
                                        }

                                        var hashTasks = journalEntries
                                            .Select(e => resilientRedisResilientRedisDatabase.HashGetAsync(payloadKey, e.Element.ToString(),
                                                CommandFlags.PreferReplica))
                                            .ToList();

                                        var hashEntries = await Task.WhenAll(hashTasks);

                                        foreach (var (journalEntry, hashEntry) in journalEntries.Zip(hashEntries))
                                        {
                                            token.ThrowIfCancellationRequested();

                                            try
                                            {
                                                if (!hashEntry.HasValue)
                                                {
                                                    continue;
                                                }

                                                AddToLruCacheConcurrentSizedDictionaryForLocalCacheInstanceKey(
                                                    localCacheForPayloadKey,
                                                    journalEntry.ToString(), hashEntry
                                                );
                                                bytes += hashEntry.Length();
                                                count += 1;
                                            }
                                            catch (Exception ex) when (ex is not OperationCanceledException)
                                            {
                                                log.Error($"Error processing hashEntry {hashEntry}: {ex}");
                                            }

                                            if (count % 100000 == 0)
                                            {
                                                await UpdateFillInLocalCacheInstanceRepositoryAsync(localCacheInstanceRepository, count, bytes, token);
                                            }

                                            if (!lruCacheConcurrentSizedDictionary.IsFull)
                                            {
                                                continue;
                                            }

                                            await FinishFillInLocalCacheInstanceRepositoryAsync(localCacheInstanceRepository, count, bytes, token);
                                            return;
                                        }

                                        if (journalEntries.Length < JournalBatchSize)
                                        {
                                            break;
                                        }

                                        journalPosition += JournalBatchSize;
                                    }
                                }

                                if (olderThanCutoff || hotKeyEntries.Length < HotKeysBatchSize)
                                {
                                    break;
                                }

                                hotKeyPosition += HotKeysBatchSize;
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            log.Error($"Could not migrate key {lruJournalKey} for exception {ex}.");
                        }
                    }

                    await FinishFillInLocalCacheInstanceRepositoryAsync(localCacheInstanceRepository, count, bytes, token);
                }
                catch (Exception ex)
                {
                    log.Error($"FillAsync: has experienced an error {ex}.");
                }
            }
            catch (OperationCanceledException ex)
            {
                log.Info($"Graceful Cancellation FillAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                log.Error($"FillAsync: has produced an error {ex}");
            }
        }

        private Task FinishFillInLocalCacheInstanceRepositoryAsync(LocalCacheInstanceRepository localCacheInstanceRepository, int count, long bytes, CancellationToken token = default)
        {
            var gCMemoryInfoForFinishFill = GC.GetGCMemoryInfo();
            return localCacheInstanceRepository.FinishFillAsync(localCacheInstance.Id, count, bytes,
                gCMemoryInfoForFinishFill.HeapSizeBytes, gCMemoryInfoForFinishFill.TotalCommittedBytes, token);
        }

        private Task UpdateFillInLocalCacheInstanceRepositoryAsync(LocalCacheInstanceRepository localCacheInstanceRepository, int count, long bytes, CancellationToken token = default)
        {
            var gCMemoryInfoForUpdateFill = GC.GetGCMemoryInfo();
            return localCacheInstanceRepository.UpdateFillAsync(localCacheInstance.Id,
                count, bytes, lruCacheConcurrentSizedDictionary.Count,
                lruCacheConcurrentSizedDictionary.TotalSize,
                gCMemoryInfoForUpdateFill.HeapSizeBytes, gCMemoryInfoForUpdateFill.TotalCommittedBytes, token);
        }

        private DictionaryNoBoxing<int> Unpack(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return new DictionaryNoBoxing<int>();
            }

            var unpacked = MessagePackSerializer
                .Deserialize<EnvelopeDictionaryNoBoxing<int>>(buffer, messagePackSerializerOptions);

            return unpacked.Data ?? new DictionaryNoBoxing<int>();
        }

        private void AddToLruCacheConcurrentSizedDictionaryForLocalCacheInstanceKey(LocalCacheInstanceKey localCacheInstanceKey, string hSetKey,
            byte[] bytes)
        {
            if (!localCache)
            {
                return;
            }

            localCacheInstanceKey.LruCacheConcurrentSizedDictionary.TryAdd(hSetKey, bytes);
        }

        private async Task AppendBulkCleanupOfPayloadGuidsAsync(List<Task<TimedTaskResult>> tasks, int tenantRegistryId, Guid entityAnalysisModelGuid, string redisKeyReferenceDate, List<RedisValue> payloadGuidsToDelete)
        {
            var redisKeyPayload = $"Payload:{tenantRegistryId}:{entityAnalysisModelGuid:N}";

            tasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.SortedSetRemoveReferenceDate, async () => await BatchSortedSetRemoveAsync(
                resilientRedisResilientRedisDatabase,
                redisKeyReferenceDate,
                payloadGuidsToDelete.ToArray()
            )));

            if (storePayloadCountsAndBytes)
            {
                var redisKeyCount = $"PayloadCount:{tenantRegistryId}";
                var redisKeyBytes = $"PayloadBytes:{tenantRegistryId}";
                var redisHashKeyForRedisKey = entityAnalysisModelGuid.ToString("N");

                foreach (var payloadGuidToDelete in payloadGuidsToDelete)
                {
                    var bytesToRemove = await resilientRedisResilientRedisDatabase.HashStringLengthAsync(redisKeyPayload, payloadGuidToDelete).ConfigureAwait(false);

                    tasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.HashDecrementBytes, async () => await resilientRedisResilientRedisDatabase.HashDecrementAsync(
                        redisKeyBytes, redisHashKeyForRedisKey,
                        bytesToRemove
                    )));

                    tasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.HashDecrementCount, async () => await resilientRedisResilientRedisDatabase.HashDecrementAsync(
                        redisKeyCount, redisHashKeyForRedisKey, 1
                    )));

                    tasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.HashDeletePayload, async () => await resilientRedisResilientRedisDatabase.HashDeleteAsync(
                        redisKeyPayload,
                        payloadGuidToDelete
                    )));
                }
            }
            else
            {
                tasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.HashDeletePayloadBulk, async () => await resilientRedisResilientRedisDatabase.HashDeleteAsync(
                    redisKeyPayload,
                    payloadGuidsToDelete.ToArray()
                )));
            }
        }

        private async Task AppendDeletionTasksAsync(List<Task<TimedTaskResult>> tasks, int tenantRegistryId, Guid entityAnalysisModelGuid, SortedSetEntry sortedSetEntry)
        {
            var redisKeyPayloadJournal = $"PayloadJournal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{sortedSetEntry.Element}";
            var redisValuesForRedisKeyPayloadJournalToDelete = await resilientRedisResilientRedisDatabase.SetMembersAsync(redisKeyPayloadJournal).ConfigureAwait(false);

            foreach (var redisJournalValue in redisValuesForRedisKeyPayloadJournalToDelete)
            {
                var redisJournalKey = $"Journal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{redisJournalValue}";
                var keyPayload = $"Payload:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
                var lruJournalKey = $"LruJournal:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
                var eventHashRemovePattern = $"HashRemove:{Dns.GetHostName()}:{localCacheInstanceGuidString}:{keyPayload}";

                DeleteFromLocalCache(keyPayload, sortedSetEntry.Element.ToString(), false);
                AppendSetRemovalTasksAndPublishEvent(tasks, sortedSetEntry, redisJournalKey, redisKeyPayloadJournal, lruJournalKey, redisJournalValue, eventHashRemovePattern);
            }
        }

        private void AppendSetRemovalTasksAndPublishEvent(List<Task<TimedTaskResult>> tasks, SortedSetEntry sortedSetEntry,
            string redisJournalKey, string redisKeyPayloadJournal, string redisLruJournalKey, RedisValue redisJournalValue,
            string eventHashRemovePattern)
        {
            tasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.SortedSetRemoveReferenceDate, async () => await resilientRedisResilientRedisDatabase.SortedSetRemoveAsync(
                redisJournalKey,
                sortedSetEntry.Element
            )));

            tasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.SetRemoveAsync, async () => await resilientRedisResilientRedisDatabase.SetRemoveAsync(
                redisKeyPayloadJournal,
                redisJournalValue
            )));

            tasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.SortedSetLruJournalRemove, async () =>
            {
                if (!await resilientRedisResilientRedisDatabase.KeyExistsAsync(redisJournalKey))
                {
                    await resilientRedisResilientRedisDatabase.SortedSetRemoveAsync(redisLruJournalKey, redisJournalKey);
                }
            }));

            if (publishSubscribe)
            {
                tasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.PublishAsync, async () => await resilientRedisResilientRedisDatabase.PublishAsync(
                    RedisChannel.Pattern(eventHashRemovePattern),
                    sortedSetEntry.Element.ToString()
                )));
            }
        }

        private void DeleteFromLocalCache(string keyPayload, string hashKey, bool subscription)
        {
            var hashSetKeyEntry = GetLocalCacheEntry(keyPayload);
            DeleteFromLocalCache(hashSetKeyEntry, hashKey, subscription);
        }

        private void DeleteFromLocalCache(LocalCacheInstanceKey localCacheInstanceKey, string hashKey, bool subscription)
        {
            if (!localCache)
            {
                return;
            }

            var removed = localCacheInstanceKey.LruCacheConcurrentSizedDictionary.Remove(hashKey);

            if (subscription)
            {
                Interlocked.Increment(ref localCacheInstanceKey.HashRemoveSubscription);
                if (removed)
                {
                    Interlocked.Increment(ref localCacheInstanceKey.HashRemoveSubscription);
                }
            }
            else
            {
                Interlocked.Increment(ref localCacheInstanceKey.HashRemove);
                if (removed)
                {
                    Interlocked.Increment(ref localCacheInstanceKey.HashRemoveMiss);
                }
            }
        }

        private LocalCacheInstanceKey GetLocalCacheEntry(string payloadKey)
        {
            if (localCacheInstanceKeys.TryGetValue(payloadKey, out var localCacheForPayloadKey))
            {
                return localCacheForPayloadKey;
            }

            var hashSetKeyEntry = new LocalCacheInstanceKey(lruCacheConcurrentSizedDictionary);
            localCacheInstanceKeys.TryAdd(payloadKey, hashSetKeyEntry);
            return hashSetKeyEntry;
        }

        public async Task InsertPayloadJournalAndLedgerAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string key,
            string value,
            DateTime referenceDate, Guid entityAnalysisModelInstanceEntryGuid)
        {
            try
            {
                var redisKeyJournal = $"Journal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{key}:{value}";
                var redisKeyPayloadJournal = $"PayloadJournal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelInstanceEntryGuid:N}";
                var valuePayloadGuid = $"{entityAnalysisModelInstanceEntryGuid:N}";

                await Task.WhenAll(
                    resilientRedisResilientRedisDatabase.SortedSetAddAsync(redisKeyJournal, valuePayloadGuid, referenceDate.ToUnixTimeMilliSeconds()),
                    resilientRedisResilientRedisDatabase.SetAddAsync(redisKeyPayloadJournal, $"{key}:{value}")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }

        private async Task BatchSortedSetRemoveAsync(IHybridResilientRedisDatabase db, RedisKey key, IEnumerable<RedisValue> values)
        {
            const int batchSize = 1000;
            var valuesArray = values.ToArray();

            for (var i = 0; i < valuesArray.Length; i += batchSize)
            {
                var batch = valuesArray.Skip(i).Take(batchSize).ToArray();
                await db.SortedSetRemoveAsync(key, batch).ConfigureAwait(false);
                await Task.Yield();
                await Task.Delay(1);
            }
        }

        private async Task CleanupIdempotencyAsync(int tenantRegistryId,
            Guid entityAnalysisModelGuid, List<RedisValue> entityAnalysisModelInstanceEntryGuids)
        {
            try
            {
                var redisJournal = $"IdempotencyJournal:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
                var entryGuidsArray = entityAnalysisModelInstanceEntryGuids.ToArray();

                var redisKeys = await resilientRedisResilientRedisDatabase.SetMembersAsync(redisJournal).ConfigureAwait(false);

                foreach (var redisKey in redisKeys)
                {
                    await resilientRedisResilientRedisDatabase
                        .SetRemoveAsync(redisKey.ToString(), entryGuidsArray).ConfigureAwait(false);

                    if (!await resilientRedisResilientRedisDatabase
                            .KeyExistsAsync(redisKey.ToString()).ConfigureAwait(false))
                    {
                        await resilientRedisResilientRedisDatabase
                            .SetRemoveAsync(redisJournal, redisKey).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }

        public Task<long> PurgeExpiredLruJournalEntriesAsync(
            int tenantRegistryId, Guid entityAnalysisModelGuid, TimeSpan maxAge)
        {
            var lruJournalKey = $"LruJournal:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
            var cutoff = DateTime.UtcNow.Add(-maxAge).ToUnixTimeMilliSeconds();

            return resilientRedisResilientRedisDatabase
                .SortedSetRemoveRangeByScoreAsync(lruJournalKey, Double.NegativeInfinity, cutoff);
        }
    }
}
