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

using Exception=System.Exception;

namespace Jube.Cache.Redis
{
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Dictionary;
    using Extensions;
    using Interfaces;
    using log4net;
    using MessagePack;
    using Models;
    using ResilientRedisConnection;
    using Serialization;
    using StackExchange.Redis;
    using TaskCancellation.TaskHelper;

    public class CachePayloadLatestRepository(
        string postgresConnectionString,
        IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
        ILog log) : ICachePayloadLatestRepository
    {
        public async Task UpsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            DateTime referenceDate, Guid entityAnalysisModelInstanceEntryGuid, string entryKey, string entryKeyValue)
        {
            try
            {
                var cachePayloadLatest = new CachePayloadLatest
                {
                    Key = $"Payload:{tenantRegistryId}:{entityAnalysisModelGuid:N}",
                    Field = entityAnalysisModelInstanceEntryGuid.ToString(),
                    ReferenceDate = referenceDate,
                    ReclassificationCount = 0,
                    ReclassificationDate = null,
                    UpdatedDate = DateTime.UtcNow
                };

                await UpsertMessagePackAsync(tenantRegistryId, entityAnalysisModelGuid, entryKey, entryKeyValue,
                    cachePayloadLatest, referenceDate).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }

        public async Task UpsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            DictionaryNoBoxing<int> payload,
            DateTime referenceDate,
            Guid entityAnalysisModelInstanceEntryGuid, string entryKey, string entryKeyValue)
        {
            try
            {
                var cachePayloadLatest = new CachePayloadLatest
                {
                    Key =
                        $"Payload:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelInstanceEntryGuid:N}",
                    ReferenceDate = referenceDate,
                    ReclassificationCount = 0,
                    ReclassificationDate = null,
                    UpdatedDate = DateTime.UtcNow
                };

                await UpsertMessagePackAsync(tenantRegistryId, entityAnalysisModelGuid, entryKey, entryKeyValue,
                    cachePayloadLatest, referenceDate).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }

        public async Task<List<string>> GetDistinctKeysPreferReplicaAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            string key, DateTime dateFrom, DateTime dateTo)
        {
            var values = new List<string>();
            try
            {
                var redisKey = $"PayloadLatest:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{key}";

                await foreach (var hashEntry in resilientRedisResilientRedisDatabase.HashScanAsync(redisKey, flags: CommandFlags.PreferReplica))
                {
                    var unpacked = MessagePackSerializer
                        .Deserialize<CachePayloadLatest>(hashEntry.Value,
                            MessagePackSerializerOptionsHelper
                                .ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(true));

                    if (unpacked.UpdatedDate >= dateFrom && unpacked.UpdatedDate <= dateTo)
                    {
                        values.Add(hashEntry.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }

            return values;
        }

        public async Task<List<string>> GetDistinctKeysPreferReplicaAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string key,
            DateTime dateBefore)
        {
            var values = new List<string>();
            try
            {
                var redisKey = $"PayloadLatest:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{key}";

                await foreach (var hashEntry in resilientRedisResilientRedisDatabase.HashScanAsync(redisKey, flags: CommandFlags.PreferReplica))
                {
                    var unpacked = MessagePackSerializer
                        .Deserialize<CachePayloadLatest>(hashEntry.Value,
                            MessagePackSerializerOptionsHelper
                                .ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(true));

                    if (unpacked.UpdatedDate <= dateBefore)
                    {
                        values.Add(hashEntry.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }

            return values;
        }

        public async Task<List<string>> GetDistinctKeysPreferReplicaAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string key)
        {
            var values = new List<string>();
            try
            {
                var redisKey = $"PayloadLatest:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{key}";

                await foreach (var hashEntry in resilientRedisResilientRedisDatabase.HashScanAsync(redisKey, flags: CommandFlags.PreferReplica))
                {
                    values.Add(hashEntry.Name);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }

            return values;
        }

        public Task DeleteByReferenceDateAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            DateTime referenceDate, DateTime thresholdReferenceDate, int limit,
            List<(string name, string interval, int intervalValue)> searchKeys)
        {
            return DeleteExpiredReferenceDateLatestPreferReplicaAsync(tenantRegistryId, entityAnalysisModelGuid, thresholdReferenceDate, limit);
        }

        private async Task UpsertMessagePackAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string entryKey,
            string entryKeyValue, CachePayloadLatest cachePayloadLatest, DateTime referenceDate)
        {
            try
            {
                var ms = new MemoryStream();
                await MessagePackSerializer.SerializeAsync(ms, cachePayloadLatest,
                    MessagePackSerializerOptionsHelper
                        .ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(true)).ConfigureAwait(false);

                var redisKeyPayloadLatest = $"PayloadLatest:{tenantRegistryId.ToString()}:{entityAnalysisModelGuid:N}";
                var redisKeyReferenceDateLatest =
                    $"ReferenceDateLatest:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entryKey}";
                var redisKeyPayloadLatestReferenceDate = $"ReferenceDateLatest:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
                var redisHSetKey = $"{entryKeyValue}";
                var referenceDateTimestamp = referenceDate.ToUnixTimeMilliSeconds();
                var bytes = ms.ToArray();

                var updateLatestTask = resilientRedisResilientRedisDatabase.SortedSetUpdateAsync(redisKeyReferenceDateLatest, redisHSetKey, referenceDateTimestamp);

                await resilientRedisResilientRedisDatabase.HashSetAsync(redisKeyPayloadLatestReferenceDate, entryKey, referenceDateTimestamp);
                await resilientRedisResilientRedisDatabase.HashSetAsync(redisKeyPayloadLatest, redisHSetKey, bytes);

                await updateLatestTask;
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }

        private async Task DeleteExpiredReferenceDateLatestPreferReplicaAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            DateTime referenceDate, int limit)
        {
            var referenceDateTimestampThreshold =
                referenceDate.ToUnixTimeMilliSeconds();

            var redisKeyLatestReferenceDate = $"ReferenceDateLatest:{tenantRegistryId}:{entityAnalysisModelGuid:N}";

            await foreach (var latestCount in resilientRedisResilientRedisDatabase.HashScanAsync(redisKeyLatestReferenceDate, flags: CommandFlags.PreferReplica))
            {
                var redisKey = $"ReferenceDateLatest:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{latestCount.Name}";

                while (true)
                {
                    var expiredSortedSetEntries = await resilientRedisResilientRedisDatabase.SortedSetRangeByScoreWithScoresAsync(
                        redisKey,
                        Int64.MinValue,
                        referenceDateTimestampThreshold,
                        skip: 0,
                        take: limit,
                        flags: CommandFlags.PreferReplica
                    ).ConfigureAwait(false);

                    if (expiredSortedSetEntries.Length == 0)
                    {
                        break;
                    }

                    var sortedSetExpiredCount = expiredSortedSetEntries.Length;
                    var expiredSortedSetMinTimestamp = (long)expiredSortedSetEntries.FirstOrDefault().Score;
                    var expiredSortedSetMaxTimestamp = (long)expiredSortedSetEntries.LastOrDefault().Score;

                    var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(postgresConnectionString, log);
                    try
                    {
                        var cachePayloadLatestRemovalBatchRepository = new CachePayloadLatestRemovalBatchRepository(dbContext);
                        var cachePayloadLatestRemovalBatch = await cachePayloadLatestRemovalBatchRepository.InsertAsync(new CachePayloadLatestRemovalBatch
                        {
                            EntityAnalysisModelGuid = entityAnalysisModelGuid,
                            ReferenceDate = referenceDate,
                            Key = latestCount.Name,
                            ExpiredSortedSetCount = sortedSetExpiredCount,
                            FirstExpiredSortedSetReferenceDate = expiredSortedSetMinTimestamp.FromUnixTimeMilliSeconds(),
                            LastExpiredSortedSetReferenceDate = expiredSortedSetMaxTimestamp.FromUnixTimeMilliSeconds()
                        });

                        var bulkInsertEntries = expiredSortedSetEntries.Select(expiredSortedSetEntry => new CachePayloadLatestRemovalBatchEntry
                            {
                                CachePayloadLatestRemovalBatchId = cachePayloadLatestRemovalBatch.Id,
                                Value = expiredSortedSetEntry.Element,
                                ReferenceDate = ((long)expiredSortedSetEntry.Score).FromUnixTimeMilliSeconds()
                            })
                            .ToList();

                        var redisValuesToDelete = expiredSortedSetEntries.Select(s => new RedisValue(s.Element)).ToArray();
                        var cachePayloadLatestRemovalBatchEntryRepository = new CachePayloadLatestRemovalBatchKeyEntryRepository(dbContext);

                        var tasks = new List<Task<TimedTaskResult>>
                        {
                            TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.SortedSetRemoveReferenceDateLatest, async () => await BatchSortedSetRemoveAsync(resilientRedisResilientRedisDatabase, redisKey, redisValuesToDelete.ToArray())),
                            TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.HashDeletePayloadLatest, async () => await BatchHashDeleteAsync(resilientRedisResilientRedisDatabase, $"PayloadLatest:{tenantRegistryId.ToString()}:{entityAnalysisModelGuid:N}", redisValuesToDelete))
                        };

                        var completedTasks = await Task.WhenAll(tasks).ConfigureAwait(false);
                        await TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.BulkInsertCachePayloadLatestRemovalBatchEntry, async () => await cachePayloadLatestRemovalBatchEntryRepository.BulkCopyAsync(bulkInsertEntries));

                        var cachePayloadLatestRemovalBatchResponseTimeRepository = new CachePayloadLatestRemovalBatchResponseTimeRepository(dbContext);

                        // ReSharper disable once MethodSupportsCancellation
                        await cachePayloadLatestRemovalBatchResponseTimeRepository.BulkCopyAsync(AggregateResponseTimesForBulkInsert(completedTasks, cachePayloadLatestRemovalBatch));
                        await cachePayloadLatestRemovalBatchRepository.FinishAsync(cachePayloadLatestRemovalBatch.Id);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"DeleteExpiredReferenceDateLatestAsync has created an error {ex}");
                    }
                    finally
                    {
                        await dbContext.CloseAsync();
                        await dbContext.DisposeAsync();
                    }

                    if (expiredSortedSetEntries.Length < limit)
                    {
                        break;
                    }
                }
            }
        }

        private static async Task BatchSortedSetRemoveAsync(IHybridResilientRedisDatabase db, RedisKey key, IEnumerable<RedisValue> values)
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

        private static async Task BatchHashDeleteAsync(IHybridResilientRedisDatabase db, RedisKey key, IEnumerable<RedisValue> fields)
        {
            const int batchSize = 1000;
            var fieldsArray = fields.ToArray();

            for (var i = 0; i < fieldsArray.Length; i += batchSize)
            {
                var batch = fieldsArray.Skip(i).Take(batchSize).ToArray();
                await db.HashDeleteAsync(key, batch).ConfigureAwait(false);
                await Task.Yield();
                await Task.Delay(1);
            }
        }

        private static List<CachePayloadLatestRemovalBatchResponseTime> AggregateResponseTimesForBulkInsert(TimedTaskResult[] tasks, CachePayloadLatestRemovalBatch cachePayloadLatestRemovalBatch)
        {
            var groupByComputeTime = tasks.GroupBy(g => g.TaskType).Select(s => new CachePayloadLatestRemovalBatchResponseTime
            {
                TaskTypeId = (int)s.Key,
                ResponseTime = s.Sum(a => a.ComputeTime),
                CachePayloadLatestRemovalBatchId = cachePayloadLatestRemovalBatch.Id
            }).ToList();

            return groupByComputeTime;
        }
    }
}
