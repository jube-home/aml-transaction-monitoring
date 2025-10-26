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
    using Models;
    using StackExchange.Redis;

    public class CacheAbstractionRepository(
        IDatabaseAsync redisDatabase,
        ILog log,
        CommandFlags commandFlag = CommandFlags.FireAndForget) : ICacheAbstractionRepository
    {
        public async Task DeleteAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string searchKey,
            string searchValue,
            string name)
        {
            try
            {
                var redisKey = $"Abstraction:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{searchKey}:{searchValue}";
                var redisHSetKey = $"{name}";

                await redisDatabase.HashDeleteAsync(redisKey, redisHSetKey, commandFlag).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }

        public async Task UpsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string searchKey,
            string searchValue,
            string name,
            double value)
        {
            try
            {
                var redisKey = $"Abstraction:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{searchKey}:{searchValue}";
                var redisHSetKey = $"{name}";

                await redisDatabase.HashSetAsync(redisKey, redisHSetKey, value,
                    When.Always, commandFlag).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }

        public async Task<double?> Get(int tenantRegistryId, Guid entityAnalysisModelGuid,
            string name, string searchKey,
            string searchValue)
        {
            try
            {
                var redisKey = $"Abstraction:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{searchKey}:{searchValue}";
                var redisHSetKey = $"{name}";
                var redisValue = await redisDatabase.HashGetAsync(redisKey, redisHSetKey).ConfigureAwait(false);

                if (!redisValue.HasValue)
                {
                    return null;
                }
                
                return (double)redisValue;
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }

            return null;
        }

        public async Task<Dictionary<string, double>>
            Get(int tenantRegistryId,
                Guid entityAnalysisModelGuid,
                List<EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue>
                    entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequests)
        {
            var value = new Dictionary<string, double>();
            try
            {
                foreach (var entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest
                         in entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequests)
                {
                    var redisKey =
                        $"Abstraction:{tenantRegistryId}:{entityAnalysisModelGuid:N}:" +
                        $"{entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest.SearchKey}:" +
                        $"{entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest.SearchValue}";
                    var redisHSetKey =
                        $"{entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest.AbstractionRuleName}";

                    var redisValue = await redisDatabase.HashGetAsync(redisKey, redisHSetKey).ConfigureAwait(false);

                    value.TryAdd(entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest.AbstractionRuleName,
                        redisValue.HasValue ? (double)redisValue : 0);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }

            return value;
        }
    }
}
