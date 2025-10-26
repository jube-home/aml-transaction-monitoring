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
    using Interfaces;
    using log4net;
    using StackExchange.Redis;

    public class CacheTtlCounterRepository(
        IDatabaseAsync redisDatabase,
        ILog log,
        CommandFlags commandFlag = CommandFlags.FireAndForget) : ICacheTtlCounterRepository
    {
        public async Task DecrementTtlCounterCacheAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            Guid entityAnalysisModelTtlCounterGuid,
            string dataName, string dataValue, int decrement)
        {
            try
            {
                var redisKey =
                    $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}";
                var redisHSetKey = $"{dataValue}";

                await redisDatabase.HashDecrementAsync(redisKey, redisHSetKey, decrement,
                    commandFlag).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }

        public async Task<int> GetByNameDataNameDataValueAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            Guid entityAnalysisModelTtlCounterGuid, string dataName, string dataValue)
        {
            try
            {
                var redisKey =
                    $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}";
                var redisHSetKey = $"{dataValue}";
                return (int)await redisDatabase.HashGetAsync(redisKey, redisHSetKey).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }

            return 0;
        }

        public async Task IncrementTtlCounterCacheAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string dataName,
            string dataValue,
            Guid entityAnalysisModelTtlCounterGuid, int increment, DateTime referenceDate)
        {
            try
            {
                var redisKey =
                    $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}";
                var redisHSetKey = $"{dataValue}";

                await redisDatabase.HashIncrementAsync(redisKey, redisHSetKey, increment,
                    commandFlag).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }
    }
}
