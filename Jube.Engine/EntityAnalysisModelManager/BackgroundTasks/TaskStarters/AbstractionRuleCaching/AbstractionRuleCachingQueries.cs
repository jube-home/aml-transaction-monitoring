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
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Query;
    using Dictionary;
    using EntityAnalysisModel;
    using EntityAnalysisModel.Models.Models;

    public class AbstractionRuleCachingQueries(DbContext dbContext, EntityAnalysisModel entityAnalysisModel)
    {
        public async Task<List<string>> GetDistinctListOfGroupingValuesAsync(DistinctSearchKey distinctSearchKey,
            DateTime toDate, CancellationToken token = default)
        {
            List<string> value = null;

            try
            {
                var getArchiveDistinctEntryKeyValue = new GetArchiveDistinctEntryKeyValue(dbContext, entityAnalysisModel.Services.Log);

                if (entityAnalysisModel.Dependencies.LastAbstractionRuleCache.ContainsKey(distinctSearchKey.SearchKey))
                {
                    if (entityAnalysisModel.Services.Log.IsDebugEnabled)
                    {
                        entityAnalysisModel.Services.Log.Info(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} has been set to a ready state and will now check if there has been any new records since it was last calculated on {entityAnalysisModel.Dependencies.LastAbstractionRuleCache[distinctSearchKey.SearchKey]} then bring back a distinct list of all grouping keys.");
                    }

                    value = await getArchiveDistinctEntryKeyValue.ExecuteAsync(entityAnalysisModel.Instance.Guid,
                        distinctSearchKey.SearchKey,
                        entityAnalysisModel.Dependencies.LastAbstractionRuleCache[distinctSearchKey.SearchKey], toDate, token).ConfigureAwait(false);

                    entityAnalysisModel.Dependencies.LastAbstractionRuleCache[distinctSearchKey.SearchKey] = toDate;

                    if (entityAnalysisModel.Services.Log.IsDebugEnabled)
                    {
                        entityAnalysisModel.Services.Log.Debug(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} Abstraction Rule Cache Last Entry Date All has been set to {toDate} and is has been added to a collection for grouping key {distinctSearchKey.SearchKey}.");
                    }
                }
                else
                {
                    if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                    {
                        entityAnalysisModel.Services.Log.Info(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} has been set to a ready state and is now bringing back a distinct list of values for the grouping key.");
                    }

                    value = await getArchiveDistinctEntryKeyValue.ExecuteAsync(entityAnalysisModel.Instance.Guid, distinctSearchKey.SearchKey, token).ConfigureAwait(false);

                    if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                    {
                        entityAnalysisModel.Services.Log.Info(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} Abstraction Rule Cache Last Entry Date All has been set to {toDate} and is has been updated to a collection for grouping key {distinctSearchKey.SearchKey}.");
                    }
                }

                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} has returned {value.Count} records when looking for distinct values for this grouping key.  There is a list of grouping keys to be evaluated,  now a check will be made for those that need to be evaluated again.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"GetDistinctListOfGroupingValuesAsync: has produced an error {ex}");
            }

            return value;
        }

        public async Task<List<DictionaryNoBoxing<string>>> GetAllForKeyAsync(DistinctSearchKey distinctSearchKey, string groupingValue, CancellationToken token = default)
        {
            List<DictionaryNoBoxing<string>> values = null;
            try
            {
                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For grouping key {distinctSearchKey.SearchKey} is processing grouping value {groupingValue} returning the top {distinctSearchKey.SearchKeyCacheFetchLimit}.");
                }

                var getArchiveSqlByKeyValueLimitQuery = new GetArchiveSqlByKeyValueLimitQuery(dbContext, entityAnalysisModel.Services.Log);

                var cachePayloadSql = "select * from (" + distinctSearchKey.SqlSelect + distinctSearchKey.SqlSelectFrom +
                                      distinctSearchKey.SqlSelectOrderBy + ") c order by 2;";

                if (distinctSearchKey.SearchKeyCacheSample)
                {
                    values = await getArchiveSqlByKeyValueLimitQuery.ExecuteAsync(
                        cachePayloadSql, distinctSearchKey.SearchKey,
                        groupingValue, "RANDOM()", distinctSearchKey.SearchKeyCacheFetchLimit, token).ConfigureAwait(false);

                    if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                    {
                        entityAnalysisModel.Services.Log.Info(
                            $"Abstraction Rule Caching: For grouping key {distinctSearchKey.SearchKey} retrieved grouping value {groupingValue} returning the top {distinctSearchKey.SearchKeyCacheFetchLimit} records for the grouping key.  There are {values.Count} ordered randomly.");
                    }
                }
                else
                {
                    values = await getArchiveSqlByKeyValueLimitQuery.ExecuteAsync(
                        cachePayloadSql, distinctSearchKey.SearchKey,
                        groupingValue, "CreatedDate", distinctSearchKey.SearchKeyCacheFetchLimit, token).ConfigureAwait(false);

                    if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                    {
                        entityAnalysisModel.Services.Log.Info(
                            $"Abstraction Rule Caching: For grouping key {distinctSearchKey.SearchKey} retrieved grouping value {groupingValue} returning the top {distinctSearchKey.SearchKeyCacheFetchLimit} records for the grouping key.  There are {values.Count} ordered by CreatedDate desc.");
                    }

                    values.Reverse();
                }

                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For grouping key {distinctSearchKey.SearchKey} retrieved grouping value {groupingValue} returning the top {distinctSearchKey.SearchKeyCacheFetchLimit} records for the grouping key.  There are {values.Count} having been finalised.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"GetAllForKeyAsync: has produced an error {ex}");
            }

            return values;
        }
    }
}
