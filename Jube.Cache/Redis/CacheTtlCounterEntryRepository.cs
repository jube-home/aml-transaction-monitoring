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
    using Extensions;
    using Interfaces;
    using log4net;
    using Models;
    using ResilientRedisConnection;
    using StackExchange.Redis;

    public class CacheTtlCounterEntryRepository(
        ResilientRedisDatabase redisDatabase,
        ILog log,
        CommandFlags commandFlag = CommandFlags.FireAndForget) : ICacheTtlCounterEntryRepository
    {
        public async Task<List<ExpiredTtlCounterEntry>>
            GetAllExpiredByTtlCounterAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
                Guid entityAnalysisModelTtlCounterGuid, string dataName, DateTime referenceDate)
        {
            var expired = new List<ExpiredTtlCounterEntry>();
            try
            {
                var redisKeyTtlCounter =
                    $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}";

                await foreach (var dataValue in redisDatabase.HashScanAsync(redisKeyTtlCounter))
                {
                    var redisKeyTtlCounterEntry = $"TtlCounterEntry:{tenantRegistryId}" +
                                                  $":{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}" +
                                                  $":{dataName}:{dataValue.Name}";

                    await foreach (var keyTtlCounterEntry in redisDatabase.HashScanAsync(redisKeyTtlCounterEntry))
                    {
                        var referenceDateTimestamp = Int64.Parse(keyTtlCounterEntry.Name).FromUnixTimeMilliSeconds();
                        if (referenceDateTimestamp >= referenceDate)
                        {
                            continue;
                        }

                        if (keyTtlCounterEntry.Value.HasValue)
                        {
                            expired.Add(new ExpiredTtlCounterEntry
                            {
                                Value = (double)keyTtlCounterEntry.Value,
                                DataName = dataValue.Name,
                                DataValue = dataValue.Value,
                                ReferenceDate = referenceDateTimestamp
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }

            return expired;
        }

        public async Task<double> GetAggregationAsync(int tenantRegistryId,
            Guid entityAnalysisModelGuid, Guid entityAnalysisModelTtlCounterGuid,
            string dataName, string dataValue,
            DateTime referenceDateFrom, DateTime referenceDateTo)
        {
            try
            {
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }

            var referenceDateFromTimestamp = referenceDateFrom.ToUnixTimeMilliSeconds();
            var referenceDateToTimestamp = referenceDateTo.ToUnixTimeMilliSeconds();

            var redisKey =
                $"TtlCounterEntry:{tenantRegistryId}:{entityAnalysisModelGuid:N}" +
                $":{entityAnalysisModelTtlCounterGuid:N}:{dataName}:{dataValue}";

            var sum = 0d;
            await foreach (var hashEntry in redisDatabase.HashScanAsync(redisKey))
            {
                var timestamp = (int)hashEntry.Name;
                if (timestamp >= referenceDateFromTimestamp && timestamp <= referenceDateToTimestamp)
                {
                    sum += (double)hashEntry.Value;
                }
            }

            return sum;

        }

        public async Task UpsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string dataName, string dataValue,
            Guid entityAnalysisModelTtlCounterGuid, DateTime referenceDate, double increment)
        {
            try
            {
                var redisKey =
                    $"TtlCounterEntry:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}:{dataValue}";
                var redisHSetKey = $"{referenceDate.ToUnixTimeMilliSeconds()}";

                await redisDatabase.HashIncrementAsync(redisKey, redisHSetKey, increment, commandFlag).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }

        public async Task DeleteAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            Guid entityAnalysisModelTtlCounterGuid,
            string dataName,
            string dataValue, DateTime referenceDate)
        {
            try
            {
                var redisKey =
                    $"TtlCounterEntry:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}:{dataValue}";
                var redisHSetKey = $"{referenceDate.ToUnixTimeMilliSeconds()}";

                await redisDatabase.HashDeleteAsync(redisKey, redisHSetKey,
                    commandFlag).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }
    }
}
