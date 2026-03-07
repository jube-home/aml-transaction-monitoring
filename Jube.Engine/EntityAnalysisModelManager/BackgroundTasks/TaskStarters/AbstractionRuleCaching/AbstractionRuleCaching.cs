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
    using Dictionary;
    using DynamicEnvironment;
    using EntityAnalysisModel;
    using EntityAnalysisModel.Models.Models;
    using EntityAnalysisModelInvoke.Context.Extensions.ReflectionHelpers;
    using EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
    using Microsoft.VisualBasic;

    public static class AbstractionRuleCaching
    {
        public static async Task StartAsync(EntityAnalysisModel entityAnalysisModel,
            DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            if (entityAnalysisModel.Services.Log.IsInfoEnabled)
            {
                entityAnalysisModel.Services.Log.Info(
                    "Entity Start: Will try and make a connection to the Database to create the Search Key Cache.");
            }

            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), entityAnalysisModel.Services.Log);
            var abstractionRuleCachingCacheService = new AbstractionRuleCachingCacheService(entityAnalysisModel);
            var abstractionRuleCachingRepository = new AbstractionRuleCachingRepository(dbContext, entityAnalysisModel);
            var abstractionRuleCachingQueries = new AbstractionRuleCachingQueries(dbContext, entityAnalysisModel);

            try
            {
                token.ThrowIfCancellationRequested();

                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} starting to loop around all of the Grouping keys that have been synchronise.");
                }

                var processedGroupingValues = 0;
                foreach (var (key, value) in entityAnalysisModel.Collections.DistinctSearchKeys)
                {
                    token.ThrowIfCancellationRequested();

                    var ready = AbstractionRuleUtilities.IsSearchKeyReady(entityAnalysisModel, value, entityAnalysisModel.Services.Log);

                    if (!ready)
                    {
                        continue;
                    }

                    var toDate = DateTime.Now;
                    var entityAnalysisModelsSearchKeyCalculationInstanceId =
                        await abstractionRuleCachingRepository.InsertEntityAnalysisModelsSearchKeyCalculationInstancesAsync(value, toDate, token).ConfigureAwait(false);

                    var groupingValues = await abstractionRuleCachingQueries.GetDistinctListOfGroupingValuesAsync(value, toDate, token).ConfigureAwait(false);

                    if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                    {
                        entityAnalysisModel.Services.Log.Info(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {key} has found {groupingValues.Count} grouping values.");
                    }

                    await abstractionRuleCachingRepository.UpdateEntityAnalysisModelsSearchKeyCalculationInstancesDistinctValuesAsync(
                        entityAnalysisModelsSearchKeyCalculationInstanceId, groupingValues.Count, token).ConfigureAwait(false);

                    var expires = await abstractionRuleCachingCacheService.CacheServiceGetExpiredCacheKeysAsync(value).ConfigureAwait(false);

                    if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                    {
                        entityAnalysisModel.Services.Log.Info(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {key} has found {expires.Count} expires values.");
                    }

                    await abstractionRuleCachingRepository.UpdateEntityAnalysisModelsSearchKeyCalculationInstancesExpiredSearchKeyCacheCountAsync(
                        entityAnalysisModelsSearchKeyCalculationInstanceId, expires.Count, token).ConfigureAwait(false);

                    groupingValues = AbstractionRuleUtilities.AddExpiredToGroupingValues(entityAnalysisModel, value, expires, groupingValues, token);

                    if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                    {
                        entityAnalysisModel.Services.Log.Info(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {key} has found {expires.Count} grouping values in total including expires.");
                    }

                    if (groupingValues.Count > 0)
                    {
                        if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                        {
                            entityAnalysisModel.Services.Log.Info(
                                $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {key} has {groupingValues.Count} values.  For each value,  records will be returned using it as key and rules executed against the returned records.");
                        }

                        foreach (var groupingValue in groupingValues)
                        {
                            token.ThrowIfCancellationRequested();

                            var entityInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                            {
                                Abstraction = new PooledDictionary<string, double>(entityAnalysisModel.Collections.ModelAbstractionRules.Count),
                                Activation = new PooledDictionary<string, EntityModelActivationRulePayload>(entityAnalysisModel.Collections.ModelActivationRules.Count),
                                Tag = new PooledDictionary<string, double>(entityAnalysisModel.Collections.EntityAnalysisModelTags.Count),
                                Dictionary = new PooledDictionary<string, double>(entityAnalysisModel.Dependencies.KvpDictionaries.Count),
                                TtlCounter = new PooledDictionary<string, double>(entityAnalysisModel.Collections.ModelTtlCounters.Count),
                                Sanction = new PooledDictionary<string, double>(entityAnalysisModel.Collections.EntityAnalysisModelSanctions.Count),
                                AbstractionCalculation = new PooledDictionary<string, double>(entityAnalysisModel.Collections.EntityAnalysisModelAbstractionCalculations.Count),
                                HttpAdaptation = new PooledDictionary<string, double>(entityAnalysisModel.Collections.EntityAnalysisModelAdaptations.Count),
                                ExhaustiveAdaptation = new PooledDictionary<string, double>(entityAnalysisModel.Collections.ExhaustiveModels.Count),
                                InvokeTaskPerformance = new InvokeTaskPerformance
                                {
                                    ComputeTimes = new InvokeTasksPerformance()
                                }
                            };

                            var abstractionRuleMatches = new Dictionary<int, List<DictionaryNoBoxing<string>>>();

                            if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                            {
                                entityAnalysisModel.Services.Log.Info(
                                    $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {key} is processing grouping value {groupingValue}.");
                            }

                            if (!String.IsNullOrEmpty(groupingValue))
                            {
                                var entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId =
                                    await abstractionRuleCachingRepository.InsertEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesAsync(
                                        entityAnalysisModelsSearchKeyCalculationInstanceId, groupingValue, token).ConfigureAwait(false);

                                var documents = await abstractionRuleCachingQueries.GetAllForKeyAsync(value, groupingValue, token).ConfigureAwait(false);

                                await abstractionRuleCachingRepository.UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesEntriesCountAsync(
                                    entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId,
                                    documents.Count, token).ConfigureAwait(false);

                                abstractionRuleMatches =
                                    await ProcessAllAbstractionRulesAsync(entityAnalysisModel, value, documents, abstractionRuleMatches, token).ConfigureAwait(false);

                                await abstractionRuleCachingRepository.UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesAbstractionRulesMatchesAsync(
                                    entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId, token).ConfigureAwait(false);

                                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                                {
                                    entityAnalysisModel.Services.Log.Info(
                                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {key}, the matches will now be aggregated by looping through each abstraction rule.");
                                }

                                foreach (var abstractionRuleMatch in abstractionRuleMatches)
                                {
                                    token.ThrowIfCancellationRequested();

                                    if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                                    {
                                        entityAnalysisModel.Services.Log.Info(
                                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {key} for abstraction rule {abstractionRuleMatch.Key} , the matches will now be aggregated by looping through each abstraction rule.");
                                    }

                                    try
                                    {
                                        var abstractionRule = entityAnalysisModel.Collections.ModelAbstractionRules.Find(x =>
                                            x.Id == abstractionRuleMatch.Key);

                                        if (abstractionRule != null)
                                        {
                                            var abstractionValue = AbstractionRuleUtilities.GetAggregateValue(entityAnalysisModel, value, groupingValue,
                                                abstractionRuleMatches, abstractionRuleMatch, abstractionRule,
                                                entityInstanceEntryPayload);

                                            await abstractionRuleCachingCacheService.CacheServiceUpsertOrDeleteSearchKeyValueAsync(value, groupingValue,
                                                abstractionRuleMatch, abstractionRule, abstractionValue).ConfigureAwait(false);

                                            if (entityAnalysisModel.Flags.EnableRdbmsArchive)
                                            {
                                                await abstractionRuleCachingRepository.InsertToArchiveAsync(
                                                    entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId,
                                                    value, groupingValue, abstractionRule, abstractionValue, token).ConfigureAwait(false);
                                            }
                                        }
                                        else
                                        {
                                            entityAnalysisModel.Services.Log.Error(
                                                $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {key} for abstraction rule {abstractionRuleMatch.Key}  could not find full details of the abstraction rule.");
                                        }
                                    }
                                    catch (Exception ex) when (ex is not OperationCanceledException)
                                    {
                                        entityAnalysisModel.Services.Log.Error(
                                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {key} for abstraction rule {abstractionRuleMatch.Key} is in error as {ex}.");
                                    }
                                }

                                await abstractionRuleCachingRepository.UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesCompletedAsync(
                                    entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId, token).ConfigureAwait(false);
                            }
                            else
                            {
                                entityAnalysisModel.Services.Log.Error(
                                    $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {key} is empty.");
                            }

                            processedGroupingValues += 1;
                            await abstractionRuleCachingRepository.UpdateEntityAnalysisModelsSearchKeyCalculationInstancesDistinctValuesProcessedValuesCountAsync(
                                entityAnalysisModelsSearchKeyCalculationInstanceId,
                                processedGroupingValues, token).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        entityAnalysisModel.Services.Log.Error(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and Grouping Key {key} is empty.");
                    }

                    await abstractionRuleCachingRepository.UpdateEntityAnalysisModelsSearchKeyCalculationInstancesCompletedAsync(
                        entityAnalysisModelsSearchKeyCalculationInstanceId, token).ConfigureAwait(false);
                }

                entityAnalysisModel.Cache.LastModelSearchKeyCacheWritten = DateTime.Now;

                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} has recorded the date of last rule cache.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error(
                    $"AbstractionRuleCachingAsync: For model {entityAnalysisModel.Instance.Id} has produced an error as {ex}.");
            }
            finally
            {
                await dbContext.CloseAsync(token).ConfigureAwait(false);
                await dbContext.DisposeAsync(token).ConfigureAwait(false);
            }
        }

        private static Task<Dictionary<int, List<DictionaryNoBoxing<string>>>> ProcessAllAbstractionRulesAsync(EntityAnalysisModel entityAnalysisModel,
            DistinctSearchKey distinctSearchKey,
            List<DictionaryNoBoxing<string>> documents,
            Dictionary<int, List<DictionaryNoBoxing<string>>> abstractionRuleMatches, CancellationToken token = default)
        {
            Dictionary<int, List<DictionaryNoBoxing<string>>> values = null;
            try
            {
                var logicHashMatches = new Dictionary<string, List<DictionaryNoBoxing<string>>>();

                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} will step through all abstraction rules.");
                }

                foreach (var evaluateAbstractionRule in entityAnalysisModel.Collections.ModelAbstractionRules)
                {
                    token.ThrowIfCancellationRequested();

                    if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                    {
                        entityAnalysisModel.Services.Log.Info(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} has a search type > 1 and matches on teh current grouping key.  The search type is {evaluateAbstractionRule.Search} and the rule grouping key is{evaluateAbstractionRule.SearchKey}.");
                    }

                    if (evaluateAbstractionRule.Search &&
                        evaluateAbstractionRule.SearchKey == distinctSearchKey.SearchKey)
                    {
                        if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                        {
                            entityAnalysisModel.Services.Log.Info(
                                $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  As some rule logic can be common across a number of rules,  a check will be made to see if this logic has already been executed using the hash of the rule logic as {evaluateAbstractionRule.LogicHash}.");
                        }

                        List<DictionaryNoBoxing<string>> matches;
                        if (logicHashMatches.TryGetValue(evaluateAbstractionRule.LogicHash, out var match))
                        {
                            matches = match;

                            if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                            {
                                entityAnalysisModel.Services.Log.Info(
                                    $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  Rule logic as {evaluateAbstractionRule.LogicHash} has already been executed so it will simply return the {matches.Count} records already having been matched on this logic.");
                            }
                        }
                        else
                        {
                            if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                            {
                                entityAnalysisModel.Services.Log.Info(
                                    $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  Rule logic as {evaluateAbstractionRule.LogicHash} has not already been executed so it be filtered using the rule logic.");
                            }

                            matches = documents.FindAll(x =>
                                ReflectRuleHelper.Execute(evaluateAbstractionRule, entityAnalysisModel, x, null, entityAnalysisModel.Services.Log));
                            logicHashMatches.Add(evaluateAbstractionRule.LogicHash, matches);

                            if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                            {
                                entityAnalysisModel.Services.Log.Info(
                                    $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  Rule logic as {evaluateAbstractionRule.LogicHash} has been executed for the first time and returned {matches.Count} records already.  It has been added to the cache using the logic hash as a key.");
                            }
                        }

                        var historyThresholdDate =
                            DateAndTime.DateAdd(evaluateAbstractionRule.AbstractionRuleAggregationFunctionIntervalType,
                                evaluateAbstractionRule.AbstractionHistoryIntervalValue * -1, DateTime.Now);

                        if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                        {
                            entityAnalysisModel.Services.Log.Info(
                                $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  Rule logic as {evaluateAbstractionRule.LogicHash}, {matches.Count} records will be filtered based on the history criteria.  It has been added to the cache using the logic hash as a key.  The interval for the rule logic is {evaluateAbstractionRule.AbstractionHistoryIntervalType} and the value is {evaluateAbstractionRule.AbstractionHistoryIntervalValue}.  Records will be return where the date is between {historyThresholdDate} and now.");
                        }

                        var finalMatches = matches.FindAll(x =>
                            x["CreatedDate"].AsDateTime() >= historyThresholdDate &&
                            x["CreatedDate"].AsDateTime() <= DateTime.Now);

                        abstractionRuleMatches.Add(evaluateAbstractionRule.Id, []);
                        abstractionRuleMatches[evaluateAbstractionRule.Id] = finalMatches;

                        if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                        {
                            entityAnalysisModel.Services.Log.Info(
                                $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} and abstraction rule {evaluateAbstractionRule.Id}  has a final number of matches of {finalMatches.Count} and has been added to the list of matches");
                        }
                    }

                    if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                    {
                        entityAnalysisModel.Services.Log.Info(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} is moving to the next grouping value.");
                    }
                }

                return Task.FromResult(abstractionRuleMatches);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"ProcessAbstractionRules: has produced an error {ex}");
            }

            return Task.FromResult(values);
        }
    }
}
