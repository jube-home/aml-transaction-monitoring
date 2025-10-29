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

// ReSharper disable CollectionNeverUpdated.Global

namespace Jube.Engine.Model
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Archive;
    using Cache;
    using Cache.Redis.Interfaces;
    using Cache.Redis.Models;
    using Data.Context;
    using Data.Poco;
    using Data.Query;
    using Data.Repository;
    using Dictionary;
    using DynamicEnvironment;
    using Invoke.Abstraction;
    using Invoke.Reflect;
    using log4net;
    using Microsoft.VisualBasic;
    using Newtonsoft.Json.Serialization;
    using Processing;
    using Processing.Payload;
    using Processing.Payload.Performance;
    using Sanctions;
    using ExhaustiveSearchInstance=Exhaustive.ExhaustiveSearchInstance;

    public class EntityAnalysisModel
    {
        public delegate MemoryStream Transform(string foreColor, string backColor, double responseElevation,
            string responseContent, string responseRedirect, DictionaryNoBoxing entityInstanceEntryPayloadCache,
            Dictionary<string, double> entityInstanceEntryAbstraction,
            Dictionary<string, int> entityInstanceEntryTtlCounters,
            Dictionary<int, string> entityInstanceEntryActivation,
            Dictionary<string, double> entityInstanceEntryAbstractionCalculations,
            Dictionary<string, int> responseTimes, ILog log);

        // ReSharper disable once UnassignedField.Global
        public CacheService CacheService;

        // ReSharper disable once UnassignedField.Global
        public DefaultContractResolver ContractResolver;
        public DateTime LastModelSearchKeyCacheWritten;

        public string ArchivePayloadSql { get; set; }
        public ILog Log { get; init; }
        public string Name { get; set; }
        public Guid Guid { get; set; }
        public int Id { get; set; }
        public ConcurrentQueue<Tag> PendingTagging { get; init; } = new ConcurrentQueue<Tag>();
        public List<EntityAnalysisModelAbstractionRule> ModelAbstractionRules { get; set; } = [];
        public List<EntityAnalysisModelTtlCounter> ModelTtlCounters { get; set; } = [];
        public List<EntityAnalysisModelSanction> EntityAnalysisModelSanctions { get; set; } = [];
        public List<EntityAnalysisModelActivationRule> ModelActivationRules { get; set; } = [];
        public List<EntityModelGatewayRule> ModelGatewayRules { get; set; } = [];
        public List<ExhaustiveSearchInstance> ExhaustiveModels { get; set; } = [];
        public Dictionary<string, DistinctSearchKey> DistinctSearchKeys { get; set; } = new Dictionary<string, DistinctSearchKey>();
        public string EntryXPath { get; set; }
        public string EntryName { get; set; }
        public string ReferenceDateXpath { get; set; }
        public string ReferenceDateName { get; set; }
        public List<EntityAnalysisModelRequestXPath> EntityAnalysisModelRequestXPaths { get; set; } = [];

        public List<EntityAnalysisModelAbstractionCalculation> EntityAnalysisModelAbstractionCalculations { get; set; }
            = [];

        public List<EntityAnalysisModelInlineFunction> EntityAnalysisModelInlineFunctions { get; set; } = [];
        public Dictionary<int, EntityAnalysisModelHttpAdaptation> EntityAnalysisModelAdaptations { get; set; } = new Dictionary<int, EntityAnalysisModelHttpAdaptation>();
        public List<EntityAnalysisModelTag> EntityAnalysisModelTags { get; set; } = [];
        public ConcurrentQueue<ActivationWatcher> PersistToActivationWatcherAsync { get; init; } = new ConcurrentQueue<ActivationWatcher>();
        public bool Started { get; set; }
        public Guid EntityAnalysisInstanceGuid { get; set; }
        public Guid EntityAnalysisModelInstanceGuid { get; set; }
        public Dictionary<string, List<string>> EntityAnalysisModelLists { get; set; } = new Dictionary<string, List<string>>();
        public List<EntityAnalysisModelInlineScript> EntityAnalysisModelInlineScripts { get; set; } = [];
        public Dictionary<int, EntityAnalysisModelDictionary> KvpDictionaries { get; set; } = new Dictionary<int, EntityAnalysisModelDictionary>();
        public int CacheTtlLimit { get; set; }
        public Dictionary<string, List<string>> EntityAnalysisModelSuppressionModels { get; set; }

        public Dictionary<string, Dictionary<string, List<string>>> EntityAnalysisModelSuppressionRules { get; set; } = new Dictionary<string, Dictionary<string, List<string>>>();

        public byte ReferenceDatePayloadLocationTypeId { get; set; }
        public double MaxResponseElevation { get; set; }
        public int TenantRegistryId { get; set; }
        public double BillingResponseElevationBalance { get; set; }

        public ConcurrentQueue<ResponseElevation> BillingResponseElevationBalanceEntries { get; } = new ConcurrentQueue<ResponseElevation>();

        public int ActivationWatcherCount { get; set; }
        public ConcurrentQueue<DateTime> ActivationWatcherCountJournal { get; } = new ConcurrentQueue<DateTime>();
        public double BillingResponseElevationCount { get; set; }
        public ConcurrentQueue<DateTime> BillingResponseElevationJournal { get; } = new ConcurrentQueue<DateTime>();
        public char MaxResponseElevationInterval { get; set; }
        public int MaxResponseElevationValue { get; set; }
        public int MaxResponseElevationThreshold { get; set; }
        public char MaxActivationWatcherInterval { get; set; }
        public int MaxActivationWatcherValue { get; set; }
        public double MaxActivationWatcherThreshold { get; set; }
        public double ActivationWatcherSample { get; set; }
        public DateTime LastCountersChecked { get; set; }
        public DateTime LastCountersWritten { get; set; }
        public int ModelInvokeCounter { get; set; }
        public int ModelInvokeGatewayCounter { get; set; }
        public int ModelResponseElevationCounter { get; set; }
        public double ModelResponseElevationSum { get; set; }
        public int BalanceLimitCounter { get; set; }
        public int ResponseElevationValueLimitCounter { get; set; }
        public int ResponseElevationFrequencyLimitCounter { get; set; }
        public int ResponseElevationValueGatewayLimitCounter { get; set; }
        public int ResponseElevationBillingSumLimitCounter { get; set; }
        public int ParentResponseElevationValueLimitCounter { get; set; }
        public int ParentBalanceLimitCounter { get; set; }
        public DateTime LastModelInvokeCountersWritten { get; set; }
        public bool OutputTransform { get; set; }
        public string FallbackResponseElevationRedirect { get; set; }
        public Transform OutputTransformDelegate { get; set; }
        public DynamicEnvironment JubeEnvironment { get; init; }
        public bool HasCheckedDatabaseForLastSearchKeyCacheDates { get; set; }
        public Dictionary<string, DateTime> LastAbstractionRuleCache { get; } = new Dictionary<string, DateTime>();
        public bool EnableCache { get; set; }
        public bool EnableSanctionCache { get; set; }
        public bool EnableTtlCounter { get; set; }
        public bool EnableActivationArchive { get; set; }
        public bool EnableRdbmsArchive { get; set; }
        public Dictionary<int, SanctionEntryDto> SanctionsEntries { get; init; } = new Dictionary<int, SanctionEntryDto>();
        public ConcurrentQueue<EntityAnalysisModelInstanceEntryPayload> PersistToDatabaseAsync { get; } = new ConcurrentQueue<EntityAnalysisModelInstanceEntryPayload>();
        public Dictionary<int, ArchiveBuffer> BulkInsertMessageBuffers { get; } = new Dictionary<int, ArchiveBuffer>();
        public bool EnableActivationWatcher { get; set; }
        public bool EnableResponseElevationLimit { get; set; }
        public char CacheTtlInterval { get; set; }
        public int CacheTtlIntervalValue { get; set; }
        public int DictionaryNoBoxingInitialSize { get; set; }

        public async Task AbstractionRuleCachingAsync()
        {
            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    "Entity Start: Will try and make a connection to the Database to create the Search Key Cache.");
            }

            var dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(JubeEnvironment.AppSettings("ConnectionString"));

            try
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} starting to loop around all of the Grouping keys that have been synchronise.");
                }

                var processedGroupingValues = 0;
                foreach (var (key, value) in DistinctSearchKeys)
                {
                    var ready = IsSearchKeyReady(value);

                    if (!ready)
                    {
                        continue;
                    }

                    var toDate = DateTime.Now;
                    var entityAnalysisModelsSearchKeyCalculationInstanceId =
                        InsertEntityAnalysisModelsSearchKeyCalculationInstances(dbContext, value, toDate);
                    var groupingValues = await GetDistinctListOfGroupingValuesAsync(value, toDate).ConfigureAwait(false);

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {key} has found {groupingValues.Count} grouping values.");
                    }

                    UpdateEntityAnalysisModelsSearchKeyCalculationInstancesDistinctValues(dbContext,
                        entityAnalysisModelsSearchKeyCalculationInstanceId, groupingValues.Count);

                    var expires = await GetExpiredCacheKeysAsync(value).ConfigureAwait(false);

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {key} has found {expires.Count} expires values.");
                    }

                    UpdateEntityAnalysisModelsSearchKeyCalculationInstancesExpiredSearchKeyCacheCount(dbContext,
                        entityAnalysisModelsSearchKeyCalculationInstanceId, expires.Count);
                    groupingValues = AddExpiredToGroupingValues(value, expires, groupingValues);

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {key} has found {expires.Count} grouping values in total including expires.");
                    }

                    if (groupingValues.Count > 0)
                    {
                        if (Log.IsInfoEnabled)
                        {
                            Log.Info(
                                $"Abstraction Rule Caching: For model {Id} and grouping key {key} has {groupingValues.Count} values.  For each value,  records will be returned using it as key and rules executed against the returned records.");
                        }

                        foreach (var groupingValue in groupingValues)
                        {
                            var entityInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                            {
                                Abstraction = new PooledDictionary<string, double>(ModelAbstractionRules.Count),
                                Activation = new PooledDictionary<string, EntityModelActivationRulePayload>(ModelActivationRules.Count),
                                Tag = new PooledDictionary<string, double>(EntityAnalysisModelTags.Count),
                                Dictionary = new PooledDictionary<string, double>(KvpDictionaries.Count),
                                TtlCounter = new PooledDictionary<string, int>(ModelTtlCounters.Count),
                                Sanction = new PooledDictionary<string, double>(EntityAnalysisModelSanctions.Count),
                                AbstractionCalculation = new PooledDictionary<string, double>(EntityAnalysisModelAbstractionCalculations.Count),
                                HttpAdaptation = new PooledDictionary<string, double>(EntityAnalysisModelAdaptations.Count),
                                ExhaustiveAdaptation = new PooledDictionary<string, double>(ExhaustiveModels.Count),
                                InvokeThreadPerformance = new InvokeThreadPerformance
                                {
                                    ComputeTime = new InvokeThreadComputeTime()
                                }
                            };

                            var abstractionRuleMatches = new Dictionary<int, List<DictionaryNoBoxing>>();

                            if (Log.IsInfoEnabled)
                            {
                                Log.Info(
                                    $"Abstraction Rule Caching: For model {Id} and grouping key {key} is processing grouping value {groupingValue}.");
                            }

                            if (!String.IsNullOrEmpty(groupingValue))
                            {
                                var entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId =
                                    InsertEntityAnalysisModelsSearchKeyDistinctValueCalculationInstances(dbContext,
                                        entityAnalysisModelsSearchKeyCalculationInstanceId, groupingValue);

                                var documents = await GetAllForKeyAsync(value, groupingValue).ConfigureAwait(false);

                                UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesEntriesCount(
                                    dbContext, entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId,
                                    documents.Count);

                                abstractionRuleMatches =
                                    ProcessAbstractionRules(value, documents, abstractionRuleMatches);

                                UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesAbstractionRulesMatches(
                                    dbContext, entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId);

                                if (Log.IsInfoEnabled)
                                {
                                    Log.Info(
                                        $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {key}, the matches will now be aggregated by looping through each abstraction rule.");
                                }

                                foreach (var abstractionRuleMatch in abstractionRuleMatches)
                                {
                                    if (Log.IsInfoEnabled)
                                    {
                                        Log.Info(
                                            $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {key} for abstraction rule {abstractionRuleMatch.Key} , the matches will now be aggregated by looping through each abstraction rule.");
                                    }

                                    try
                                    {
                                        var abstractionRule = ModelAbstractionRules.Find(x =>
                                            x.Id == abstractionRuleMatch.Key);

                                        if (abstractionRule != null)
                                        {
                                            var abstractionValue = GetAggregateValue(value, groupingValue,
                                                abstractionRuleMatches, abstractionRuleMatch, abstractionRule,
                                                entityInstanceEntryPayload);

                                            await UpsertOrDeleteSearchKeyCacheValueAsync(value, groupingValue,
                                                abstractionRuleMatch, abstractionRule, abstractionValue).ConfigureAwait(false);

                                            if (EnableRdbmsArchive)
                                            {
                                                ReplicateToDatabase(dbContext,
                                                    entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId,
                                                    value, groupingValue, abstractionRule, abstractionValue);
                                            }
                                        }
                                        else
                                        {
                                            Log.Error(
                                                $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {key} for abstraction rule {abstractionRuleMatch.Key}  could not find full details of the abstraction rule.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(
                                            $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {key} for abstraction rule {abstractionRuleMatch.Key} is in error as {ex}.");
                                    }
                                }

                                UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesCompleted(
                                    dbContext, entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId);
                            }
                            else
                            {
                                Log.Error(
                                    $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {key} is empty.");
                            }

                            processedGroupingValues += 1;
                            UpdateEntityAnalysisModelsSearchKeyCalculationInstancesDistinctValuesProcessedValuesCount(
                                dbContext, entityAnalysisModelsSearchKeyCalculationInstanceId,
                                processedGroupingValues);
                        }
                    }
                    else
                    {
                        Log.Error(
                            $"Abstraction Rule Caching: For model {Id} and Grouping Key {key} is empty.");
                    }

                    UpdateEntityAnalysisModelsSearchKeyCalculationInstancesCompleted(dbContext,
                        entityAnalysisModelsSearchKeyCalculationInstanceId);
                }

                LastModelSearchKeyCacheWritten = DateTime.Now;

                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} has recorded the date of last rule cache.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"Abstraction Rule Caching: For model {Id} has produced an error as {ex}.");
            }
            finally
            {
                await dbContext.CloseAsync().ConfigureAwait(false);
                await dbContext.DisposeAsync().ConfigureAwait(false);
            }
        }

        private static int InsertEntityAnalysisModelsSearchKeyDistinctValueCalculationInstances(DbContext dbContext,
            int entityAnalysisModelsSearchKeyCalculationInstanceId, string groupingValue)
        {
            var repository = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceRepository(dbContext);

            var model = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstance
            {
                EntityAnalysisModelSearchKeyCalculationInstanceId = entityAnalysisModelsSearchKeyCalculationInstanceId,
                SearchKeyValue = groupingValue,
                CreatedDate = DateTime.Now
            };

            model = repository.Insert(model);

            return model.Id;
        }

        private int InsertEntityAnalysisModelsSearchKeyCalculationInstances(DbContext dbContext,
            DistinctSearchKey distinctSearchKey,
            DateTime toDate)
        {
            var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);

            var model = new EntityAnalysisModelSearchKeyCalculationInstance
            {
                SearchKey = distinctSearchKey.SearchKey,
                EntityAnalysisModelGuid = Guid,
                DistinctFetchToDate = toDate,
                CreatedDate = DateTime.Now
            };

            repository.Insert(model);

            return model.Id;
        }

        private static void UpdateEntityAnalysisModelsSearchKeyCalculationInstancesDistinctValues(DbContext dbContext,
            int entityAnalysisModelsSearchKeyCalculationInstanceId, int count)
        {
            var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);

            repository.UpdateDistinctValuesCount(entityAnalysisModelsSearchKeyCalculationInstanceId, count);
        }

        private static void UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesEntriesCount(
            DbContext dbContext, int entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId, int count)
        {
            var repository = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceRepository(dbContext);

            repository.UpdateEntriesCount(entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId, count);
        }

        private static void UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesAbstractionRulesMatches(
            DbContext dbContext, int entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId)
        {
            var repository = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceRepository(dbContext);

            repository.UpdateAbstractionRuleMatches(entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId);
        }

        private static void UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesCompleted(
            DbContext dbContext,
            int entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId)
        {
            var repository = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceRepository(dbContext);

            repository.UpdateCompleted(entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId);
        }

        private static void UpdateEntityAnalysisModelsSearchKeyCalculationInstancesExpiredSearchKeyCacheCount(
            DbContext dbContext, int entityAnalysisModelsSearchKeyCalculationInstanceId, int count)
        {
            var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);
            repository.UpdateExpiredSearchKeyCacheCount(entityAnalysisModelsSearchKeyCalculationInstanceId, count);
        }

        private static void UpdateEntityAnalysisModelsSearchKeyCalculationInstancesDistinctValuesProcessedValuesCount(
            DbContext dbContext, int entityAnalysisModelsSearchKeyCalculationInstanceId,
            int distinctValuesProcessedValuesCount)
        {
            var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);
            repository.UpdateDistinctValuesProcessedValuesCount(entityAnalysisModelsSearchKeyCalculationInstanceId,
                distinctValuesProcessedValuesCount);
        }

        private static void UpdateEntityAnalysisModelsSearchKeyCalculationInstancesCompleted(DbContext dbContext,
            int entityAnalysisModelsSearchKeyCalculationInstanceId)
        {
            var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);
            repository.UpdateCompleted(entityAnalysisModelsSearchKeyCalculationInstanceId);
        }

        private async Task UpsertOrDeleteSearchKeyCacheValueAsync(DistinctSearchKey distinctSearchKey,
            string groupingValue,
            KeyValuePair<int, List<DictionaryNoBoxing>> abstractionRuleMatch,
            EntityAnalysisModelAbstractionRule abstractionRule,
            double abstractionValue)
        {
            var value = await FindCacheKeyValueEntryAsync(CacheService.CacheAbstractionRepository,
                distinctSearchKey, groupingValue, abstractionRuleMatch, abstractionRule,
                abstractionValue).ConfigureAwait(false);

            if (value == null)
            {
                if (abstractionValue > 0)
                {
                    await InsertSearchKeyCacheValue(CacheService.CacheAbstractionRepository,
                        distinctSearchKey, groupingValue,
                        abstractionRuleMatch, abstractionRule,
                        abstractionValue).ConfigureAwait(false);
                }
            }
            else
            {
                await UpdateOrDeleteSearchKeyCacheValue(CacheService.CacheAbstractionRepository, distinctSearchKey, groupingValue,
                    abstractionRule,
                    abstractionValue).ConfigureAwait(false);
            }
        }

        private static void ReplicateToDatabase(DbContext dbContext,
            int entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId, DistinctSearchKey distinctSearchKey,
            string groupingValue, EntityAnalysisModelAbstractionRule abstractionRule, double abstractionValue)
        {
            var repository = new ArchiveEntityAnalysisModelAbstractionEntryRepository(dbContext);

            var model = new ArchiveEntityAnalysisModelAbstractionEntry
            {
                EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceId =
                    entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId,
                SearchKey = distinctSearchKey.SearchKey,
                SearchValue = groupingValue,
                Value = abstractionValue,
                EntityAnalysisModelAbstractionRuleId = abstractionRule.Id,
                CreatedDate = DateTime.Now
            };

            repository.Insert(model);
        }

        private async Task UpdateOrDeleteSearchKeyCacheValue(ICacheAbstractionRepository cacheAbstractionRepository,
            DistinctSearchKey distinctSearchKey, string groupingValue,
            EntityAnalysisModelAbstractionRule abstractionRule,
            double abstractionValue)
        {
            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As there are no existing cache values, it will be updated or deleted.");
            }

            if (abstractionValue == 0)
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As the abstraction value is zero, it will be deleted to save storage.");
                }

                await cacheAbstractionRepository.DeleteAsync(TenantRegistryId, Guid, distinctSearchKey.SearchKey,
                    groupingValue, abstractionRule.Name).ConfigureAwait(false);

                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As the abstraction value is zero, it has been deleted to save storage.");
                }
            }
            else
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As the abstraction value is not zero, it will be updated.");
                }

                await cacheAbstractionRepository.UpsertAsync(TenantRegistryId, Guid, distinctSearchKey.SearchKey,
                    groupingValue, abstractionRule.Name, abstractionValue).ConfigureAwait(false);

                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRule.Name}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As the abstraction value is not zero, it has been updated.");
                }
            }
        }

        private async Task InsertSearchKeyCacheValue(ICacheAbstractionRepository cacheAbstractionRepository,
            DistinctSearchKey distinctSearchKey, string groupingValue,
            KeyValuePair<int, List<DictionaryNoBoxing>> abstractionRuleMatch,
            EntityAnalysisModelAbstractionRule abstractionRule,
            double abstractionValue)
        {
            var (key, _) = abstractionRuleMatch;

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {key}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}. As there are no existing cache values, it will be inserted.");
            }

            await cacheAbstractionRepository.UpsertAsync(TenantRegistryId, Guid, distinctSearchKey.SearchKey,
                groupingValue, abstractionRule.Name, abstractionValue).ConfigureAwait(false);

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {key}  has returned aggregated value of {abstractionValue}.  The cache will be searched with the rule name {abstractionRule.Name}.  As there are no existing cache values, it has been updated.");
            }
        }

        private async Task<double?> FindCacheKeyValueEntryAsync(ICacheAbstractionRepository cacheAbstractionRepository,
            DistinctSearchKey distinctSearchKey, string groupingValue,
            KeyValuePair<int, List<DictionaryNoBoxing>> abstractionRuleMatch,
            EntityAnalysisModelAbstractionRule abstractionRule,
            double abstractionValue)
        {
            var value = await cacheAbstractionRepository.Get(
                TenantRegistryId, Guid, abstractionRule.Name, distinctSearchKey.SearchKey, groupingValue).ConfigureAwait(false);

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {abstractionRuleMatch.Key}  has returned aggregated value of {abstractionValue}.  The cache has returned for rule name {abstractionRule.Name} with {value == null} null document.  An upsert will now take place.");
            }

            return value;
        }

        private double GetAggregateValue(DistinctSearchKey distinctSearchKey, string groupingValue,
            Dictionary<int, List<DictionaryNoBoxing>> abstractionRuleMatches,
            KeyValuePair<int, List<DictionaryNoBoxing>> abstractionRuleMatch,
            EntityAnalysisModelAbstractionRule abstractionRule,
            EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayload)
        {
            var (key, _) = abstractionRuleMatch;

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {key}  has returned full details of the abstraction rule.");
            }

            var abstractionValue = EntityAnalysisModelAbstractionRuleAggregator.Aggregate(
                entityAnalysisModelInstanceEntryPayload, abstractionRuleMatches,
                abstractionRule, Log);

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and for Grouping Value {groupingValue} and Grouping Key {distinctSearchKey.SearchKey} for abstraction rule {key}  has returned aggregated value of {abstractionValue}.");
            }

            return abstractionValue;
        }

        private Dictionary<int, List<DictionaryNoBoxing>> ProcessAbstractionRules(
            DistinctSearchKey distinctSearchKey,
            List<DictionaryNoBoxing> documents,
            Dictionary<int, List<DictionaryNoBoxing>> abstractionRuleMatches)
        {
            var logicHashMatches = new Dictionary<string, List<DictionaryNoBoxing>>();

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} will step through all abstraction rules.");
            }

            foreach (var evaluateAbstractionRule in ModelAbstractionRules)
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} has a search type > 1 and matches on teh current grouping key.  The search type is {evaluateAbstractionRule.Search} and the rule grouping key is{evaluateAbstractionRule.SearchKey}.");
                }

                if (evaluateAbstractionRule.Search &&
                    evaluateAbstractionRule.SearchKey == distinctSearchKey.SearchKey)
                {
                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  As some rule logic can be common across a number of rules,  a check will be made to see if this logic has already been executed using the hash of the rule logic as {evaluateAbstractionRule.LogicHash}.");
                    }

                    List<DictionaryNoBoxing> matches;
                    if (logicHashMatches.TryGetValue(evaluateAbstractionRule.LogicHash, out var match))
                    {
                        matches = match;

                        if (Log.IsInfoEnabled)
                        {
                            Log.Info(
                                $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  Rule logic as {evaluateAbstractionRule.LogicHash} has already been executed so it will simply return the {matches.Count} records already having been matched on this logic.");
                        }
                    }
                    else
                    {
                        if (Log.IsInfoEnabled)
                        {
                            Log.Info(
                                $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  Rule logic as {evaluateAbstractionRule.LogicHash} has not already been executed so it be filtered using the rule logic.");
                        }

                        matches = documents.FindAll(x =>
                            ReflectRule.Execute(evaluateAbstractionRule, this, x, null, null, Log));
                        logicHashMatches.Add(evaluateAbstractionRule.LogicHash, matches);

                        if (Log.IsInfoEnabled)
                        {
                            Log.Info(
                                $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  Rule logic as {evaluateAbstractionRule.LogicHash} has been executed for the first time and returned {matches.Count} records already.  It has been added to the cache using the logic hash as a key.");
                        }
                    }

                    var historyThresholdDate =
                        DateAndTime.DateAdd(evaluateAbstractionRule.AbstractionRuleAggregationFunctionIntervalType,
                            evaluateAbstractionRule.AbstractionHistoryIntervalValue * -1, DateTime.Now);

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} will be used to filter for matches.  Rule logic as {evaluateAbstractionRule.LogicHash}, {matches.Count} records will be filtered based on the history criteria.  It has been added to the cache using the logic hash as a key.  The interval for the rule logic is {evaluateAbstractionRule.AbstractionHistoryIntervalType} and the value is {evaluateAbstractionRule.AbstractionHistoryIntervalValue}.  Records will be return where the date is between {historyThresholdDate} and now.");
                    }

                    var finalMatches = matches.FindAll(x =>
                        x["CreatedDate"].AsDateTime() >= historyThresholdDate &&
                        x["CreatedDate"].AsDateTime() <= DateTime.Now);

                    abstractionRuleMatches.Add(evaluateAbstractionRule.Id, []);
                    abstractionRuleMatches[evaluateAbstractionRule.Id] = finalMatches;

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} and abstraction rule {evaluateAbstractionRule.Id}  has a final number of matches of {finalMatches.Count} and has been added to the list of matches");
                    }
                }

                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is checking is abstraction rule {evaluateAbstractionRule.Id} is moving to the next grouping value.");
                }
            }

            return abstractionRuleMatches;
        }

        private async Task<List<DictionaryNoBoxing>> GetAllForKeyAsync(DistinctSearchKey distinctSearchKey,
            string groupingValue)
        {
            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} is processing grouping value {groupingValue} returning the top {distinctSearchKey.SearchKeyCacheFetchLimit}.");
            }

            var getArchiveSqlByKeyValueLimitQuery =
                new GetArchiveSqlByKeyValueLimitQuery(JubeEnvironment.AppSettings("ConnectionString"), Log);

            var cachePayloadSql = "select * from (" + distinctSearchKey.SqlSelect + distinctSearchKey.SqlSelectFrom +
                                  distinctSearchKey.SqlSelectOrderBy + ") c order by 2;";

            List<DictionaryNoBoxing> values;
            if (distinctSearchKey.SearchKeyCacheSample)
            {
                values = await getArchiveSqlByKeyValueLimitQuery.Execute(
                    cachePayloadSql, distinctSearchKey.SearchKey,
                    groupingValue, "RANDOM()", distinctSearchKey.SearchKeyCacheFetchLimit).ConfigureAwait(false);

                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} retrieved grouping value {groupingValue} returning the top {distinctSearchKey.SearchKeyCacheFetchLimit} records for the grouping key.  There are {values.Count} ordered randomly.");
                }
            }
            else
            {
                values = await getArchiveSqlByKeyValueLimitQuery.Execute(
                    cachePayloadSql, distinctSearchKey.SearchKey,
                    groupingValue, "CreatedDate", distinctSearchKey.SearchKeyCacheFetchLimit).ConfigureAwait(false);

                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} retrieved grouping value {groupingValue} returning the top {distinctSearchKey.SearchKeyCacheFetchLimit} records for the grouping key.  There are {values.Count} ordered by CreatedDate desc.");
                }

                values.Reverse();
            }

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} retrieved grouping value {groupingValue} returning the top {distinctSearchKey.SearchKeyCacheFetchLimit} records for the grouping key.  There are {values.Count} having been finalised.");
            }

            return values;
        }

        private List<string> AddExpiredToGroupingValues(DistinctSearchKey distinctSearchKey,
            IReadOnlyCollection<string> expires,
            List<string> groupingValues)
        {
            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has returned {expires.Count} cache keys.  These keys will now be added to the list distinct wise.");
            }

            foreach (var expired in expires)
            {
                if (!groupingValues.Contains(expired))
                {
                    groupingValues.Add(expired);

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has added the value {expired}.");
                    }
                }
                else
                {
                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has not added the value {expired} as it is a duplicate of one already added.");
                    }
                }
            }

            return groupingValues;
        }

        private async Task<List<string>> GetExpiredCacheKeysAsync(DistinctSearchKey distinctSearchKey)
        {
            if (!LastAbstractionRuleCache.ContainsKey(distinctSearchKey.SearchKey))
            {
                return [];
            }

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has a interval value of {distinctSearchKey.SearchKeyCacheTtlIntervalValue} and an interval of {distinctSearchKey.SearchKeyCacheIntervalType}.  Calculating the threshold for grouping keys that have expired.");
            }

            var deleteLineCacheKeys = distinctSearchKey.SearchKeyCacheIntervalType switch
            {
                "s" => LastAbstractionRuleCache[distinctSearchKey.SearchKey]
                    .AddSeconds(distinctSearchKey.SearchKeyCacheTtlIntervalValue * -1),
                "n" => LastAbstractionRuleCache[distinctSearchKey.SearchKey]
                    .AddMinutes(distinctSearchKey.SearchKeyCacheTtlIntervalValue * -1),
                "h" => LastAbstractionRuleCache[distinctSearchKey.SearchKey]
                    .AddHours(distinctSearchKey.SearchKeyCacheTtlIntervalValue * -1),
                _ => LastAbstractionRuleCache[distinctSearchKey.SearchKey]
                    .AddDays(distinctSearchKey.SearchKeyCacheTtlIntervalValue * -1)
            };

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} the date threshold for cache keys that have expired is {deleteLineCacheKeys}.");
            }

            return await CacheService.CachePayloadLatestRepository.GetDistinctKeysAsync(TenantRegistryId, Guid,
                distinctSearchKey.SearchKey,
                deleteLineCacheKeys).ConfigureAwait(false);
        }

        private async Task<List<string>> GetDistinctListOfGroupingValuesAsync(DistinctSearchKey distinctSearchKey,
            DateTime toDate)
        {
            List<string> value;

            var getArchiveDistinctEntryKeyValue =
                new GetArchiveDistinctEntryKeyValue(Log, JubeEnvironment.AppSettings("ConnectionString"));

            if (LastAbstractionRuleCache.ContainsKey(distinctSearchKey.SearchKey))
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has been set to a ready state and will now check if there has been any new records since it was last calculated on {LastAbstractionRuleCache[distinctSearchKey.SearchKey]} then bring back a distinct list of all grouping keys.");
                }

                value = await getArchiveDistinctEntryKeyValue.Execute(Guid,
                    distinctSearchKey.SearchKey,
                    LastAbstractionRuleCache[distinctSearchKey.SearchKey], toDate).ConfigureAwait(false);

                LastAbstractionRuleCache[distinctSearchKey.SearchKey] = toDate;

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Abstraction Rule Caching: For model {Id} Abstraction Rule Cache Last Entry Date All has been set to {toDate} and is has been added to a collection for grouping key {distinctSearchKey.SearchKey}.");
                }
            }
            else
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has been set to a ready state and is now bringing back a distinct list of values for the grouping key.");
                }

                value = await getArchiveDistinctEntryKeyValue.Execute(Guid, distinctSearchKey.SearchKey).ConfigureAwait(false);

                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} Abstraction Rule Cache Last Entry Date All has been set to {toDate} and is has been updated to a collection for grouping key {distinctSearchKey.SearchKey}.");
                }
            }

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has returned {value.Count} records when looking for distinct values for this grouping key.  There is a list of grouping keys to be evaluated,  now a check will be made for those that need to be evaluated again.");
            }

            return value;
        }

        private bool IsSearchKeyReady(DistinctSearchKey distinctSearchKey)
        {
            bool ready;

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Abstraction Rule Caching: For model {Id} Checking to see if grouping key {distinctSearchKey.SearchKey} is a search key.  It has a search key value of {distinctSearchKey.SearchKeyCache}.");
            }

            if (distinctSearchKey.SearchKeyCache)
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} grouping key {distinctSearchKey.SearchKey} is a search key. A check will now be performed to understand when this abstraction rule key was last calculated.");
                }

                if (LastAbstractionRuleCache.TryGetValue(distinctSearchKey.SearchKey, out var value))
                {
                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} will calculate the date threshold for this grouping key to be run.  It was last run on {LastAbstractionRuleCache.ContainsKey(distinctSearchKey.SearchKey)} and the SearchKey Cache Interval Type is {distinctSearchKey.SearchKeyCacheIntervalType} and the Search Key Cache Interval Value{distinctSearchKey.SearchKeyCacheIntervalValue}.");
                    }

                    var dateThreshold = DateAndTime.DateAdd(distinctSearchKey.SearchKeyCacheIntervalType,
                        distinctSearchKey.SearchKeyCacheIntervalValue, value);

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} should next run on {dateThreshold}.");
                    }

                    if (DateTime.Now > dateThreshold)
                    {
                        ready = true;

                        if (Log.IsInfoEnabled)
                        {
                            Log.Info(
                                $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has been set to a ready state as the threshold has lapsed.");
                        }
                    }
                    else
                    {
                        ready = false;

                        if (Log.IsInfoEnabled)
                        {
                            Log.Info(
                                $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has been set to a unready state as the threshold has not lapsed.");
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
                ready = false;

                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Abstraction Rule Caching: For model {Id} and grouping key {distinctSearchKey.SearchKey} has been set to a ready state as it has never been run.");
                }
            }

            return ready;
        }

        public async Task TtlCounterServerAsync()
        {
            try
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"TTL Counter Administration: has started for {Id}.  Is about to loop around all TTL Counters.");
                }

                foreach (var ttlCounterWithinLoop in ModelTtlCounters)
                {
                    try
                    {
                        if (Log.IsInfoEnabled)
                        {
                            Log.Info(
                                $"TTL Counter Administration: has started for {Id} is about to process TTL Counter {ttlCounterWithinLoop.Name} and data name {ttlCounterWithinLoop.TtlCounterDataName}.");
                        }

                        var referenceDate = await GetReferenceDate().ConfigureAwait(false);

                        if (referenceDate.HasValue)
                        {
                            if (Log.IsInfoEnabled)
                            {
                                Log.Info(
                                    $"TTL Counter Administration: has started for {Id} is about to process TTL Counter {ttlCounterWithinLoop.Name} obtained reference date of {referenceDate}.");
                            }

                            var adjustedTtlCounterDate =
                                GetAdjustedTtlCounterDate(ttlCounterWithinLoop, referenceDate.Value);

                            var aggregateList = await GetExpiredTtlCounterCacheCountsAsync(
                                CacheService.CacheTtlCounterEntryRepository, ttlCounterWithinLoop,
                                adjustedTtlCounterDate).ConfigureAwait(false);

                            foreach (var value in aggregateList)
                            {
                                await DecrementTtlCounterCache(ttlCounterWithinLoop, value.DataValue, value.Value).ConfigureAwait(false);

                                await DeleteTtlCounterEntryAsync(CacheService.CacheTtlCounterEntryRepository, ttlCounterWithinLoop,
                                    value.DataValue,
                                    value.ReferenceDate).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            if (Log.IsInfoEnabled)
                            {
                                Log.Info(
                                    $"TTL Counter Administration: Reference Date returned for {Id} and {ttlCounterWithinLoop.Name} as null.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"TTL Counter Administration: has produced an error for {ttlCounterWithinLoop.Name} and Data Name {ttlCounterWithinLoop.TtlCounterDataName} as {ex}.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"TTL Counter Administration: has produced an error as {ex}");
            }
            finally
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"TTL Counter Administration: Model TTL Counter processing for model id {Id} has finished.");
                }
            }
        }

        private async Task<DateTime?> GetReferenceDate()
        {
            var referenceDate = await CacheService.CacheReferenceDate.GetReferenceDate(TenantRegistryId, Guid).ConfigureAwait(false);
            return referenceDate;
        }

        private async Task DecrementTtlCounterCache(EntityAnalysisModelTtlCounter ttlCounter,
            string value, int decrement)
        {
            await CacheService.CacheTtlCounterRepository.DecrementTtlCounterCacheAsync(TenantRegistryId, Guid, ttlCounter.Guid,
                ttlCounter.TtlCounterDataName, value, decrement).ConfigureAwait(false);

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"TTL Counter Administration: has finished aggregation for {ttlCounter.Name} and Data Name {ttlCounter.TtlCounterDataName} and has also decremented value {value} by {decrement} in the TTL counter cache.  Will now use the same date criteria to delete the records from the entries table.");
            }
        }

        private async Task<List<ExpiredTtlCounterEntry>>
            GetExpiredTtlCounterCacheCountsAsync(
                ICacheTtlCounterEntryRepository cacheTtlCounterEntryRepository,
                EntityAnalysisModelTtlCounter ttlCounter,
                DateTime adjustedTtlCounterDate)
        {
            return await cacheTtlCounterEntryRepository.GetExpiredTtlCounterCacheCountsAsync(
                TenantRegistryId, Guid,
                ttlCounter.Guid, ttlCounter.TtlCounterDataName, adjustedTtlCounterDate).ConfigureAwait(false);
        }

        private async Task DeleteTtlCounterEntryAsync(
            ICacheTtlCounterEntryRepository cacheTtlCounterEntryRepository,
            EntityAnalysisModelTtlCounter ttlCounter,
            string dataValue,
            DateTime referenceDate)
        {
            await cacheTtlCounterEntryRepository.DeleteAsync(TenantRegistryId, Guid,
                ttlCounter.Guid, ttlCounter.TtlCounterDataName, dataValue, referenceDate).ConfigureAwait(false);
        }

        private DateTime GetAdjustedTtlCounterDate(EntityAnalysisModelTtlCounter ttlCounter, DateTime referenceDate)
        {
            try
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"TTL Counter Administration: has found a reference date of {referenceDate} for {ttlCounter.Name} and Data Name {ttlCounter.TtlCounterDataName}.");
                }

                return ttlCounter.TtlCounterInterval switch
                {
                    "d" => referenceDate.AddDays(ttlCounter.TtlCounterValue * -1),
                    "h" => referenceDate.AddHours(ttlCounter.TtlCounterValue * -1),
                    "n" => referenceDate.AddMinutes(ttlCounter.TtlCounterValue * -1),
                    "s" => referenceDate.AddSeconds(ttlCounter.TtlCounterValue * -1),
                    "m" => referenceDate.AddMonths(ttlCounter.TtlCounterValue * -1),
                    "y" => referenceDate.AddYears(ttlCounter.TtlCounterValue * -1),
                    _ => referenceDate.AddDays(ttlCounter.TtlCounterValue * -1)
                };
            }
            catch (Exception ex)
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"TTL Counter Administration: has found a reference date of {referenceDate}" +
                        $" for {ttlCounter.Name} and Data Name {ttlCounter.TtlCounterDataName}." +
                        $" Error of {ex} returning reference date as default.");
                }

                return referenceDate;
            }
        }

        public bool TryProcessSingleDequeueForCaseCreationAndArchiver(int threadSequence)
        {
            var found = false;
            try
            {
                if (BulkInsertMessageBuffers.TryGetValue(threadSequence, out var buffer))
                {
                    PersistToDatabaseAsync.TryDequeue(out var payload);

                    if (payload != null)
                    {
                        buffer.LastMessage = DateTime.Now;

                        found = true;

                        CaseCreationAndArchiver(payload, buffer);

                        payload = null;//Totally concluded at this point.  Release for GC.
                    }
                    else
                    {
                        if (buffer.LastMessage.AddSeconds(10) <= DateTime.Now &&
                            buffer.Archive.Count > 0)
                        {
                            WriteToDatabase(buffer);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Database Persist: An error has occurred as {ex}");
            }

            return found;
        }

        public void CaseCreationAndArchiver(EntityAnalysisModelInstanceEntryPayload payload,
            ArchiveBuffer bulkInsertMessageBuffer)
        {
            var payloadJsonStore = new EntityAnalysisModelInstanceEntryPayloadJson();
            var json = payloadJsonStore.BuildJson(payload, ContractResolver);

            string jsonString = null;
            if (payload.CreateCasePayload != null)
            {
                jsonString = Encoding.UTF8.GetString(json.ToArray());
                CreateCase(payload, jsonString);
                payload.CreateCasePayload = null;
            }

            if (!payload.StoreInRdbms)
            {
                return;
            }

            if (String.IsNullOrEmpty(jsonString))
            {
                jsonString = Encoding.UTF8.GetString(json.ToArray());
            }

            if (payload.Reprocess)
            {
                TransactionalUpdate(payload, jsonString);
            }
            else if (bulkInsertMessageBuffer is null)
            {
                Log.Error("Database Persist: Not implemented bulkInsertMessageBuffer is null.");
            }
            else
            {
                DataTableInsertToBuffer(bulkInsertMessageBuffer, payload, jsonString);

                if (bulkInsertMessageBuffer.Archive.Count >=
                    Int32.Parse(JubeEnvironment.AppSettings("BulkCopyThreshold")))
                {
                    WriteToDatabase(bulkInsertMessageBuffer);
                }
            }
            // ReSharper disable once RedundantAssignment
            payload = null;//No more use for this object, let the GC have it back.
        }

        private void WriteToDatabase(ArchiveBuffer bulkInsertMessageBuffer)
        {
            var sw = new Stopwatch();
            sw.Start();

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    "Database Persist: The bulk copy threshold has been exceeded and the SQL Bulk Copy will be executed. A timer has been started.");
            }

            var dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(JubeEnvironment.AppSettings("ConnectionString"));

            if (Log.IsInfoEnabled)
            {
                Log.Info("Database Persist: Opened an SQL Bulk Collection via repository.");
            }

            var repositoryArchive = new ArchiveRepository(dbContext);
            var repositoryArchiveKeys = new ArchiveKeyRepository(dbContext);

            try
            {
                repositoryArchive.BulkCopy(bulkInsertMessageBuffer.Archive);
                repositoryArchiveKeys.BulkCopy(bulkInsertMessageBuffer.ArchiveKeys);
            }
            catch (Exception ex)
            {
                Log.Error($"Database Persist: An error has been created on build insert as {ex}");
            }
            finally
            {
                bulkInsertMessageBuffer.Archive.Clear();
                bulkInsertMessageBuffer.ArchiveKeys.Clear();

                dbContext.Close();
                dbContext.Dispose();

                if (Log.IsInfoEnabled)
                {
                    Log.Info("Database Persist: Closed an SQL Bulk Collection.");
                }
            }
        }

        private void DataTableInsertToBuffer(ArchiveBuffer bulkInsertMessageBuffer,
            EntityAnalysisModelInstanceEntryPayload payload, string jsonString)
        {
            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Database Persist: Database Persist message is valid for storage with Entry GUID of {payload.EntityAnalysisModelInstanceEntryGuid}.  This is being sent for bulk insert.");
            }

            try
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        "Database Persist: The flag to promote report table has been set for this model,  will now check columns are available and add the record to the data table.");
                }

                var model = new Data.Poco.Archive
                {
                    Json = jsonString,
                    EntityAnalysisModelInstanceEntryGuid = payload.EntityAnalysisModelInstanceEntryGuid,
                    ResponseElevation = payload.ResponseElevation.Value,
                    EntityAnalysisModelActivationRuleId = payload.PrevailingEntityAnalysisModelActivationRuleId,
                    EntityAnalysisModelId = payload.EntityAnalysisModelId,
                    ActivationRuleCount = payload.EntityAnalysisModelActivationRuleCount,
                    EntryKeyValue = payload.EntityInstanceEntryId,
                    ReferenceDate = payload.ReferenceDate,
                    CreatedDate = DateTime.Now
                };

                bulkInsertMessageBuffer.Archive.Add(model);

                foreach (var reportDatabaseValue in payload.ReportDatabaseValue)
                {
                    bulkInsertMessageBuffer.ArchiveKeys.Add(reportDatabaseValue);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Database Persist: An error has occurred as {ex}");
            }
        }

        private void TransactionalUpdate(EntityAnalysisModelInstanceEntryPayload payload, string jsonString)
        {
            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Database Persist: Database Persist message is valid for storage with Entry GUID of {payload.EntityAnalysisModelInstanceEntryGuid}.  This is being sent for update as it is reprocess.");
            }

            var dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(JubeEnvironment.AppSettings("ConnectionString"));
            try
            {
                var repository = new ArchiveRepository(dbContext);

                var model = new Data.Poco.Archive
                {
                    Json = jsonString,
                    EntityAnalysisModelInstanceEntryGuid = payload.EntityAnalysisModelInstanceEntryGuid,
                    ResponseElevation = payload.ResponseElevation.Value,
                    EntityAnalysisModelActivationRuleId = payload.PrevailingEntityAnalysisModelActivationRuleId,
                    EntityAnalysisModelId = payload.EntityAnalysisModelId,
                    ActivationRuleCount = payload.EntityAnalysisModelActivationRuleCount,
                    EntryKeyValue = payload.EntityInstanceEntryId,
                    ReferenceDate = payload.ReferenceDate,
                    CreatedDate = DateTime.Now
                };

                repository.Update(model);
            }
            catch (Exception ex)
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Error($"Database Persist: error processing payload as {ex}.");
                }
            }
            finally
            {
                dbContext.Close();
                dbContext.Dispose();

                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Database Persist: Database Persist message is valid for storage with Entry GUID of {payload.EntityAnalysisModelInstanceEntryGuid}.  Has finished reprocess ");
                }
            }
        }

        private void CreateCase(EntityAnalysisModelInstanceEntryPayload payload, string jsonString)
        {
            var dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(JubeEnvironment.AppSettings("ConnectionString"));
            try
            {
                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Case Creation: has received a case creation message with case entry GUID of {payload.CreateCasePayload.CaseEntryGuid}.");
                }

                var repositoryCase = new CaseRepository(dbContext);
                var query = new GetExistingCasePriorityQuery(dbContext);

                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Case Creation: connection to the database established for case entry GUID of {payload.CreateCasePayload.CaseEntryGuid}.");
                }

                var model = new Case
                {
                    EntityAnalysisModelInstanceEntryGuid = payload.CreateCasePayload.CaseEntryGuid,
                    CaseWorkflowGuid = payload.CreateCasePayload.CaseWorkflowGuid,
                    CaseWorkflowStatusGuid = payload.CreateCasePayload.CaseWorkflowStatusGuid,
                    CaseKey = payload.CreateCasePayload.CaseKey,
                    CaseKeyValue = payload.CreateCasePayload.CaseKeyValue,
                    Locked = 0,
                    Rating = 0,
                    CreatedDate = DateTime.Now
                };

                if (payload.CreateCasePayload.SuspendBypass)
                {
                    model.Diary = 1;
                    model.DiaryDate = payload.CreateCasePayload.SuspendBypassDate;
                    model.ClosedStatusId = 4;
                }
                else
                {
                    model.Diary = 0;
                    model.DiaryDate = payload.CreateCasePayload.SuspendBypassDate;
                    model.ClosedStatusId = 0;
                    model.DiaryDate = DateTime.Now;
                }

                model.Json = jsonString;

                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Case Creation: Have created a case creation SQL command with Case Entry GUID of {payload.CreateCasePayload.CaseEntryGuid}, Case Workflow ID of {payload.CreateCasePayload.CaseWorkflowGuid}, Case Workflow Status ID of {payload.CreateCasePayload.CaseWorkflowStatusGuid}, Case Key of {payload.CreateCasePayload.CaseKeyValue}, Case XML Bytes {jsonString.Length}");
                }

                var existing = query.Execute(model.CaseWorkflowGuid, model.CaseKey, model.CaseKeyValue);

                if (existing == null)
                {
                    repositoryCase.Insert(model);
                }
                else
                {
                    var repositoryCasesWorkflowsStatus =
                        new CaseWorkflowStatusRepository(dbContext, TenantRegistryId);

                    var recordCasesWorkflowsStatus =
                        repositoryCasesWorkflowsStatus.GetByGuid(model.CaseWorkflowStatusGuid);

                    if (recordCasesWorkflowsStatus.Priority < existing.Priority)
                    {
                        model.Id = existing.CaseId;
                        model.Locked = 0;
                        model.CaseWorkflowStatusGuid = payload.CreateCasePayload.CaseWorkflowStatusGuid;
                        repositoryCase.Update(model);
                    }
                }

                if (Log.IsInfoEnabled)
                {
                    Log.Info(
                        $"Case Creation: Executed Case Entry GUID of {payload.CreateCasePayload.CaseEntryGuid}, Case Workflow ID of {payload.CreateCasePayload.CaseWorkflowGuid}, Case Workflow Status ID of {payload.CreateCasePayload.CaseWorkflowStatusGuid}, Case Key of {payload.CreateCasePayload.CaseKeyValue}, Case JSON Bytes {jsonString.Length}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Case Creation: error processing payload as {ex}.");
            }
            finally
            {
                dbContext.Close();
                dbContext.Dispose();

                if (Log.IsInfoEnabled)
                {
                    Log.Info("Case Creation: closed the database connection.");
                }
            }
        }
    }
}
