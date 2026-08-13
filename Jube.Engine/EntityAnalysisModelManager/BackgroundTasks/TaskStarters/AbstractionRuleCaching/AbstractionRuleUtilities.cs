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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using Dictionary;
    using EntityAnalysisModel;
    using EntityAnalysisModel.Models.Models;
    using EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys;
    using EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using log4net;
    using Microsoft.VisualBasic;

    public static class AbstractionRuleUtilities
    {
        public static bool IsSearchKeyReady(EntityAnalysisModel entityAnalysisModel, DistinctSearchKey distinctSearchKey, ILog log)
        {
            var ready = false;

            try
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} Checking to see if grouping key {distinctSearchKey.SearchKey} is a search key.  It has a search key value of {distinctSearchKey.SearchKeyCache}.");
                }

                if (distinctSearchKey.SearchKeyCache)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} grouping key {distinctSearchKey.SearchKey} is a search key. A check will now be performed to understand when this abstraction rule key was last calculated.");
                    }

                    if (entityAnalysisModel.Dependencies.LastAbstractionRuleCache.TryGetValue(distinctSearchKey.SearchKey, out var value))
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} will calculate the date threshold for this grouping key to be run.  It was last run on {entityAnalysisModel.Dependencies.LastAbstractionRuleCache.ContainsKey(distinctSearchKey.SearchKey)} and the SearchKey Cache Interval Type is {distinctSearchKey.SearchKeyCacheInterval} and the Search Key Cache Interval Value{distinctSearchKey.SearchKeyCacheValue}.");
                        }

                        var dateThreshold = DateAndTime.DateAdd(distinctSearchKey.SearchKeyCacheInterval,
                            distinctSearchKey.SearchKeyCacheValue, value);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} should next run on {dateThreshold}.");
                        }

                        if (DateTime.UtcNow > dateThreshold)
                        {
                            ready = true;

                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} has been set to a ready state as the threshold has lapsed.");
                            }
                        }
                        else
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} has been set to a unready state as the threshold has not lapsed.");
                            }
                        }
                    }
                    else
                    {
                        ready = true;
                    }
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} has been set to a ready state as it has never been run.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"IsSearchKeyReady: has produced an error {ex}");
            }

            return ready;
        }

        public static List<string> AddExpiredToGroupingValues(EntityAnalysisModel entityAnalysisModel,
            DistinctSearchKey distinctSearchKey,
            IReadOnlyCollection<string> expires,
            List<string> groupingValues, CancellationToken token = default)
        {
            try
            {
                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} has returned {expires.Count} cache keys.  These keys will now be added to the list distinct wise.");
                }

                foreach (var expired in expires)
                {
                    token.ThrowIfCancellationRequested();

                    if (!groupingValues.Contains(expired))
                    {
                        groupingValues.Add(expired);

                        if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                        {
                            entityAnalysisModel.Services.Log.Info(
                                $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} has added the value {expired}.");
                        }
                    }
                    else
                    {
                        if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                        {
                            entityAnalysisModel.Services.Log.Info(
                                $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and grouping key {distinctSearchKey.SearchKey} has not added the value {expired} as it is a duplicate of one already added.");
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"AddExpiredToGroupingValues: has produced an error {ex}");
            }

            return groupingValues;
        }

        public static double GetAggregateValue(EntityAnalysisModel entityAnalysisModel, DistinctSearchKey distinctSearchKey, string groupingValue,
            ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>> abstractionRuleMatches,
            KeyValuePair<int, List<DictionaryNoBoxing<string>>> abstractionRuleMatch,
            EntityAnalysisModelAbstractionRule abstractionRule,
            EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayload)
        {
            var value = 0d;
            try
            {
                var (key, _) = abstractionRuleMatch;

                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {key}  has returned full details of the abstraction rule.");
                }

                value = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                    entityAnalysisModelInstanceEntryPayload, abstractionRuleMatches,
                    abstractionRule, entityAnalysisModel.Services.Log);

                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"Abstraction Rule Caching: For model {entityAnalysisModel.Instance.Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {key}  is returning aggregated value of {value}.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"GetAggregateValue: has produced an error {ex}");
            }

            return value;
        }
    }
}
