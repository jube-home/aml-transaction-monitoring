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
    using MessagePack;
    using Interfaces;
    using log4net;
    using Serialization;
    using Serialization.DictionaryNoBoxing.MessagePack;
    using StackExchange.Redis;
    using LocalCacheInstanceKey=Models.LocalCacheInstanceKey;

    public class CachePayloadRepository : ICachePayloadRepository
    {
        private readonly CommandFlags commandFlag;
        private readonly ConnectionMultiplexer connectionMultiplexer;
        private readonly DbContext dbContext;
        private readonly bool fill;
        private readonly bool localCache;
        private readonly long localCacheBytes;
        private readonly bool publishSubscribe;
        private readonly ILog log;
        private readonly MessagePackSerializerOptions messagePackSerializerOptions;
        private readonly IDatabaseAsync redisDatabase;
        private readonly bool storePayloadCountsAndBytes;
        private readonly object timerLock = new object();
        private EntityAnalysisModelRepository entityAnalysisModelRepository;
        private LocalCacheInstance localCacheInstance;
        private string localCacheInstanceGuidString;
        private LocalCacheInstanceKeyRepository localCacheInstanceKeyRepository;
        private ConcurrentDictionary<string, LocalCacheInstanceKey> localCacheInstanceKeys;
        private LocalCacheInstanceLruRepository localCacheInstanceLruRepository;
        private LocalCacheInstanceRepository localCacheInstanceRepository;
        private LruCacheConcurrentSizedDictionary<string, byte[]> lruCacheConcurrentSizedDictionary;
        // ReSharper disable once NotAccessedField.Local
        private Timer timer;

        private CachePayloadRepository(ConnectionMultiplexer connectionMultiplexer, IDatabaseAsync redisDatabase,
            DbContext dbContext, ILog log,
            CommandFlags commandFlag, bool fill, bool localCache, long localCacheBytes, bool messagePackCompression, bool storePayloadCountsAndBytes,
            bool publishSubscribe)
        {
            this.connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
            this.redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.commandFlag = commandFlag;
            this.fill = fill;
            this.localCache = localCache;
            this.localCacheBytes = localCacheBytes;
            this.storePayloadCountsAndBytes = storePayloadCountsAndBytes;
            this.publishSubscribe = publishSubscribe;
            
            messagePackSerializerOptions = MessagePackSerializerOptionsHelper.EnveloperMessagePackSerializerWithCompressionOptions(messagePackCompression);
            
            InstantiateLruCacheConcurrentSizedDictionary();
            InstantiateRepositoriesAndCreateLocalCacheInstance();
            SubscribeToRedisHashEvents();
            InstantiateLocalCacheInstanceCountersTimer();
        }

        public async Task InsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            DictionaryNoBoxing payload,
            DateTime referenceDate,
            Guid entityAnalysisModelInstanceEntryGuid)
        {
            try
            {
                var keyPayload = $"Payload:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
                var localCacheForPayloadKey = GetLocalCacheEntry(keyPayload);

                var ms = new MemoryStream();

                var dictionaryNoBoxingWrapper = new EnvelopeDictionaryNoBoxing
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
                    redisDatabase.HashSetAsync(keyPayload, hSetKey, bytes,
                        When.Always, commandFlag),
                    redisDatabase.SortedSetAddAsync(keyReferenceDate, sortedSet, referenceDate.ToUnixTimeMilliSeconds(),
                        commandFlag)
                };

                if (publishSubscribe)
                {
                    tasks.Add(redisDatabase.PublishAsync(
                        RedisChannel.Pattern($"HashSet:{Dns.GetHostName()}:{localCacheInstanceGuidString}:{keyPayload}:{hSetKey}"),
                        bytes));
                }
                
                if (storePayloadCountsAndBytes)
                {
                    var redisKeyPayloadCount = $"PayloadCount:{tenantRegistryId}";
                    var redisKeyPayloadBytes = $"PayloadBytes:{tenantRegistryId}";
                    tasks.Add(redisDatabase.HashIncrementAsync(redisKeyPayloadCount, entityAnalysisModelGuid.ToString("N")));
                    tasks.Add(redisDatabase.HashIncrementAsync(redisKeyPayloadBytes, entityAnalysisModelGuid.ToString("N"), bytes.Length));
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
            DictionaryNoBoxing payload,
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

        public async Task DeleteByReferenceDate(int tenantRegistryId, Guid entityAnalysisModelGuid, DateTime referenceDate, int limit)
        {
            var referenceDateTimestampThreshold = referenceDate.ToUnixTimeMilliSeconds();
            var redisKeyReferenceDate = $"ReferenceDate:{tenantRegistryId}:{entityAnalysisModelGuid:N}";

            while (true)
            {
                var sortedSetEntriesForRedisKeyReferenceDate = await redisDatabase.SortedSetRangeByRankWithScoresAsync(redisKeyReferenceDate, 0, limit)
                    .ConfigureAwait(false);

                if (sortedSetEntriesForRedisKeyReferenceDate.Length == 0)
                {
                    return;
                }

                var tasks = new List<Task>();
                var payloadGuidsToDelete = new List<RedisValue>();

                foreach (var sortedSetEntryForRedisKeyReferenceDate in sortedSetEntriesForRedisKeyReferenceDate)
                {
                    if (sortedSetEntryForRedisKeyReferenceDate.Score > referenceDateTimestampThreshold)
                    {
                        return;
                    }

                    payloadGuidsToDelete.Add(new RedisValue(sortedSetEntryForRedisKeyReferenceDate.Element.ToString()));

                    await AppendDeletionTasksAsync(tasks, tenantRegistryId, entityAnalysisModelGuid, sortedSetEntryForRedisKeyReferenceDate).ConfigureAwait(false);
                }

                if (payloadGuidsToDelete.Count <= 0)
                {
                    continue;
                }

                tasks.Add(AppendBulkCleanupOfPayloadGuids(tasks, tenantRegistryId, entityAnalysisModelGuid, redisKeyReferenceDate, payloadGuidsToDelete));

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        public static async Task<CachePayloadRepository> CreateAsync(
            ConnectionMultiplexer connectionMultiplexer,
            IDatabaseAsync redisDatabase,
            DbContext dbContext,
            ILog log,
            CommandFlags commandFlag,
            bool localCacheFill,
            bool localCache,
            long localCacheBytes,
            bool messagePackCompression,
            bool storePayloadCountsAndBytes,
            bool publishSubscribe)
        {
            var repository = new CachePayloadRepository(connectionMultiplexer, redisDatabase,
                dbContext, log, commandFlag, localCacheFill, localCache, localCacheBytes, messagePackCompression, storePayloadCountsAndBytes, publishSubscribe);
            await repository.FullyInitializeAsync();

            return repository;
        }

        private async Task FullyInitializeAsync()
        {
            if (fill && localCache)
            {
                await Fill();
            }
        }

        private void InstantiateLocalCacheInstanceCountersTimer()
        {

            timer = new Timer(OnTimerElapsed, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            return;

            void OnTimerElapsed(object state)
            {
                if (!Monitor.TryEnter(timerLock))
                {
                    return;
                }

                try
                {
                    UpdateAllLocalCacheInstanceKeys();
                    InsertLocalCacheInstanceLru();

                    UpdateLocalCacheInstance();
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
                    Monitor.Exit(timerLock);
                }
            }
        }

        private void UpdateAllLocalCacheInstanceKeys()
        {

            foreach (var entry in localCacheInstanceKeys)
            {
                try
                {
                    InsertLocalCacheInstanceKey(entry);
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
        private static void ResetLocalCacheInstanceKeyCounters(KeyValuePair<string, LocalCacheInstanceKey> entry)
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
        private void UpdateLocalCacheInstance()
        {
            var info = GC.GetGCMemoryInfo();

            localCacheInstanceRepository.UpdateCountAndBytes(localCacheInstance.Id,
                lruCacheConcurrentSizedDictionary.Count,
                lruCacheConcurrentSizedDictionary.TotalSize,
                info.HeapSizeBytes,
                info.TotalCommittedBytes);
        }

        private void InsertLocalCacheInstanceLru()
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

                localCacheInstanceLruRepository.Insert(localCacheInstanceLru);
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

        private void InsertLocalCacheInstanceKey(KeyValuePair<string, LocalCacheInstanceKey> entry)
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

            localCacheInstanceKeyRepository.Insert(localCacheInstanceKey);
        }

        private void SubscribeToRedisHashEvents()
        {
            {
                if (publishSubscribe)
                {
                    SubscribeToHashSet();
                    SubscribeToHashRemove();   
                }
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

                    Interlocked.Add(ref hashSetKeyEntry.HashRemoveSubscription, 1);

                    DeleteFromLocalCache(hashSetKeyEntry, value.ToString(), true);
                });
            }
        }

        private void AddToLocalCacheIfNotExistsWithCounters(string key, string[] splits, RedisValue value)
        {
            var localCacheForPayloadKey = GetLocalCacheEntry(key);

            Interlocked.Add(ref localCacheForPayloadKey.HashSetSubscriptions, 1);

            var sw = new Stopwatch();
            sw.Start();

            AddToLruCacheConcurrentSizedDictionaryForLocalCacheInstanceKey(localCacheForPayloadKey, splits[^1], value);

            Interlocked.Add(ref localCacheForPayloadKey.UnpackResponseTime, sw.ElapsedTicks);

            sw.Stop();
        }

        private bool CheckThatTheEventDidNotComeFromThisHost(string cacheInstanceGuidString, string cacheHostName)
        {
            return localCacheInstanceGuidString == cacheInstanceGuidString && Dns.GetHostName() == cacheHostName;
        }

        private void InstantiateRepositoriesAndCreateLocalCacheInstance()
        {

            entityAnalysisModelRepository = new EntityAnalysisModelRepository(dbContext);
            localCacheInstanceRepository = new LocalCacheInstanceRepository(dbContext);
            localCacheInstanceKeyRepository = new LocalCacheInstanceKeyRepository(dbContext);
            localCacheInstanceLruRepository = new LocalCacheInstanceLruRepository(dbContext);

            localCacheInstance = localCacheInstanceRepository.Insert(new LocalCacheInstance
            {
                Instance = Dns.GetHostName(),
                Guid = Guid.NewGuid()
            });

            localCacheInstanceGuidString = localCacheInstance.Guid.ToString("N");
        }

        private void InstantiateLruCacheConcurrentSizedDictionary()
        {

            lruCacheConcurrentSizedDictionary = new LruCacheConcurrentSizedDictionary<string, byte[]>(obj => obj.Length,
                localCacheBytes, 0.85);

            localCacheInstanceKeys = new ConcurrentDictionary<string, LocalCacheInstanceKey>();
        }

        private async Task Fill()
        {
            if (fill)
            {
                var count = 0;
                var bytes = 0L;

                localCacheInstanceRepository.StartFill(localCacheInstance.Id);

                foreach (var entityAnalysisModel in entityAnalysisModelRepository.Get().ToList())
                {
                    var payloadKey = $"Payload:{entityAnalysisModel.TenantRegistryId}:{entityAnalysisModel.Guid:N}";
                    var localCacheForPayloadKey = GetLocalCacheEntry(payloadKey);

                    try
                    {
                        await foreach (var hashEntry in redisDatabase.HashScanAsync(payloadKey))
                        {
                            try
                            {
                                AddToLruCacheConcurrentSizedDictionaryForLocalCacheInstanceKey(
                                    localCacheForPayloadKey,
                                    hashEntry.Name.ToString(), hashEntry.Value
                                );
                                Interlocked.Add(ref bytes, hashEntry.Value.Length());
                                Interlocked.Add(ref count, 1);
                            }
                            catch (Exception ex)
                            {
                                log.Error($"Error processing hashEntry {hashEntry.Name}: {ex.Message}");
                            }

                            if (count % 100000 == 0)
                            {
                                UpdateFillInLocalCacheInstanceRepository(count, bytes);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Could not migrate key {payloadKey} for exception {ex}.");
                    }
                }

                FinishFillInLocalCacheInstanceRepository(count, bytes);
            }
            else
            {
                localCacheInstanceRepository.FinishFill(localCacheInstance.Id, 0, 0, lruCacheConcurrentSizedDictionary.Count, lruCacheConcurrentSizedDictionary.TotalSize);
            }
        }
        private void FinishFillInLocalCacheInstanceRepository(int count, long bytes)
        {
            var gCMemoryInfoForFinishFill = GC.GetGCMemoryInfo();
            localCacheInstanceRepository.FinishFill(localCacheInstance.Id, count, bytes,
                gCMemoryInfoForFinishFill.HeapSizeBytes, gCMemoryInfoForFinishFill.TotalCommittedBytes);
        }

        private void UpdateFillInLocalCacheInstanceRepository(int count, long bytes)
        {
            var gCMemoryInfoForUpdateFill = GC.GetGCMemoryInfo();
            localCacheInstanceRepository.UpdateFill(localCacheInstance.Id,
                count, bytes, lruCacheConcurrentSizedDictionary.Count,
                lruCacheConcurrentSizedDictionary.TotalSize,
                gCMemoryInfoForUpdateFill.HeapSizeBytes, gCMemoryInfoForUpdateFill.TotalCommittedBytes);
        }

        private DictionaryNoBoxing Unpack(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return new DictionaryNoBoxing();
            }

            var unpacked = MessagePackSerializer
                .Deserialize<EnvelopeDictionaryNoBoxing>(buffer, messagePackSerializerOptions);
            
            return unpacked.Data ?? new DictionaryNoBoxing();
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

        private async Task AppendBulkCleanupOfPayloadGuids(List<Task> tasks, int tenantRegistryId, Guid entityAnalysisModelGuid, string redisKeyReferenceDate, List<RedisValue> payloadGuidsToDelete)
        {
            var redisKeyPayload = $"Payload:{tenantRegistryId}:{entityAnalysisModelGuid:N}";

            tasks.Add(redisDatabase.SortedSetRemoveAsync(
                redisKeyReferenceDate,
                payloadGuidsToDelete.ToArray()
            ));

            if (storePayloadCountsAndBytes)
            {
                var redisKeyCount = $"PayloadCount:{tenantRegistryId}";
                var redisKeyBytes = $"PayloadBytes:{tenantRegistryId}";
                var redisHashKeyForRedisKey = entityAnalysisModelGuid.ToString("N");

                foreach (var payloadGuidToDelete in payloadGuidsToDelete)
                {
                    var bytesToRemove = await redisDatabase.HashStringLengthAsync(redisKeyPayload, payloadGuidToDelete);

                    tasks.Add(redisDatabase.HashDecrementAsync(
                        redisKeyBytes, redisHashKeyForRedisKey,
                        bytesToRemove
                    ));

                    tasks.Add(redisDatabase.HashDecrementAsync(
                        redisKeyCount, redisHashKeyForRedisKey
                    ));

                    tasks.Add(redisDatabase.HashDeleteAsync(
                        redisKeyPayload,
                        payloadGuidToDelete
                    ));
                }
            }
            else
            {
                tasks.Add(redisDatabase.HashDeleteAsync(
                    redisKeyPayload,
                    payloadGuidsToDelete.ToArray()
                ));
            }

        }

        private async Task AppendDeletionTasksAsync(List<Task> tasks, int tenantRegistryId, Guid entityAnalysisModelGuid, SortedSetEntry sortedSetEntry)
        {
            var redisKeyPayloadJournal = $"PayloadJournal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{sortedSetEntry.Element}";
            var redisValuesForRedisKeyPayloadJournalToDelete = await redisDatabase.SetMembersAsync(redisKeyPayloadJournal).ConfigureAwait(false);

            foreach (var redisJournalValue in redisValuesForRedisKeyPayloadJournalToDelete)
            {
                var redisJournalKey = $"Journal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{redisJournalValue}";
                var keyPayload = $"Payload:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
                var eventHashRemovePattern = $"HashRemove:{Dns.GetHostName()}:{localCacheInstanceGuidString}:{keyPayload}";

                DeleteFromLocalCache(keyPayload, sortedSetEntry.Element.ToString(), false);
                AppendSetRemovalTasksAndPublishEvent(tasks, sortedSetEntry, redisJournalKey, redisKeyPayloadJournal, redisJournalValue, eventHashRemovePattern);
            }
        }

        private void AppendSetRemovalTasksAndPublishEvent(List<Task> tasks, SortedSetEntry sortedSetEntry, string redisJournalKey, string redisKeyPayloadJournal, RedisValue redisJournalValue, string eventHashRemovePattern)
        {

            tasks.Add(redisDatabase.SortedSetRemoveAsync(
                redisJournalKey,
                sortedSetEntry.Element
            ));

            tasks.Add(redisDatabase.SetRemoveAsync(
                redisKeyPayloadJournal,
                redisJournalValue
            ));

            if (publishSubscribe)
            {
                tasks.Add(redisDatabase.PublishAsync(
                    RedisChannel.Pattern(eventHashRemovePattern),
                    sortedSetEntry.Element.ToString()
                ));   
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
                Interlocked.Add(ref localCacheInstanceKey.HashRemoveSubscription, 1);
                if (removed)
                {
                    Interlocked.Add(ref localCacheInstanceKey.HashRemoveSubscription, 1);
                }
            }
            else
            {
                Interlocked.Add(ref localCacheInstanceKey.HashRemove, 1);
                if (removed)
                {
                    Interlocked.Add(ref localCacheInstanceKey.HashRemoveMiss, 1);
                }
            }
        }

        public async Task<List<DictionaryNoBoxing>> GetExcludeCurrent(
            int tenantRegistryId,
            Guid entityAnalysisModelGuid,
            string key,
            string value,
            int limit,
            Guid entityInconsistentAnalysisModelInstanceEntryGuid)
        {
            var documents = new List<DictionaryNoBoxing>();
            try
            {
                var redisKey = $"Journal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{key}:{value}";

                var sortedSetEntries =
                    (await redisDatabase.SortedSetRangeByRankWithScoresAsync(redisKey, 0, limit, Order.Descending)
                        .ConfigureAwait(false))
                    .Reverse();

                var sortedSetKeys = sortedSetEntries
                    .Where(entry => !entry.Element.IsNull &&
                                    entry.Element.ToString() != entityInconsistentAnalysisModelInstanceEntryGuid.ToString("N"))
                    .Select(entry => entry.Element)
                    .ToList();
                
                var keyPayload = $"Payload:{tenantRegistryId}:{entityAnalysisModelGuid:N}";

                var localCacheForPayloadKey = GetLocalCacheEntry(keyPayload);
                var missedSortedSetKeys = new List<RedisValue>();
                var keyToDocumentMap = new Dictionary<string, DictionaryNoBoxing>();

                foreach (var sortedSetKey in sortedSetKeys)
                {
                    Interlocked.Add(ref localCacheForPayloadKey.Requests, 1);

                    if (localCache)
                    {
                        if (localCacheForPayloadKey.LruCacheConcurrentSizedDictionary.TryGetValue(sortedSetKey.ToString(),
                                out var dictionaryNoBoxing))
                        {
                            var sw = new Stopwatch();
                            sw.Start();
                            
                            keyToDocumentMap[sortedSetKey.ToString()] = Unpack(dictionaryNoBoxing);
                            
                            Interlocked.Add(ref localCacheForPayloadKey.UnpackResponseTime, sw.ElapsedTicks);
                            
                            sw.Stop();
                            
                            continue;
                        }
                    }

                    Interlocked.Add(ref localCacheForPayloadKey.Misses, 1);
                    missedSortedSetKeys.Add(sortedSetKey);
                }

                if (missedSortedSetKeys.Any())
                {
                    var sw = new Stopwatch();
                    try
                    {
                        sw.Start();

                        var redisKeyPayloadHashKeyValues = await redisDatabase.HashGetAsync(
                            keyPayload, missedSortedSetKeys.ToArray()).ConfigureAwait(false);

                        for (var i = 0; i < missedSortedSetKeys.Count; i++)
                        {
                            if (!redisKeyPayloadHashKeyValues[i].HasValue)
                            {
                                Interlocked.Add(ref localCacheForPayloadKey.DualMiss, 1);
                                continue;
                            }

                            Interlocked.Add(ref localCacheForPayloadKey.MissRemoteResponseTime, sw.ElapsedTicks);

                            sw.Reset();
                            
                            var unpacked = Unpack(redisKeyPayloadHashKeyValues[i]);
                            
                            Interlocked.Add(ref localCacheForPayloadKey.UnpackResponseTime, sw.ElapsedTicks);

                            keyToDocumentMap[missedSortedSetKeys[i].ToString()] = unpacked;

                            sw.Stop();
                        }
                    }
                    catch (Exception ex)
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info($"Cache Redis: Serialisation error on unpacking {keyPayload} with {ex}.");
                        }
                    }
                }

                foreach (var sortedSetKey in sortedSetKeys)
                {
                    if (keyToDocumentMap.TryGetValue(sortedSetKey.ToString(), out var document))
                    {
                        documents.Add(document);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }

            return documents;
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

        public async Task InsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string key,
            string value,
            DictionaryNoBoxing payload,
            DateTime referenceDate, Guid entityAnalysisModelInstanceEntryGuid)
        {
            try
            {
                var redisKeyJournal = $"Journal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{key}:{value}";
                var redisKeyPayloadJournal = $"PayloadJournal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelInstanceEntryGuid:N}";
                var valuePayloadGuid = $"{entityAnalysisModelInstanceEntryGuid:N}";

                await Task.WhenAll(
                    redisDatabase.SortedSetAddAsync(redisKeyJournal, valuePayloadGuid, referenceDate.ToUnixTimeMilliSeconds(),
                        commandFlag),
                    redisDatabase.SetAddAsync(redisKeyPayloadJournal, $"{key}:{value}", commandFlag));
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }
    }
}
