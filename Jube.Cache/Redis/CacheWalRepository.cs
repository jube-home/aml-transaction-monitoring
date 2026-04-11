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
    using ResilientRedisConnection;
    using StackExchange.Redis;

    public class CacheWalRepository(
        ResilientRedisDatabase redisDatabase,
        ILog log,
        CommandFlags commandFlag = CommandFlags.None) : ICacheWalRepository
    {
        public async Task InsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, Guid entityAnalysisModelInstanceEntryGuid, string node, byte[] bytes)
        {
            try
            {
                var redisKey = $"Wal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{node}";
                var redisHSetKey = $"{entityAnalysisModelInstanceEntryGuid:N}";
                await redisDatabase.HashSetAsync(redisKey, redisHSetKey, bytes, When.Always, commandFlag).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }

        public async Task FlushWalAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string node, Guid[] entityAnalysisModelInstanceEntryGuids)
        {
            try
            {
                var redisKey = $"Wal:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{node}";
                await redisDatabase.HashDeleteAsync(redisKey,
                    entityAnalysisModelInstanceEntryGuids.Select(x => (RedisValue)x.ToString("N"))
                        .ToArray(), commandFlag).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }
    }
}
