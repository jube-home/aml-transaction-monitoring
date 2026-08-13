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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using AbstractionRulesWithSearchKeys;
    using Cache;
    using Cache.Redis.Models;
    using Data.Poco;
    using Dictionary;
    using StackExchange.Redis;
    using TaskCancellation.TaskHelper;
    using EntityAnalysisModelAbstractionRule=EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelAbstractionRule;

    public static class AbstractionRulesWithSearchKeysExtensions
    {
        public static async Task<Context> ExecuteAbstractionRulesWithSearchKeysAsync(this Context context)
        {
            if (context.EntityAnalysisModel.Flags.EnableCache)
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} Entity cache storage is enabled so will now proceed to loop through the distinct grouping keys for this model.");
                }

                var abstractionRuleMatches = new ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>>();
                var sortedSetKeysByGroupingKey = new ConcurrentDictionary<string, List<RedisValue>>();

                var payloadMap = await FetchSortedSetKeysByGroupingKeyAsync(context, sortedSetKeysByGroupingKey).ConfigureAwait(false);
                var parsedPayloadMap = BuildParsedPayloadMap(context, payloadMap);

                await ExecuteAbstractionRulesForGroupingKeysAsync(context, sortedSetKeysByGroupingKey, abstractionRuleMatches, parsedPayloadMap).ConfigureAwait(false);
                await CalculateAbstractionRuleValuesOrLookupFromTheCacheAsync(context, context.EntityAnalysisModel.Services.CacheService, abstractionRuleMatches).ConfigureAwait(false);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} all abstraction aggregation has finished, basic rules will now be processed.");
                }
            }
            else
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} Entity cache storage is not enabled so it cannot fetch anything relating to Abstraction Rules.");
                }
            }

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.AbstractionRulesWithSearchKeysAsync = (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            return context;
        }

        private static Task ExecuteAbstractionRulesForGroupingKeysAsync(Context context, ConcurrentDictionary<string, List<RedisValue>> sortedSetKeysByGroupingKey, ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>> abstractionRuleMatches, Dictionary<string, DictionaryNoBoxing<string>> parsedPayloadMap)
        {

            var pendingExecutionThreads = new List<Task>();
            foreach (var (key, value) in context.EntityAnalysisModel.Collections.DistinctSearchKeys)
            {
                if (value.SearchKeyCache)
                {
                    continue;
                }
                if (!context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(key))
                {
                    continue;
                }

                if (!sortedSetKeysByGroupingKey.TryGetValue(key, out var sortedSetKeys))
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} grouping key {key} had no sorted set keys available — rules for this grouping key will not be evaluated.");
                    }

                    continue;
                }

                var execute = new Execute
                {
                    EntityInstanceEntryDictionaryKvPs = context.EntityAnalysisModelInstanceEntryPayload.Dictionary,
                    DistinctSearchKey = value,
                    CachePayloadDocument = context.EntityAnalysisModelInstanceEntryPayload.Payload,
                    EntityAnalysisModelInstanceEntryPayload = context.EntityAnalysisModelInstanceEntryPayload,
                    AbstractionRuleMatches = abstractionRuleMatches,
                    EntityAnalysisModel = context.EntityAnalysisModel,
                    Log = context.Log,
                    SortedSetKeys = sortedSetKeys,
                    PayloadMap = parsedPayloadMap
                };

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has created an execute object for grouping key {key} with {sortedSetKeys.Count} sorted set keys.");
                }

                pendingExecutionThreads.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(
                    TaskType.ExecuteAbstractionRulesWithSearchKeyAsync,
                    async () => await execute.StartAsync().ConfigureAwait(false)));
            }

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} will now loop around all of the Abstraction rules for the purposes of performing the aggregations.");
            }

            return Task.WhenAll(pendingExecutionThreads);
        }

        private static Dictionary<string, DictionaryNoBoxing<string>> BuildParsedPayloadMap(Context context, Dictionary<string, DictionaryNoBoxing<int>> payloadMap)
        {

            var parsedPayloadMap = new Dictionary<string, DictionaryNoBoxing<string>>(payloadMap.Count);
            var parseIndexCache = context.EntityAnalysisModel.Collections.ParseIndexCache;

            foreach (var (key, raw) in payloadMap)
            {
                var document = new DictionaryNoBoxing<string>(raw.Count);
                foreach (var (i, value) in raw)
                {
                    switch (i)
                    {
                        case -1:
                            document.AddUnchecked(context.EntityAnalysisModel.References.ReferenceDateName, value);
                            continue;
                        case < 0:
                            continue;
                    }

                    if (parseIndexCache is not null && parseIndexCache.TryGetValue(i, out var name))
                    {
                        document.AddUnchecked(name, value);
                    }
                }
                parsedPayloadMap[key] = document;
            }
            return parsedPayloadMap;
        }

        private static async Task<Dictionary<string, DictionaryNoBoxing<int>>> FetchSortedSetKeysByGroupingKeyAsync(Context context, ConcurrentDictionary<string, List<RedisValue>> sortedSetKeysByGroupingKey)
        {

            var sortedSetFetchTasks = new List<Task>();
            foreach (var (key, value) in context.EntityAnalysisModel.Collections.DistinctSearchKeys)
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating grouping key {key}.");
                }

                try
                {
                    if (value.SearchKeyCache)
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} grouping key {key} is a search key, so the values will be fetched from the cache later on.");
                        }
                    }
                    else
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} checking if grouping key {key} exists in the current payload data.");
                        }

                        if (context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(key))
                        {
                            var limit = context.EntityAnalysisModel.Cache.CacheTtlLimit < value.SearchKeyFetchLimit
                                ? context.EntityAnalysisModel.Cache.CacheTtlLimit
                                : value.SearchKeyFetchLimit;

                            context.PendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.CachePayloadInsertAsync, async () => await context.EntityAnalysisModel.Services.CacheService.CachePayloadRepository
                                .InsertPayloadJournalAndLedgerAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                                    context.EntityAnalysisModel.Instance.Guid,
                                    key,
                                    context.EntityAnalysisModelInstanceEntryPayload.Payload[key].AsString(),
                                    context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate,
                                    context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid).ConfigureAwait(false)));

                            sortedSetFetchTasks.Add(FetchSortedSetKeysAsync(key, limit));

                            async Task FetchSortedSetKeysAsync(string groupingKey, int fetchLimit)
                            {
                                try
                                {
                                    var keys = await context.EntityAnalysisModel.Services.CacheService.CachePayloadRepository
                                        .GetSortedSetKeysAsync(
                                            context.EntityAnalysisModel.Instance.TenantRegistryId,
                                            context.EntityAnalysisModel.Instance.Guid,
                                            groupingKey,
                                            context.EntityAnalysisModelInstanceEntryPayload.Payload[groupingKey].AsString(),
                                            fetchLimit,
                                            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid)
                                        .ConfigureAwait(false);

                                    sortedSetKeysByGroupingKey[groupingKey] = keys;

                                    if (context.Log.IsInfoEnabled)
                                    {
                                        context.Log.Info(
                                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} grouping key {groupingKey} returned {keys.Count} sorted set keys.");
                                    }
                                }
                                catch (Exception ex) when (ex is not OperationCanceledException)
                                {
                                    context.Log.Error(
                                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} grouping key {groupingKey} failed to fetch sorted set keys and will be excluded from this evaluation as {ex}.");
                                }
                            }
                        }
                        else
                        {
                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} grouping key {key} does not exist in the current transaction data being processed.");
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} checking if grouping key {key} has created an error as {ex}.");
                }
            }

            await Task.WhenAll(sortedSetFetchTasks).ConfigureAwait(false);

            var distinctRedisValues = sortedSetKeysByGroupingKey.Values
                .SelectMany(x => x)
                .Select(x => x.ToString())
                .Distinct()
                .Select(s => (RedisValue)s)
                .ToList();

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has {distinctRedisValues.Count} distinct payload keys across all grouping keys. Fetching batch.");
            }

            var payloadMap = await context.EntityAnalysisModel.Services.CacheService.CachePayloadRepository
                .GetPayloadBatchAsync(
                    context.EntityAnalysisModel.Instance.TenantRegistryId,
                    context.EntityAnalysisModel.Instance.Guid,
                    distinctRedisValues,
                    context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid)
                .ConfigureAwait(false);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} batch payload fetch returned {payloadMap.Count} records.");
            }
            return payloadMap;
        }

        private static async Task CalculateAbstractionRuleValuesOrLookupFromTheCacheAsync(Context context,
            CacheService cacheService, ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>> abstractionRuleMatches)
        {
            foreach (var abstractionRule in context.EntityAnalysisModel.Collections.ModelAbstractionRules)
            {
                try
                {
                    if (!abstractionRule.Search)
                    {
                        continue;
                    }

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating abstraction rule {abstractionRule.Id}.");
                    }

                    var isCacheBacked = context.EntityAnalysisModel.Collections.DistinctSearchKeys.FirstOrDefault(x =>
                        x.Key == abstractionRule.SearchKey && x.Value.SearchKeyCache).Value != null;

                    if (isCacheBacked)
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} abstraction rule {abstractionRule.Id} has its values in the cache.");
                        }

                        var request = new List<EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue>
                        {
                            new EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue
                            {
                                AbstractionRuleName = abstractionRule.Name,
                                SearchKey = abstractionRule.SearchKey,
                                SearchValue = context.EntityAnalysisModelInstanceEntryPayload.Payload[abstractionRule.SearchKey].AsString()
                            }
                        };

                        foreach (var abstractionRuleNameValue in await cacheService.CacheAbstractionRepository
                                     .GetAsync(context.EntityAnalysisModel.Instance.TenantRegistryId, context.EntityAnalysisModel.Instance.Guid, request)
                                     .ConfigureAwait(false))
                        {
                            AddComputedValuesToAbstractionRulePayload(context, abstractionRule, abstractionRuleNameValue.Value);
                        }
                    }
                    else
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is aggregating abstraction rule {abstractionRule.Id} using documents in the entities collection of the cache.");
                        }

                        var aggregatedValue = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                            context.EntityAnalysisModelInstanceEntryPayload, abstractionRuleMatches, abstractionRule, context.Log);

                        AddComputedValuesToAbstractionRulePayload(context, abstractionRule, aggregatedValue);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is aggregating abstraction rule {abstractionRule.Id} but has created an error as {ex}.");
                }
            }
        }

        private static void AddComputedValuesToAbstractionRulePayload(Context context,
            EntityAnalysisModelAbstractionRule abstractionRule, double value)
        {
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction
                .Add(abstractionRule.Name, value);

            if (abstractionRule.ReportTable)
            {
                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                {
                    ProcessingTypeId = 5,
                    Key = abstractionRule.Name,
                    KeyValueFloat = value,
                    EntityAnalysisModelInstanceEntryGuid = context.EntityAnalysisModelInstanceEntryPayload
                        .EntityAnalysisModelInstanceEntryGuid
                });

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is aggregating abstraction rule {abstractionRule.Id} added value {value} to report payload with a column name of {abstractionRule.Name}.");
                }
            }

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} finished aggregating abstraction rule {abstractionRule.Id}.");
            }
        }
    }
}
