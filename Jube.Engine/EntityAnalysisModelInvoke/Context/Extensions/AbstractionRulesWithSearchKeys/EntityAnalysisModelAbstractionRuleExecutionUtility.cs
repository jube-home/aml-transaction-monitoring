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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cache;
    using Dictionary;
    using DynamicEnvironment;
    using EntityAnalysisModelManager.EntityAnalysisModel;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using log4net;
    using Microsoft.VisualBasic;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using ReflectionHelpers;
    using TaskCancellation.TaskHelper;

    public class Execute
    {
        public string AbstractionRuleGroupingKey { get; init; }
        public DistinctSearchKey DistinctSearchKey { get; init; }
        public DictionaryNoBoxing<string> CachePayloadDocument { get; init; }
        public EntityAnalysisModel EntityAnalysisModel { get; init; }
        public EntityAnalysisModelInstanceEntryPayload EntityAnalysisModelInstanceEntryPayload { get; init; }
        public PooledDictionary<string, double> EntityInstanceEntryDictionaryKvPs { get; init; }
        public Dictionary<int, List<DictionaryNoBoxing<string>>> AbstractionRuleMatches { get; init; } = new Dictionary<int, List<DictionaryNoBoxing<string>>>();
        public bool Finished { get; private set; }
        public ILog Log { get; init; }
        public CacheService CacheService { get; init; }
        public DynamicEnvironment DynamicEnvironment { get; set; }
        public List<Task<TimedTaskResult>> PendingWritesTasks { get; init; }

        public async Task StartAsync()
        {
            try
            {
                var limit = EntityAnalysisModel.Cache.CacheTtlLimit < DistinctSearchKey.SearchKeyFetchLimit
                    ? EntityAnalysisModel.Cache.CacheTtlLimit
                    : DistinctSearchKey.SearchKeyFetchLimit;

                PendingWritesTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.CachePayloadInsertAsync, async () => await CacheService.CachePayloadRepository
                    .InsertPayloadJournalAndLedgerAsync(EntityAnalysisModel.Instance.TenantRegistryId,
                        EntityAnalysisModel.Instance.Guid,
                        AbstractionRuleGroupingKey,
                        CachePayloadDocument[AbstractionRuleGroupingKey].AsString(),
                        EntityAnalysisModelInstanceEntryPayload.ReferenceDate,
                        EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid).ConfigureAwait(false)));

                var documentsRemovedInterned = await CacheService.CachePayloadRepository.GetExcludeCurrentAsync(
                    EntityAnalysisModel.Instance.TenantRegistryId,
                    EntityAnalysisModel.Instance.Guid,
                    AbstractionRuleGroupingKey,
                    CachePayloadDocument[AbstractionRuleGroupingKey].AsString(),
                    limit,
                    EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                ).ConfigureAwait(false);

                var documents = ParseDocuments(documentsRemovedInterned);
                documents.Add(CachePayloadDocument);

                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has created a filter for cache where {AbstractionRuleGroupingKey} has added the current transaction to the records,  so there are now {documents.Count} records for evaluation.  The records will now be matched against the Abstraction rules where this {AbstractionRuleGroupingKey} is expressed and the rule is marked as a history rule (else it will be done later as a basic rule).");
                }

                var logicHashMatches = new ConcurrentDictionary<string, List<DictionaryNoBoxing<string>>>();
                var abstractionRuleMatches = new ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>>();

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 4
                };

                var rulesToEvaluate = EntityAnalysisModel.Collections.ModelAbstractionRules
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
                            matches = documents.FindAll(x => ReflectRuleHelper.Execute(
                                evaluateAbstractionRule,
                                EntityAnalysisModel, x,
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
                            x[EntityAnalysisModel.References.ReferenceDateName].AsDateTime() >= fromDate &&
                            x[EntityAnalysisModel.References.ReferenceDateName].AsDateTime() <=
                            EntityAnalysisModelInstanceEntryPayload.ReferenceDate);

                        abstractionRuleMatches[evaluateAbstractionRule.Id] = finalMatches;

                        if (Log.IsInfoEnabled)
                        {
                            Log.Info(
                                $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} abstraction rule id {evaluateAbstractionRule.Id} has {finalMatches.Count} final matches.");
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        if (Log.IsInfoEnabled)
                        {
                            Log.Info(
                                $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} abstraction rule id {evaluateAbstractionRule.Id} exception {ex}.");
                        }
                    }
                });

                foreach (var kvp in abstractionRuleMatches)
                {
                    AbstractionRuleMatches[kvp.Key] = kvp.Value;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
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
        
        private List<DictionaryNoBoxing<string>> ParseDocuments(List<DictionaryNoBoxing<int>> documentsRemovedInterned)
        {
            var entityAnalysisModelRequestXPathsByIndex = EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths
                .Where(f => f.Cache)
                .ToDictionary(f => f.CacheIndexId);

            var inlineScriptPropertyAttributeByIndex = EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts
                .SelectMany(s => s.EntityAnalysisModelInlineScriptPropertyAttributes)
                .Where(a => a.Value.CacheIndexId != null)
                .ToDictionary(a => a.Value.CacheIndexId!.Value, a => a.Key);

            var documents = new List<DictionaryNoBoxing<string>>();
            foreach (var documentRemovedInterned in documentsRemovedInterned)
            {
                var document = new DictionaryNoBoxing<string>();
                foreach (var (key, value) in documentRemovedInterned)
                {
                    if (key < 0)
                    {
                        if (key == -1)
                        {
                            document.Add(EntityAnalysisModel.References.ReferenceDateName, value);
                        }
                        continue;
                    }

                    if (inlineScriptPropertyAttributeByIndex.TryGetValue(key, out var attributeName))
                    {
                        document.Add(attributeName, value);
                    }

                    if (entityAnalysisModelRequestXPathsByIndex.TryGetValue(key, out var xpath))
                    {
                        document.Add(xpath.Name, value);
                    }
                }
                documents.Add(document);
            }
            return documents;
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
