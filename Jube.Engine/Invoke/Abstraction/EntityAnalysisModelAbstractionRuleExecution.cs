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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jube.Data.Extension;
using Jube.Engine.Invoke.Reflect;
using Jube.Engine.Model;
using Jube.Engine.Model.Processing.Payload;
using log4net;
using Microsoft.VisualBasic;
using StackExchange.Redis;
using Redis = Jube.Data.Cache.Redis;

namespace Jube.Engine.Invoke.Abstraction;

public class Execute
{
    public string AbstractionRuleGroupingKey { get; init; }
    public DistinctSearchKey DistinctSearchKey { get; init; }
    public Dictionary<string, object> CachePayloadDocument { get; init; }
    public EntityAnalysisModel EntityAnalysisModel { get; init; }
    public EntityAnalysisModelInstanceEntryPayload EntityAnalysisModelInstanceEntryPayload { get; init; }
    public Dictionary<string, double> EntityInstanceEntryDictionaryKvPs { get; init; }
    public Dictionary<int, List<Dictionary<string, object>>> AbstractionRuleMatches { get; init; } = new();
    public bool Finished { get; private set; }
    public ILog Log { get; init; }
    public IDatabase RedisDatabase { get; set; }
    public DynamicEnvironment.DynamicEnvironment DynamicEnvironment { get; set; }
    public List<Task> PendingWritesTasks { get; set; }

    public async Task StartAsync()
    {
        try
        {
            var limit = EntityAnalysisModel.CacheTtlLimit < DistinctSearchKey.SearchKeyFetchLimit
                ? EntityAnalysisModel.CacheTtlLimit
                : DistinctSearchKey.SearchKeyFetchLimit;

            var cachePayloadRepository = new Redis.CachePayloadRepository(RedisDatabase, Log);

            PendingWritesTasks.Add(cachePayloadRepository
                .InsertAsync(EntityAnalysisModel.TenantRegistryId,
                    EntityAnalysisModel.Guid,
                    AbstractionRuleGroupingKey,
                    CachePayloadDocument[AbstractionRuleGroupingKey].AsString(),
                    EntityAnalysisModelInstanceEntryPayload.Payload,
                    EntityAnalysisModelInstanceEntryPayload.ReferenceDate,
                    EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid));

            var documents = await cachePayloadRepository.GetExcludeCurrent(EntityAnalysisModel.TenantRegistryId,
                EntityAnalysisModel.Guid,
                AbstractionRuleGroupingKey,
                CachePayloadDocument[AbstractionRuleGroupingKey].AsString(),
                limit, EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
            ).ConfigureAwait(false);

            if (documents != null)
            {
                documents.Add(CachePayloadDocument);

                Log.Info(
                    $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has created a filter for cache where {AbstractionRuleGroupingKey} has added the current transaction to the records,  so there are now {documents.Count} records for evaluation.  The records will now be matched against the Abstraction rules where this {AbstractionRuleGroupingKey} is expressed and the rule is marked as a history rule (else it will be done later as a basic rule).");

                var logicHashMatches = new Dictionary<string, List<Dictionary<string, object>>>();
                foreach (var evaluateAbstractionRule in EntityAnalysisModel.ModelAbstractionRules.FindAll(x =>
                             x.SearchKey == AbstractionRuleGroupingKey && x.Search))
                {
                    Log.Info(
                        $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has created a filter for cache where {AbstractionRuleGroupingKey} has added the current transaction to the records,  so there are now {documents.Count} records for evaluation.  The records will now be matched against the Abstraction rules where this {AbstractionRuleGroupingKey} will process Abstraction Rule {evaluateAbstractionRule.Id}.");

                    try
                    {
                        List<Dictionary<string, object>> matches;

                        Log.Info(
                            $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} abstraction rule id {evaluateAbstractionRule.Id} has a logic hash of {evaluateAbstractionRule.LogicHash} and will be checked against similar rules already run for the results returned from cache.");

                        if (logicHashMatches.TryGetValue(evaluateAbstractionRule.LogicHash, out var match))
                        {
                            matches = match;

                            Log.Info(
                                $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} abstraction rule id {evaluateAbstractionRule.Id} has a logic hash of {evaluateAbstractionRule.LogicHash} and has already run for the results returned from cache, having {matches.Count} records.");
                        }
                        else
                        {
                            Log.Info(
                                $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} abstraction rule id {evaluateAbstractionRule.Id} has a logic hash of {evaluateAbstractionRule.LogicHash} and has not already run.  There are currently {documents.Count} records before the filter.");

                            matches = documents.FindAll(x => ReflectRule.Execute(evaluateAbstractionRule,
                                EntityAnalysisModel, x,
                                null,
                                EntityInstanceEntryDictionaryKvPs, Log));

                            logicHashMatches.Add(evaluateAbstractionRule.LogicHash, matches);

                            Log.Info(
                                $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} abstraction rule id {evaluateAbstractionRule.Id} has a logic hash of {evaluateAbstractionRule.LogicHash} and has been run.  There are now {matches.Count} having matched.  It has been added to the logic cache so it does not have to be run again.");
                        }

                        var fromDate = GetFromDate(evaluateAbstractionRule);

                        Log.Info(
                            $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} abstraction rule id {evaluateAbstractionRule.Id} has a logic hash of {evaluateAbstractionRule.LogicHash} will search for matches between {fromDate} and {EntityAnalysisModelInstanceEntryPayload.ReferenceDate}.");

                        var finalMatches = matches.FindAll(x =>
                            x[EntityAnalysisModel.ReferenceDateName].AsDateTime() >= fromDate &&
                            x[EntityAnalysisModel.ReferenceDateName].AsDateTime() <=
                            EntityAnalysisModelInstanceEntryPayload.ReferenceDate);
                        AbstractionRuleMatches.Add(evaluateAbstractionRule.Id,
                            []);
                        AbstractionRuleMatches[evaluateAbstractionRule.Id] = finalMatches;

                        Log.Info(
                            $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} abstraction rule id {evaluateAbstractionRule.Id} has a logic hash of {evaluateAbstractionRule.LogicHash} has a final number of matches of {finalMatches.Count} and has been added to a collection for aggregation later on.");
                    }
                    catch (Exception ex)
                    {
                        Log.Info(
                            $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} abstraction rule id {evaluateAbstractionRule.Id} has a logic hash of {evaluateAbstractionRule.LogicHash} has produced an error as {ex}.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Info(
                $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has produced an error for grouping key {AbstractionRuleGroupingKey} as {ex}.");
        }
        finally
        {
            Finished = true;

            Log.Info(
                $"Abstraction Rule Execute: GUID {EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has concluded for grouping key {AbstractionRuleGroupingKey}.");
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