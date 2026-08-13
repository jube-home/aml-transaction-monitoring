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

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.AbstractionRuleCaching
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cache.Redis;
    using Dictionary;
    using EntityAnalysisModel;
    using EntityAnalysisModel.Models.Models;

    public class AbstractionRuleCachingCacheService(EntityAnalysisModel entityAnalysisModel)
    {
        public async Task<List<string>> CacheServiceGetExpiredCacheKeysAsync(DistinctSearchKey distinctSearchKey)
        {
            List<string> values = null;

            try
            {
                if (!entityAnalysisModel.Dependencies.LastAbstractionRuleCache.TryGetValue(distinctSearchKey.SearchKey, out var value))
                {
                    return [];
                }

                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} has a interval value of {distinctSearchKey.SearchKeyCacheTtlValue} and an interval of {distinctSearchKey.SearchKeyCacheInterval}.  Calculating the threshold for grouping keys that have expired.");
                }

                var deleteLineCacheKeys = distinctSearchKey.SearchKeyCacheTtlInterval switch
                {
                    "s" => value.AddSeconds(distinctSearchKey.SearchKeyCacheTtlValue * -1),
                    "n" => value.AddMinutes(distinctSearchKey.SearchKeyCacheTtlValue * -1),
                    "h" => value.AddHours(distinctSearchKey.SearchKeyCacheTtlValue * -1),
                    _ => value.AddDays(distinctSearchKey.SearchKeyCacheTtlValue * -1)
                };

                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} the date threshold for cache keys that have expired is {deleteLineCacheKeys}.");
                }

                values = await entityAnalysisModel.Services.CacheService.CachePayloadLatestRepository.GetDistinctKeysPreferReplicaAsync(
                    entityAnalysisModel.Instance.TenantRegistryId, entityAnalysisModel.Instance.Guid,
                    distinctSearchKey.SearchKey,
                    deleteLineCacheKeys).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"CacheServiceGetExpiredCacheKeysAsync: has produced an error {ex}");
            }

            return values;
        }

        public async Task CacheServiceUpsertOrDeleteSearchKeyValueAsync(DistinctSearchKey distinctSearchKey,
            string groupingValue,
            KeyValuePair<int, List<DictionaryNoBoxing<string>>> abstractionRuleMatch,
            EntityAnalysisModelAbstractionRule abstractionRule,
            double abstractionValue)
        {
            try
            {
                var value = await CacheServiceFindCacheKeyValueEntryAsync(entityAnalysisModel.Services.CacheService.CacheAbstractionRepository,
                    distinctSearchKey, groupingValue, abstractionRuleMatch, abstractionRule,
                    abstractionValue).ConfigureAwait(false);

                if (value == null)
                {
                    if (abstractionValue > 0)
                    {
                        await CacheServiceInsertSearchKeyValueAsync(entityAnalysisModel.Services.CacheService.CacheAbstractionRepository,
                            distinctSearchKey, groupingValue,
                            abstractionRuleMatch, abstractionRule,
                            abstractionValue).ConfigureAwait(false);
                    }
                }
                else
                {
                    await CacheServiceUpdateOrDeleteSearchKeyValueAsync(entityAnalysisModel.Services.CacheService.CacheAbstractionRepository, distinctSearchKey, groupingValue,
                        abstractionRule,
                        abstractionValue).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"CacheServiceUpsertOrDeleteSearchKeyValueAsync: has produced an error {ex}");
            }
        }

        private async Task<double?> CacheServiceFindCacheKeyValueEntryAsync(CacheAbstractionRepository cacheAbstractionRepository,
            DistinctSearchKey distinctSearchKey, string groupingValue,
            KeyValuePair<int, List<DictionaryNoBoxing<string>>> abstractionRuleMatch,
            EntityAnalysisModelAbstractionRule abstractionRule,
            double abstractionValue)
        {
            double? value = null;
            try
            {
                value = await cacheAbstractionRepository.GetPreferReplicaAsync(
                    entityAnalysisModel.Instance.TenantRegistryId, entityAnalysisModel.Instance.Guid, abstractionRule.Name, distinctSearchKey.SearchKey, groupingValue).ConfigureAwait(false);

                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRuleMatch.Key}  has returned aggregated value of {abstractionValue}.  The cache has returned for rule name {abstractionRule.Name} with {value == null} null document.  An upsert will now take place.");
                }

                return value;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"CacheServiceFindCacheKeyValueEntryAsync: has produced an error {ex}");
            }

            return value;
        }

        private async Task CacheServiceInsertSearchKeyValueAsync(CacheAbstractionRepository cacheAbstractionRepository,
            DistinctSearchKey distinctSearchKey, string groupingValue,
            KeyValuePair<int, List<DictionaryNoBoxing<string>>> abstractionRuleMatch,
            EntityAnalysisModelAbstractionRule abstractionRule,
            double abstractionValue)
        {
            try
            {
                var (key, _) = abstractionRuleMatch;

                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {key}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}. As there are no existing cache values, it will be inserted.");
                }

                await cacheAbstractionRepository.UpsertAsync(entityAnalysisModel.Instance.TenantRegistryId, entityAnalysisModel.Instance.Guid, distinctSearchKey.SearchKey,
                    groupingValue, abstractionRule.Name, abstractionValue).ConfigureAwait(false);

                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {key}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As there are no existing cache values, it has been updated.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"CacheServiceInsertSearchKeyValueAsync: has produced an error {ex}");
            }
        }

        private async Task CacheServiceUpdateOrDeleteSearchKeyValueAsync(CacheAbstractionRepository cacheAbstractionRepository,
            DistinctSearchKey distinctSearchKey, string groupingValue,
            EntityAnalysisModelAbstractionRule abstractionRule,
            double abstractionValue)
        {
            try
            {
                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As there are no existing cache values, it will be updated or deleted.");
                }

                if (abstractionValue == 0)
                {
                    if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                    {
                        entityAnalysisModel.Services.Log.Info(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As the abstraction value is zero, it will be deleted to save storage.");
                    }

                    await cacheAbstractionRepository.DeleteAsync(entityAnalysisModel.Instance.TenantRegistryId, entityAnalysisModel.Instance.Guid, distinctSearchKey.SearchKey,
                        groupingValue, abstractionRule.Name).ConfigureAwait(false);

                    if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                    {
                        entityAnalysisModel.Services.Log.Info(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As the abstraction value is zero, it has been deleted to save storage.");
                    }
                }
                else
                {
                    if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                    {
                        entityAnalysisModel.Services.Log.Info(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As the abstraction value is not zero, it will be updated.");
                    }

                    await cacheAbstractionRepository.UpsertAsync(entityAnalysisModel.Instance.TenantRegistryId, entityAnalysisModel.Instance.Guid, distinctSearchKey.SearchKey,
                        groupingValue, abstractionRule.Name, abstractionValue).ConfigureAwait(false);

                    if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                    {
                        entityAnalysisModel.Services.Log.Info(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As the abstraction value is not zero, it has been updated.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"CacheServiceUpdateOrDeleteSearchKeyValueAsync: has produced an error {ex}");
            }
        }
    }
}
