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

namespace Jube.Engine.Invoke.Abstraction
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cache;
    using Dictionary;
    using DynamicEnvironment;
    using Helpers.TaskHelper;
    using log4net;
    using Microsoft.VisualBasic;
    using Model;
    using Model.Processing.Payload;
    using Reflect;

    public class Execute
    {
        public string AbstractionRuleGroupingKey { get; init; }
        public DistinctSearchKey DistinctSearchKey { get; init; }
        public DictionaryNoBoxing CachePayloadDocument { get; init; }
        public EntityAnalysisModel EntityAnalysisModel { get; init; }
        public EntityAnalysisModelInstanceEntryPayload EntityAnalysisModelInstanceEntryPayload { get; init; }
        public PooledDictionary<string, double> EntityInstanceEntryDictionaryKvPs { get; init; }
        public Dictionary<int, List<DictionaryNoBoxing>> AbstractionRuleMatches { get; init; } = new Dictionary<int, List<DictionaryNoBoxing>>();
        public bool Finished { get; private set; }
        public ILog Log { get; init; }
        public CacheService CacheService { get; set; }
        public DynamicEnvironment DynamicEnvironment { get; set; }
        public List<Task<TimedTaskResult>> PendingWritesTasks { get; set; }

        public async Task StartAsync()
        {
            try
            {
                var limit = EntityAnalysisModel.CacheTtlLimit < DistinctSearchKey.SearchKeyFetchLimit
                    ? EntityAnalysisModel.CacheTtlLimit
                    : DistinctSearchKey.SearchKeyFetchLimit;

                PendingWritesTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.CachePayloadLatestInsertAsync, async () => await CacheService.CachePayloadRepository
                    .InsertAsync(EntityAnalysisModel.TenantRegistryId,
                        EntityAnalysisModel.Guid,
                        AbstractionRuleGroupingKey,
                        CachePayloadDocument[AbstractionRuleGroupingKey].AsString(),
                        EntityAnalysisModelInstanceEntryPayload.Payload,
                        EntityAnalysisModelInstanceEntryPayload.ReferenceDate,
                        EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid)));

                var documents = await CacheService.CachePayloadRepository.GetExcludeCurrent(
                    EntityAnalysisModel.TenantRegistryId,
                    EntityAnalysisModel.Guid,
                    AbstractionRuleGroupingKey,
                    CachePayloadDocument[AbstractionRuleGroupingKey].AsString(),
                    limit,
                    EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                ).ConfigureAwait(false);

                {
                    documents.Add(CachePayloadDocument);

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has created a filter for cache where {AbstractionRuleGroupingKey} has added the current transaction to the records,  so there are now {documents.Count} records for evaluation.  The records will now be matched against the Abstraction rules where this {AbstractionRuleGroupingKey} is expressed and the rule is marked as a history rule (else it will be done later as a basic rule).");
                    }

                    var logicHashMatches = new ConcurrentDictionary<string, List<DictionaryNoBoxing>>();
                    var abstractionRuleMatches = new ConcurrentDictionary<int, List<DictionaryNoBoxing>>();

                    var parallelOptions = new ParallelOptions
                    {
                        // Adjust degree of parallelism as needed
                        //MaxDegreeOfParallelism = Environment.ProcessorCount
                    };

                    var rulesToEvaluate = EntityAnalysisModel.ModelAbstractionRules
                        .FindAll(x => x.SearchKey == AbstractionRuleGroupingKey && x.Search);

                    Parallel.ForEach(rulesToEvaluate, parallelOptions, evaluateAbstractionRule =>
                    {
                        try
                        {
                            if (Log.IsInfoEnabled)
                            {
                                Log.Info(
                                    $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} will process Abstraction Rule {evaluateAbstractionRule.Id}.");
                            }

                            if (!logicHashMatches.TryGetValue(evaluateAbstractionRule.LogicHash, out var matches))
                            {
                                matches = documents.FindAll(x => ReflectRule.Execute(
                                    evaluateAbstractionRule,
                                    EntityAnalysisModel, x,
                                    null,
                                    EntityInstanceEntryDictionaryKvPs, Log));

                                logicHashMatches.TryAdd(evaluateAbstractionRule.LogicHash, matches);

                                if (Log.IsInfoEnabled)
                                {
                                    Log.Info(
                                        $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} abstraction rule id {evaluateAbstractionRule.Id} logic hash {evaluateAbstractionRule.LogicHash} run now and added to logic cache - {matches.Count} matched.");
                                }
                            }
                            else
                            {
                                if (Log.IsInfoEnabled)
                                {
                                    Log.Info(
                                        $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} abstraction rule id {evaluateAbstractionRule.Id} reuse matches from logic cache [{matches.Count}] for logic hash {evaluateAbstractionRule.LogicHash}.");
                                }
                            }

                            var fromDate = GetFromDate(evaluateAbstractionRule);

                            var finalMatches = matches.FindAll(x =>
                                x[EntityAnalysisModel.ReferenceDateName].AsDateTime() >= fromDate &&
                                x[EntityAnalysisModel.ReferenceDateName].AsDateTime() <=
                                EntityAnalysisModelInstanceEntryPayload.ReferenceDate);

                            abstractionRuleMatches[evaluateAbstractionRule.Id] = finalMatches;

                            if (Log.IsInfoEnabled)
                            {
                                Log.Info(
                                    $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} abstraction rule id {evaluateAbstractionRule.Id} has {finalMatches.Count} final matches.");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (Log.IsInfoEnabled)
                            {
                                Log.Info(
                                    $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} abstraction rule id {evaluateAbstractionRule.Id} exception {ex}.");
                            }
                        }
                    });

                    AbstractionRuleMatches.Clear();
                    foreach (var kvp in abstractionRuleMatches)
                    {
                        AbstractionRuleMatches[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has produced an error for grouping key {AbstractionRuleGroupingKey} as {ex}.");
                }
            }
            finally
            {
                Finished = true;
                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has concluded for grouping key {AbstractionRuleGroupingKey}.");
                }
            }
        }

        private DateTime GetFromDate(EntityAnalysisModelAbstractionRule evaluateAbstractionRule)
        {
            var fromDateModel = DateAndTime.DateAdd(
                evaluateAbstractionRule.AbstractionRuleAggregationFunctionIntervalType,
                evaluateAbstractionRule.AbstractionHistoryIntervalValue * -1,
                EntityAnalysisModelInstanceEntryPayload.ReferenceDate);

            var fromDatSearchKey = DateAndTime.DateAdd(
                DistinctSearchKey.SearchKeyTtlInterval,
                DistinctSearchKey.SearchKeyTtlIntervalValue * -1,
                EntityAnalysisModelInstanceEntryPayload.ReferenceDate);

            var fromDate = fromDatSearchKey > fromDateModel ? fromDatSearchKey : fromDateModel;
            return fromDate;
        }
    }
}
