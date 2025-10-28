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

namespace Jube.Engine
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Accord.Neuro;
    using AutoMapper.Internal;
    using Cache;
    using Data.Cache.Postgres.Callback;
    using Data.Context;
    using Data.Poco;
    using Data.Query;
    using Data.Reporting;
    using Data.Repository;
    using Dictionary;
    using DynamicEnvironment;
    using Helpers;
    using Helpers.Json;
    using Invoke;
    using log4net;
    using Model;
    using Model.Archive;
    using Model.Compiler;
    using Model.Processing;
    using Model.Processing.Payload;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Parser;
    using RabbitMQ.Client;
    using Sanctions;
    using EntityAnalysisModel=Model.EntityAnalysisModel;
    using EntityAnalysisModelAbstractionCalculation=Model.EntityAnalysisModelAbstractionCalculation;
    using EntityAnalysisModelAbstractionRule=Model.EntityAnalysisModelAbstractionRule;
    using EntityAnalysisModelActivationRule=Model.EntityAnalysisModelActivationRule;
    using EntityAnalysisModelDictionary=Model.EntityAnalysisModelDictionary;
    using EntityAnalysisModelHttpAdaptation=Model.EntityAnalysisModelHttpAdaptation;
    using EntityAnalysisModelInlineFunction=Model.EntityAnalysisModelInlineFunction;
    using EntityAnalysisModelInlineScript=Model.EntityAnalysisModelInlineScript;
    using EntityAnalysisModelRequestXPath=Parser.EntityAnalysisModelRequestXPath;
    using EntityAnalysisModelSanction=Model.EntityAnalysisModelSanction;
    using EntityAnalysisModelTag=Model.EntityAnalysisModelTag;
    using EntityAnalysisModelTtlCounter=Model.EntityAnalysisModelTtlCounter;
    using ExhaustiveSearchInstance=Model.Exhaustive.ExhaustiveSearchInstance;
    using ExhaustiveSearchInstancePromotedTrialInstanceVariable=Model.Exhaustive.ExhaustiveSearchInstancePromotedTrialInstanceVariable;

    public class EntityAnalysisModelManager
    {
        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<Thread> activationWatcherThreads = [];

        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<Thread> archiverThreads = [];
        private readonly List<ArchiverThreadStarter> archiverThreadStarters = [];
        private readonly List<EntityAnalysisModelInlineScript> inlineScripts = [];

        // ReSharper disable once NotAccessedField.Local
        private Task abstractionRuleCachingTask;

        // ReSharper disable once NotAccessedField.Local
        private Task cachePruneTask;

        // ReSharper disable once CollectionNeverQueried.Local
        private Thread cacheThread;
        public DefaultContractResolver ContractResolver;
        private Guid entityAnalysisInstanceGuid;

        // ReSharper disable once CollectionNeverQueried.Local
        private List<Task> entityReprocessingTasks;
        private JsonSerializerSettings jsonSerializerSettings;

        // ReSharper disable once NotAccessedField.Local
        private Task listenForCallbacksTask;

        // ReSharper disable once NotAccessedField.Local
        private Task modelSyncTask;
        private Thread modelSyncThread;
        private bool stopping;

        // ReSharper disable once NotAccessedField.Local
        private Task ttlCounterAdministrationTask;
        private Thread tTtlCThread;

        public ILog Log { get; set; }
        public ConcurrentQueue<Tag> PendingTagging { get; set; } = new ConcurrentQueue<Tag>();
        public Dictionary<int, EntityAnalysisModel> ActiveEntityAnalysisModels { get; } = new Dictionary<int, EntityAnalysisModel>();
        private ConcurrentQueue<ActivationWatcher> PersistToActivationWatcherAsync { get; } = new ConcurrentQueue<ActivationWatcher>();
        public Dictionary<string, Assembly> HashCacheAssembly { get; set; }
        public Dictionary<int, SanctionEntryDto> SanctionsEntries { get; set; } = new Dictionary<int, SanctionEntryDto>();
        public Random Seeded { get; set; }
        public DynamicEnvironment JubeEnvironment { get; set; }
        public IModel RabbitMqChannel { get; set; }
        public CacheService CacheService { get; set; }
        public ConcurrentQueue<Notification> PendingNotification { get; set; }
        // ReSharper disable once MemberCanBePrivate.Global
        public bool EntityModelsHasLoadedForStartup { get; set; }
        // ReSharper disable once MemberCanBePrivate.Global
        public ConcurrentDictionary<Guid, Callback> PendingCallbacks { get; set; }

        public void StopMe()
        {
            stopping = true;

            foreach (var archiverThreadStarter in archiverThreadStarters)
            {
                archiverThreadStarter.StopMe();
            }
        }

        public void Start()
        {
            BuildAndCacheJsonContractResolver();
            StartCallbackListener();
            LogStartInstance();
            StartModelSync();
            StartArchiver();
            StartActivationWatcherArchive();
            StartSearchKeyCacheServer();
            StartTtlCounterServer();
            StartReprocessing();
            StartCachePrune();
        }

        private void BuildAndCacheJsonContractResolver()
        {
            jsonSerializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
                ContractResolver = new DeepContractResolver()
            };

            jsonSerializerSettings.Error += (_, args) =>
            {
                if (args.ErrorContext.Error.InnerException is NotImplementedException)
                {
                    return;
                }

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Exhaustive, Balance and Currencies: Json has received " +
                        $"the json three a handled error on {args.ErrorContext.Error.InnerException}.");
                }

                args.ErrorContext.Handled = true;
            };
        }

        private void StartCallbackListener()
        {
            if (!JubeEnvironment.AppSettings("EnableCallback").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            PendingCallbacks = new ConcurrentDictionary<Guid, Callback>();

            var cacheCallbackRepository =
                new CacheCallbackRepository(JubeEnvironment.AppSettings("ConnectionString"), Log, PendingCallbacks);

            listenForCallbacksTask = Task.Run(cacheCallbackRepository.ListenForCallbacksAsync);
        }

        private void StartCachePrune()
        {
            if (JubeEnvironment.AppSettings("CachePruneServer").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                cachePruneTask = Task.Run(CachePrune);
            }
        }

        private async Task CachePrune()
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    "Cache Prune: Starting task.");
            }

            var limit = GetDeletionLimitOrDefaultIfNull();

            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    $"Cache Prune: Has built cache references with deletion chunk limit of {limit}.  Will proceed to enter loop for background deletion job.");
            }

            while (!stopping)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Cache Prune: There are {ActiveEntityAnalysisModels.Count} active models.");
                }

                foreach (var model in ActiveEntityAnalysisModels.Values)
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Cache Prune: For model {model.Id} the reference date will be looked up.");
                    }

                    var referenceDate = await CacheService.CacheReferenceDate.GetReferenceDate(model.TenantRegistryId, model.Guid)
                        .ConfigureAwait(false);

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Cache Prune: For model {model.Id} the reference date for {model.ReferenceDateName} is {referenceDate}.  " +
                            $"Will test not null before proceeding to delete.  Will move to calculate the threshold date.");
                    }

                    if (!referenceDate.HasValue)
                    {
                        continue;
                    }

                    var thresholdReferenceDatePayload = GetThresholdReferenceDateForDeletion(model, referenceDate);

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Cache Prune: For model {model.Id} the threshold reference date for " +
                            $"{model.ReferenceDateName} is {thresholdReferenceDatePayload}.  Will now instruct the delete if threshold date not null.");
                    }

                    if (thresholdReferenceDatePayload == null)
                    {
                        continue;
                    }

                    await CacheService.CachePayloadRepository.DeleteByReferenceDate(model.TenantRegistryId, model.Guid,
                        thresholdReferenceDatePayload.Value, limit).ConfigureAwait(false);

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Cache Prune: For model {model.Id} deletion routine has returned in the Payload repository.");
                    }

                    await CacheService.CachePayloadLatestRepository.DeleteByReferenceDate(model.TenantRegistryId, model.Guid,
                        referenceDate.Value, thresholdReferenceDatePayload.Value, limit,
                        model.DistinctSearchKeys.Select(distinctSearchKey
                            => (distinctSearchKey.Key,
                                distinctSearchKey.Value.SearchKeyTtlInterval,
                                distinctSearchKey.Value.SearchKeyTtlIntervalValue)).ToList()).ConfigureAwait(false);

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Cache Prune: For model {model.Id} deletion routine has returned in the Payload Latest " +
                            $"repository.  Will now loop around search keys to begin deletion of sorted sets linking to payload.");
                    }
                }

                var waitCachePrune = Int32.Parse(JubeEnvironment.AppSettings("WaitCachePrune"));

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Cache Prune: Active models processed.  Will sleep for {waitCachePrune}.");
                }

                Thread.Sleep(waitCachePrune);
            }
        }

        private static DateTime? GetThresholdReferenceDateForDeletion(EntityAnalysisModel model,
            DateTime? referenceDate)
        {
            if (referenceDate == null)
            {
                return null;
            }

            var thresholdReferenceDate = model.CacheTtlInterval switch
            {
                'd' => referenceDate.Value.AddDays(model.CacheTtlIntervalValue * -1),
                'h' => referenceDate.Value.AddHours(model.CacheTtlIntervalValue * -1),
                'n' => referenceDate.Value.AddMinutes(model.CacheTtlIntervalValue * -1),
                's' => referenceDate.Value.AddSeconds(model.CacheTtlIntervalValue * -1),
                'm' => referenceDate.Value.AddMonths(model.CacheTtlIntervalValue * -1),
                'y' => referenceDate.Value.AddYears(model.CacheTtlIntervalValue * -1),
                _ => referenceDate.Value.AddDays(model.CacheTtlIntervalValue * -1)
            };
            return thresholdReferenceDate;
        }

        private int GetDeletionLimitOrDefaultIfNull()
        {
            var limit = 100;
            if (JubeEnvironment.AppSettings("CacheTtlDeleteLimit") != null)
            {
                limit = Int32.Parse(JubeEnvironment.AppSettings("CacheTtlDeleteLimit"));
            }

            return limit;
        }

        private void StartReprocessing()
        {
            if (JubeEnvironment.AppSettings("EnableReprocessing").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                int i;
                var tempVar = Int32.Parse(JubeEnvironment.AppSettings("ReprocessingThreads"));

                entityReprocessingTasks = [];
                for (i = 1; i <= tempVar; i++)
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug($"Entity Start: Starting Reprocessing routine for thread {i}.");
                    }

                    entityReprocessingTasks.Add(Task.Run(EntityReprocessingAsync));

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug($"Entity Start: Started Reprocessing in start routine for thread {i}.");
                    }
                }
            }
            else
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug("Entity Start: Has not started reprocessing as it is disabled.");
                }
            }
        }

        private Parser ConfigureTokenParserForSecurity(DbContext dbContext)
        {
            if (Log.IsInfoEnabled)
            {
                Log.Info("Entity Start: Starting soft code parser.");
            }

            var repository = new RuleScriptTokenRepository(dbContext);
            var tokens = repository.Get().Select(s => s.Token).ToList();

            if (Log.IsInfoEnabled)
            {
                Log.Info($"Entity Start: Has fetched {tokens.Count} tokens.  Will construct and return the parser.");
            }

            return new Parser(Log, tokens);
        }

        private void StartTtlCounterServer()
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    $"Entity Start: TTL Counter Administration is set to be {JubeEnvironment.AppSettings("EnableTtlCounter")} on this node.");
            }

            if (!JubeEnvironment.AppSettings("EnableTtlCounter").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    $"Entity Start: Starting the TTL Counter Administration with a polling rate of {JubeEnvironment.AppSettings("WaitTtlCounterDecrement")}.");
            }

            ttlCounterAdministrationTask = Task.Run(TtlCounterAdministrationAsync);

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: TTL Counter Administration.");
            }
        }

        private void StartSearchKeyCacheServer()
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    $"Entity Start: The Rule Cache Server is set to be {JubeEnvironment.AppSettings("EnableSearchKeyCache")} on this node.");
            }

            if (!JubeEnvironment.AppSettings("EnableSearchKeyCache")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Starting the Rule Cache Engine.");
            }

            abstractionRuleCachingTask = Task.Run(AbstractionRuleCachingAsync);

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Started the Rule Cache Engine.");
            }
        }

        private void StartActivationWatcherArchive()
        {
            if (!JubeEnvironment.AppSettings("ActivationWatcherAllowPersist")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int i;
            var tempVar = Int32.Parse(JubeEnvironment.AppSettings("ActivationWatcherPersistThreads"));
            for (i = 1; i <= tempVar; i++)
            {
                ThreadStart tsActivationWatcher = PersistToActivationWatcher;
                var activationWatcherThread = new Thread(tsActivationWatcher)
                {
                    IsBackground = false,
                    Priority = ThreadPriority.Normal
                };
                activationWatcherThread.Start();
                activationWatcherThreads.Add(activationWatcherThread);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(String.Format("Entity Start: Started Activation Watcher Persist Thread " + i + "."));
                }
            }
        }

        private void StartArchiver()
        {
            int i;

            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    $"Entity Start: There are {JubeEnvironment.AppSettings("ArchiverPersistThreads")} SQL Persist threads about to start.");
            }

            var tempVar = Int32.Parse(JubeEnvironment.AppSettings("ArchiverPersistThreads"));
            for (i = 1; i <= tempVar; i++)
            {
                var persistToDatabaseAcrossAllModels = new ArchiverThreadStarter
                {
                    Log = Log,
                    ThreadSequence = i,
                    ActiveModels = ActiveEntityAnalysisModels
                };

                ThreadStart tsSql = persistToDatabaseAcrossAllModels.Start;
                var archiverThread = new Thread(tsSql)
                {
                    IsBackground = false,
                    Priority = ThreadPriority.Normal
                };
                archiverThread.Start();
                archiverThreads.Add(archiverThread);
                archiverThreadStarters.Add(persistToDatabaseAcrossAllModels);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Entity Start: Started Database Persist Thread {i}.");
                }
            }
        }

        private void StartModelSync()
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Starting Model Sync.");
            }

            modelSyncTask = Task.Run(ModelSyncAsync);

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Started Model Sync in start routine.");
            }
        }

        private void LogStartInstance()
        {
            var dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(JubeEnvironment.AppSettings("ConnectionString"));

            try
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug("Entity Start: Trying to establish a connection to the Database database.");
                }

                var repository = new EntityAnalysisInstanceRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug("Entity Start: Established a connection to the Database database.");
                }

                entityAnalysisInstanceGuid = Guid.NewGuid();

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: A GUID for this instance has been created and is {entityAnalysisInstanceGuid}.");
                }

                var model = new EntityAnalysisInstance
                {
                    Guid = entityAnalysisInstanceGuid,
                    Instance = Dns.GetHostName(),
                    CreatedDate = DateTime.Now
                };

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Passing values to record the entity instance starting Entity_Analysis_Instance_GUID {entityAnalysisInstanceGuid}; Node {model.Instance};.");
                }

                repository.Insert(model);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Recorded the entity instance starting in the Database database with a GUID of {entityAnalysisInstanceGuid}.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Entity Start: {ex}");
            }
            finally
            {
                dbContext.Close();
                dbContext.Dispose();

                if (Log.IsDebugEnabled)
                {
                    Log.Debug("Entity Start: Closed the Database Connection Finally.");
                }
            }
        }

        private async Task ModelSyncAsync()
        {
            var startupTenantRegistrySchedule = true;
            try
            {
                while (!stopping)
                {
                    try
                    {
                        var codeBase = Assembly.GetExecutingAssembly().Location;

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug($"Entity Model Sync: The code base path has been returned as {codeBase}.");
                        }

                        var strPathBinary = Path.GetDirectoryName(codeBase);
                        var strPathFramework = Path.GetDirectoryName(typeof(object).Assembly.Location);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug($"Entity Model Sync: The code base path has been returned as {codeBase}.");
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug("Entity Model Sync: Making a connection to the Database database.");
                        }

                        var dbContext =
                            DataConnectionDbContext.GetDbContextDataConnection(
                                JubeEnvironment.AppSettings("ConnectionString"));

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug("Entity Model Sync: Connected to the Database database.");
                        }

                        try
                        {
                            var tenantRegistrySchedules = GetTenantRegistrySchedule(dbContext);

                            CompileInlineScripts(strPathBinary, strPathFramework, dbContext);

                            CreateDataTableBuffersIfNotExist();

                            foreach (var tenantRegistrySchedule in tenantRegistrySchedules)
                            {
                                var parser = ConfigureTokenParserForSecurity(dbContext);

                                SyncExhaustiveSearchInstances(dbContext, parser);
                                SyncEntityAnalysisModelLists(dbContext, parser);
                                SyncEntityAnalysisModelDictionaries(dbContext, parser);

                                if (tenantRegistrySchedule.SynchronisationPending || startupTenantRegistrySchedule)
                                {
                                    if (startupTenantRegistrySchedule)
                                    {
                                        startupTenantRegistrySchedule = false;
                                    }

                                    SyncEntityAnalysisModels(dbContext, tenantRegistrySchedule.TenantRegistryId);
                                    SyncEntityAnalysisModelRequestXPath(dbContext, parser);
                                    SyncEntityAnalysisModelInlineScripts(dbContext);
                                    SyncEntityAnalysisModelInlineFunctions(dbContext, strPathBinary, parser);
                                    SyncEntityAnalysisModelGatewayRules(dbContext, strPathBinary, parser);
                                    SyncEntityAnalysisModelSanctions(dbContext, parser);
                                    SyncEntityAnalysisModelAbstractionRules(dbContext, strPathBinary, parser);
                                    SyncEntityAnalysisModelAbstractionCalculations(dbContext, strPathBinary, parser);
                                    SyncEntityAnalysisModelTtlCounters(dbContext, parser);
                                    SyncEntityAnalysisModelHttpAdaptation(dbContext, parser);
                                    SyncEntityAnalysisModelActivationRules(dbContext, strPathBinary, parser);
                                    SyncEntityAnalysisModelTags(dbContext);
                                    ConfirmSync(dbContext, tenantRegistrySchedule.TenantRegistryId);
                                }
                                else
                                {
                                    HeartbeatThisModel(dbContext, tenantRegistrySchedule.TenantRegistryId);
                                }

                                StoreRuleCounterValues(dbContext);
                                SyncSuppression(dbContext);
                                SyncActivationRuleSuppression(dbContext);
                                await StartupModelAsync(dbContext).ConfigureAwait(false);
                                EntityModelsHasLoadedForStartup = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Entity Start: Has produced an error {ex}");
                        }
                        finally
                        {
                            await dbContext.CloseAsync().ConfigureAwait(false);
                            await dbContext.DisposeAsync().ConfigureAwait(false);

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug("Entity Start: Closing the database connection.");
                            }

                            Thread.Sleep(Int32.Parse(JubeEnvironment.AppSettings("ModelSynchronisationWait")));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Entity Start: Error in licence and file path check as {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Fatal Failure of Model Sync.{ex}");
            }
        }

        private void ConfirmSync(DbContext dbContext, int tenantRegistryId)
        {
            try
            {
                var repository = new EntityAnalysisModelSyncronisationNodeStatusEntryRepository(dbContext);

                var upsert = new EntityAnalysisModelSynchronisationNodeStatusEntry
                {
                    Instance = Dns.GetHostName(),
                    TenantRegistryId = tenantRegistryId
                };

                repository.UpsertSynchronisation(upsert);
            }
            catch (Exception ex)
            {
                Log.Error($"Entity Start: Error in model sync as {ex}");
            }
        }

        private void StoreRuleCounterValues(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose Synchronisation of the model counters.  Will now start with the Gateway Rule Counters.");
                }

                foreach (var gatewayRule in value.ModelGatewayRules)
                {
                    if (gatewayRule.Counter > 0)
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Checking if model {key} is about to update gateway rule id {gatewayRule.EntityAnalysisModelGatewayRuleId} and counter of {gatewayRule.Counter}.");
                        }

                        UpdateGatewayRuleCounter(dbContext, gatewayRule);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Checking if model {key} has finished processing updating gateway rule id {gatewayRule.EntityAnalysisModelGatewayRuleId} and counter of {gatewayRule.Counter}.");
                        }
                    }
                    else
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Checking if model {key} will not update gateway rule id {gatewayRule.EntityAnalysisModelGatewayRuleId} as counter is 0.");
                        }
                    }
                }

                foreach (var activationRule in value.ModelActivationRules)
                {
                    if (activationRule.Counter > 0)
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Checking if model {key} is about to update activation rule id {activationRule.Id} and counter of {activationRule.Counter}.");
                        }

                        UpdateActivationRuleCounter(dbContext, activationRule);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Checking if model {key} has finished processing updating activation rule id {activationRule.Id} and counter of {activationRule.Counter}.");
                        }
                    }
                    else
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Checking if model {key} will not update activation rule id {activationRule.Id} as counter is 0.");
                        }
                    }
                }

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is finished Synchronisation of the model counters.");
                }
            }
        }

        private Task StartupModelAsync(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose of starting the thread.");
                }

                if (ActiveEntityAnalysisModels[key].Started)
                {
                    continue;
                }

                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Entity Start: Checking if model {key} is not started.");
                }

                value.EntityAnalysisModelInstanceGuid = Guid.NewGuid();

                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Entity Start: Checking if model {key} has now been started.");
                }

                var repository = new EntityAnalysisModelInstanceRepository(dbContext);

                var model = new EntityAnalysisModelInstance
                {
                    CreatedDate = DateTime.Now,
                    EntityAnalysisInstanceGuid = ActiveEntityAnalysisModels[key].EntityAnalysisInstanceGuid,
                    EntityAnalysisModelInstanceGuid = value.EntityAnalysisModelInstanceGuid,
                    EntityAnalysisModelId = ActiveEntityAnalysisModels[key].Id
                };

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Recording that model {key} with Created Date {model.CreatedDate}, Entity_Analysis_Model_GUID {ActiveEntityAnalysisModels[key].Guid}, Entity_Analysis_Model_Instance_GUID {ActiveEntityAnalysisModels[key].EntityAnalysisModelInstanceGuid}, Entity_Analysis_Instance_GUID {model.EntityAnalysisInstanceGuid} has now been started.");
                }

                repository.Insert(model);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Started model {key} with Started_Date {model.CreatedDate}, Entity_Analysis_Model_GUID {ActiveEntityAnalysisModels[key].Guid}, Entity_Analysis_Model_Instance_GUID {ActiveEntityAnalysisModels[key].EntityAnalysisModelInstanceGuid}, Entity_Analysis_Instance_GUID {model.EntityAnalysisInstanceGuid} has now been started.");
                }

                value.Started = true;

                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Entity Start: has started {value.Id}.");
                }
            }

            return Task.CompletedTask;
        }

        private void CreateDataTableBuffersIfNotExist()
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                try
                {
                    int i;
                    var tempVar = Int32.Parse(JubeEnvironment.AppSettings("ArchiverPersistThreads"));
                    for (i = 1; i <= tempVar; i++)
                    {
                        value.BulkInsertMessageBuffers.TryAdd(i, new ArchiveBuffer());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        $"Entity Start: Create table process has created an error as {ex} for model {key}.");
                }
            }
        }

        private void SyncActivationRuleSuppression(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose determining adding Suppression Activation Rules.");
                }

                var repository = new EntityAnalysisModelActivationRuleSuppressionRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelActivationRuleSuppressionRepository.GetByEntityAnalysisModelId for and entity model key of {key}.");
                }

                var records = repository.GetByEntityAnalysisModelGuidOrderById(value.Guid);

                var shadowEntityAnalysisModelSuppressionDictionary =
                    new Dictionary<string, Dictionary<string, List<string>>>();
                foreach (var record in records)
                {
                    try
                    {
                        Log.Debug(
                            $"Entity Start: Suppression ID {record.Id} returned for model {key}.");

                        if (record.SuppressionKey == null)
                        {
                            continue;
                        }

                        var suppressionDictionary = new Dictionary<string, List<string>>();
                        if (record.SuppressionKeyValue == null)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Checking to see if there is a shadow collection for suppression value {record.Id} for Entity Model ID {key}.");
                        }

                        if (!suppressionDictionary.ContainsKey(record.SuppressionKeyValue ?? String.Empty))
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: No shadow collection for suppression value {record.Id} for Entity Model ID {key} so it is being created.");
                            }

                            suppressionDictionary.Add(record.SuppressionKeyValue ?? String.Empty,
                                []);

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: checking for collection for suppression value {record.Id} and activation rule name {record.EntityAnalysisModelActivationRuleName} for Entity Model ID {key} so it is being created.");
                            }

                            if (!suppressionDictionary[
                                        record.SuppressionKeyValue ?? String.Empty]
                                    .Contains(record.EntityAnalysisModelActivationRuleName))
                            {
                                suppressionDictionary[
                                        record.SuppressionKeyValue ?? String.Empty]
                                    .Add(record.EntityAnalysisModelActivationRuleName);

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: added to collection for suppression value {record.Id} and activation rule name {record.EntityAnalysisModelActivationRuleName} for Entity Model ID {key} so it is being created.");
                                }
                            }
                            else
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: was not added to collection for suppression value {record.Id} and activation rule name {record.EntityAnalysisModelActivationRuleName} for Entity Model ID {key} as it already exists.");
                                }
                            }
                        }
                        else
                        {
                            if (!suppressionDictionary[
                                        record.SuppressionKeyValue ?? String.Empty]
                                    .Contains(record.EntityAnalysisModelActivationRuleName))
                            {
                                suppressionDictionary[
                                        record.SuppressionKeyValue ?? String.Empty]
                                    .Add(record.EntityAnalysisModelActivationRuleName);

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: added to collection for suppression value {record.Id} and activation rule name {record.EntityAnalysisModelActivationRuleName} for Entity Model ID {key} so it is being created.");
                                }
                            }
                        }

                        if (!shadowEntityAnalysisModelSuppressionDictionary.TryAdd(record.SuppressionKey,
                                suppressionDictionary))
                        {
                            shadowEntityAnalysisModelSuppressionDictionary[record.SuppressionKey] =
                                suppressionDictionary;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Suppression Key Value as {record.SuppressionKey} and already exists in collection,  added to key.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Suppression Key Value as {record.SuppressionKey} and already exists in collection,  added to key.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Suppression ID {record.Id} returned for model {key} is in error with {ex}.");
                    }
                }

                value.EntityAnalysisModelSuppressionRules = shadowEntityAnalysisModelSuppressionDictionary;

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Model {key} and suppression Model  {key} added to collection.");
                }
            }
        }

        private void SyncSuppression(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose adding suppression.");
                }

                var repository = new EntityAnalysisModelSuppressionRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelSuppressionRepository.GetByEntityAnalysisModelId and entity model key of {key}.");
                }

                var records = repository.GetByEntityAnalysisModelId(key);

                var shadowEntityAnalysisModelSuppressionList = new Dictionary<string, List<string>>();
                foreach (var record in records)
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Entity Analysis Model Activation Rule Suppression ID {record.Id} returned for model {key}.");
                        }

                        var suppressionDictionary = new List<string>();

                        if (record.SuppressionKeyValue == null)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Value as {record.SuppressionKeyValue} also checking to see if it is already added.");
                        }

                        if (!suppressionDictionary.Contains(record.SuppressionKeyValue))
                        {
                            suppressionDictionary.Add(record.SuppressionKeyValue);

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Suppression ID  {record.Id} set Value as {record.SuppressionKeyValue} has been added to a shadow list of suppression.");
                            }
                        }

                        if (!shadowEntityAnalysisModelSuppressionList.TryAdd(record.SuppressionKey,
                                suppressionDictionary))
                        {
                            shadowEntityAnalysisModelSuppressionList[record.SuppressionKey] =
                                suppressionDictionary;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Suppression Key Value as {record.SuppressionKey} and already exists in collection,  added to key.");
                            }
                        }
                        else
                        {
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Suppression Key Value as {record.SuppressionKey} and does not exist in collection,  created key.");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Entity Analysis Model Activation Rule Suppression ID {record.Id} returned for model {key} is in error with {ex}.");
                    }
                }

                value.EntityAnalysisModelSuppressionModels = shadowEntityAnalysisModelSuppressionList;

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Model {key} and Activation Rule Suppression ID added to collection.");
                }
            }
        }

        private void SyncEntityAnalysisModelDictionaries(DbContext dbContext, Parser parser)
        {
            parser.EntityAnalysisModelsDictionaries = [];

            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose determining adding Dictionary.");
                }

                var repositoryDictionary = new EntityAnalysisModelDictionaryRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelDictionaryRepository.GetByEntityAnalysisModelId and entity model key of {key}.");
                }

                var recordsDictionary = repositoryDictionary.GetByEntityAnalysisModelIdOrderById(key);

                var shadowKvpDictionary = new Dictionary<int, EntityAnalysisModelDictionary>();
                foreach (var recordDictionary in recordsDictionary)
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Dictionary ID {recordDictionary.Id} returned for model {key}.");
                        }

                        if (recordDictionary.Active.Value != 1)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Dictionary ID {recordDictionary.Id} returned for model {key} is active.");
                        }

                        var kvpDictionary = new EntityAnalysisModelDictionary();

                        if (recordDictionary.DataName != null)
                        {
                            kvpDictionary.DataName = recordDictionary.DataName.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} set Data Name as {kvpDictionary.DataName}.");
                            }
                        }

                        if (recordDictionary.ResponsePayload.HasValue)
                        {
                            kvpDictionary.ResponsePayload = recordDictionary.ResponsePayload == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} set Response Payload as {kvpDictionary.ResponsePayload}.");
                            }
                        }
                        else
                        {
                            kvpDictionary.ResponsePayload = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} found empty and set Response Payload as {kvpDictionary.ResponsePayload}.");
                            }
                        }

                        if (recordDictionary.Name == null)
                        {
                            continue;
                        }

                        kvpDictionary.Name = recordDictionary.Name.Replace(" ", "_");

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} set Name as {kvpDictionary.Name}.");
                        }

                        shadowKvpDictionary.Add(recordDictionary.Id, kvpDictionary);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} added {kvpDictionary.Name} to shadow copy of dictionary.");
                        }

                        parser.EntityAnalysisModelsDictionaries.TryAdd(recordDictionary.Name);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} added {kvpDictionary.Name} to parser.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Dictionary ID {recordDictionary.Id} returned for model {key} is in error with {ex}.");
                    }
                }

                foreach (var (i, kvpDictionary) in shadowKvpDictionary)
                {
                    var repositoryDictionaryKvp = new EntityAnalysisModelDictionaryKvpRepository(dbContext);

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelDictionaryKvpRepository.GetByEntityAnalysisModelDictionaryId for entity model key of {key} and Dictionary id {i}.");
                    }

                    var recordsDictionaryKvp = repositoryDictionaryKvp.GetByEntityAnalysisModelDictionaryIdOrderById(i);

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug("Returned all Dictionary KVP from the database.");
                    }

                    foreach (var recordDictionaryKvp in recordsDictionaryKvp)
                    {
                        try
                        {
                            if (recordDictionaryKvp.KvpKey != null)
                            {
                                var kvpKey = recordDictionaryKvp.KvpKey;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Dictionary KVP entity model key of {key} and Dictionary KVP id {recordDictionaryKvp.KvpKey} has found value of {kvpKey} .");
                                }

                                if (recordDictionaryKvp.KvpValue.HasValue)
                                {
                                    var kvpValue = recordDictionaryKvp.KvpValue.Value;

                                    if (kvpDictionary.KvPs.TryAdd(kvpKey, kvpValue))
                                    {
                                        continue;
                                    }
                                    
                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {key} has already added the KVP value.");
                                    }
                                }
                                else
                                {
                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {key} has found null value.");
                                    }
                                }
                            }
                            else
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {key} has found null value.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(
                                $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {key} is in error with {ex}.");
                        }
                    }

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {i} has added all Dictionary values.");
                    }
                }

                value.KvpDictionaries = shadowKvpDictionary;
            }
        }

        private void SyncEntityAnalysisModelLists(DbContext dbContext, Parser parser)
        {
            parser.EntityAnalysisModelsLists = [];

            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose determining adding Lists.");
                }

                var repositoryList = new EntityAnalysisModelListRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelListRepository.GetByEntityAnalysisModelId and entity model key of {key}.");
                }

                var recordsList = repositoryList.GetByEntityAnalysisModelIdOrderById(key);

                var listIdName = new Dictionary<int, string>();
                var shadowEntityAnalysisModelLists = new Dictionary<string, List<string>>();
                foreach (var recordList in recordsList)
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: List ID ID {recordList.Id} returned for model {key}.");
                        }

                        if (recordList.Active != 1)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: List ID ID {recordList.Id} returned for model {key} is active.");
                        }

                        if (recordList.Name == null)
                        {
                            continue;
                        }

                        var name = recordList.Name.Replace(" ", "_");
                        listIdName.Add(recordList.Id, recordList.Name);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model  {key} and List {recordList.Id} set Name as {name}.");
                        }

                        parser.EntityAnalysisModelsLists.TryAdd(recordList.Name);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model  {key} and List {recordList.Id} added {name} to parser.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: List ID ID {recordList.Id} returned for model {key} is in error with {ex}.");
                    }
                }


                foreach (var (i, s) in
                         from listIdNameKvp in listIdName
                         where !shadowEntityAnalysisModelLists.ContainsKey(listIdNameKvp.Value)
                         select listIdNameKvp)
                {
                    shadowEntityAnalysisModelLists.Add(s, []);

                    var repositoryListValues = new EntityAnalysisModelListValueRepository(dbContext);

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelListValueRepository.GetByEntityAnalysisModelListId for entity model key of {key} and list id {i}.");
                    }

                    var recordsListValues = repositoryListValues.GetByEntityAnalysisModelListIdOrderById(i);

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug("Returned all ListValues from the database.");
                    }

                    foreach (var recordListValues in recordsListValues)
                    {
                        try
                        {
                            if (recordListValues.ListValue != null)
                            {
                                shadowEntityAnalysisModelLists[s].Add(recordListValues.ListValue);

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: List Value entity model key of {key} and list id {recordListValues.EntityAnalysisModelListId} has found value of {recordListValues.ListValue} .");
                                }
                            }
                            else
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: List Value entity model key of {key} and list id {recordListValues.EntityAnalysisModelListId} has found null value.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(
                                $"Entity Start: List Value entity model key of {key} and list id {recordListValues.EntityAnalysisModelListId} is in error with {ex}.");
                        }
                    }

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: List Value entity model key of {key} and list id {i} has added all list values.");
                    }
                }

                value.EntityAnalysisModelLists = shadowEntityAnalysisModelLists;

                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Model {key} and List ID shadow copy has been over written");
                }
            }
        }

        private void SyncExhaustiveSearchInstances(DbContext dbContext, Parser parser)
        {
            parser.EntityAnalysisModelsExhaustiveAdaptations = [];

            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose determining adding models created and promoted by the Exhaustive Algorithm.");
                }

                var repository = new ExhaustiveSearchInstanceRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing select from Exhaustive Search Instance Repository and entity model key of {key}.");
                }

                var records = repository.GetByEntityAnalysisModelIdOrderById(key);

                var shadowEntityAnalysisModelExhaustive = new List<ExhaustiveSearchInstance>();
                foreach (var record in records.ToList())
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug($"Entity Start: Exhaustive ID {record.Id} returned for model {key}.");
                        }

                        if (record.Active != 1)
                        {
                            continue;
                        }

                        var exhaustive = new ExhaustiveSearchInstance
                        {
                            Id = record.Id,
                            Guid = record.Guid
                        };

                        if (record.Name == null)
                        {
                            exhaustive.Name =
                                $"Exhaustive_{exhaustive.Id}";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Exhaustive_{exhaustive.Id} set DEFAULT Name as {exhaustive.Name}.");
                            }
                        }
                        else
                        {
                            exhaustive.Name = record.Name.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set Name as {exhaustive.Name}.");
                            }
                        }

                        if (!record.ResponsePayload.HasValue)
                        {
                            exhaustive.ResponsePayload = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set DEFAULT Name as {exhaustive.ResponsePayload}.");
                            }
                        }
                        else
                        {
                            exhaustive.ResponsePayload = record.ResponsePayload == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set Name as {exhaustive.ResponsePayload}.");
                            }
                        }

                        if (!record.ReportTable.HasValue)
                        {
                            exhaustive.ReportTable = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set DEFAULT Name as {exhaustive.ReportTable}.");
                            }
                        }
                        else
                        {
                            exhaustive.ReportTable = record.ReportTable == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set Name as {exhaustive.ReportTable}.");
                            }
                        }

                        var getExhaustiveSearchInstancePromotedTrialInstanceQuery
                            = new GetExhaustiveSearchInstancePromotedTrialInstanceByLastActiveQuery(dbContext)
                                .Execute(exhaustive.Id);

                        if (getExhaustiveSearchInstancePromotedTrialInstanceQuery != null)
                        {
                            try
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Exhaustive, Balance and Currencies: Exhaustive GUID {exhaustive.Id} has received " +
                                        $"the json as {getExhaustiveSearchInstancePromotedTrialInstanceQuery.Json} from the database and will now start to load to Accord.");
                                }

                                exhaustive.TopologyNetwork =
                                    JsonConvert.DeserializeObject<ActivationNetwork>
                                    (getExhaustiveSearchInstancePromotedTrialInstanceQuery.Json,
                                        jsonSerializerSettings);

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Exhaustive, Balance and Currencies: Exhaustive GUID {exhaustive.Id} has deserialized " +
                                        $"json and will add to the Exhaustive");
                                }

                                var getExhaustiveSearchInstancePromotedTrialInstanceVariableQuery
                                    = new GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(dbContext);

                                foreach (var exhaustiveVariable in
                                         getExhaustiveSearchInstancePromotedTrialInstanceVariableQuery
                                             .ExecuteByExhaustiveSearchInstanceTrialInstanceId(
                                                 getExhaustiveSearchInstancePromotedTrialInstanceQuery
                                                     .ExhaustiveSearchInstanceTrialInstanceId).ToList().Select(variable =>
                                                 new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                                                 {
                                                     Name = variable.Name,
                                                     ProcessingTypeId = variable.ProcessingTypeId,
                                                     Mean = variable.Mean,
                                                     Sd = variable.StandardDeviation,
                                                     NormalisationTypeId = variable.NormalisationTypeId
                                                 }))
                                {
                                    exhaustive.NetworkVariablesInOrder.Add(exhaustiveVariable);
                                }

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Exhaustive GUID {exhaustive.Id} has loaded the byte array to Accord.");
                                }
                            }
                            catch (Exception ex)
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Exhaustive GUID {exhaustive.Id} has created an error during loading as {ex}.");
                                }
                            }

                            shadowEntityAnalysisModelExhaustive.Add(exhaustive);

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Exhaustive GUID {exhaustive.Id} has added {exhaustive.Name} to shadow collection.");
                            }

                            parser.EntityAnalysisModelsExhaustiveAdaptations.TryAdd(exhaustive.Name);

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Exhaustive GUID {exhaustive.Id} has added {exhaustive.Name} to parser.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Exhaustive GUID {exhaustive.Id} has empty json indicating training not concluded.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Exhaustive Id {record.Id} returned for model {key} as created an error as {ex}.");
                    }
                }

                value.ExhaustiveModels = shadowEntityAnalysisModelExhaustive;
            }
        }

        private void SyncEntityAnalysisModelTags(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose determining adding models created and adding Tags.");
                }

                var repository = new EntityAnalysisModelTagRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Entity Start: Executing fetch of all Tags for entity model {key}.");
                }

                var records = repository.GetByEntityAnalysisModelIdOrderById(key);

                var shadowEntityAnalysisModelTags = new List<EntityAnalysisModelTag>();
                foreach (var record in records)
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Tag {record.Id} returned for model {key}.");
                        }

                        if (record.Active != 1)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Tag ID {record.Id} returned for model {key} is active.");
                        }

                        var entityAnalysisModelTag = new EntityAnalysisModelTag
                        {
                            Id = record.Id
                        };

                        if (record.Name == null)
                        {
                            entityAnalysisModelTag.Name =
                                $"Tag_{entityAnalysisModelTag.Id}";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Tag_{entityAnalysisModelTag.Id} set DEFAULT Name as {entityAnalysisModelTag.Name}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelTag.Name = record.Name.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set Name as {entityAnalysisModelTag.Name}.");
                            }
                        }

                        if (!record.ResponsePayload.HasValue)
                        {
                            entityAnalysisModelTag.ResponsePayload = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set DEFAULT Name as {entityAnalysisModelTag.ResponsePayload}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelTag.ResponsePayload = record.ResponsePayload == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set Name as {entityAnalysisModelTag.ResponsePayload}.");
                            }
                        }

                        if (!record.ReportTable.HasValue)
                        {
                            entityAnalysisModelTag.ReportTable = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set DEFAULT Name as {entityAnalysisModelTag.ReportTable}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelTag.ReportTable = record.ReportTable == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set Name as {entityAnalysisModelTag.ReportTable}.");
                            }
                        }

                        shadowEntityAnalysisModelTags.Add(entityAnalysisModelTag);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} has been added to a shadow list of Tags.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Tag ID {record.Id} returned for model {key} as created an error as {ex}.");
                    }
                }

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Model {key} and Tag Model  {key} has completed creating the adaptations into a shadow list of adaptations and it will now be allocated the fields in the order that they appeared in model training.");
                }

                value.EntityAnalysisModelTags = shadowEntityAnalysisModelTags;

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Model {key} and Adaptations Model  {key} has completed creating the adaptations into a shadow list of adaptations and it has now be allocated the fields in the order that they appeared in model training from the shadow list of these variables.");
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Completed adding Adaptations and Exhaustive Neural Networks to entity models.");
            }
        }

        private void SyncEntityAnalysisModelHttpAdaptation(DbContext dbContext, Parser parser)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose determining adding models created and promoted by the HttP Adaptation.");
                }

                var repository = new EntityAnalysisModelHttpAdaptationRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelHttpAdaptationRepository.GetByEntityAnalysisModelId for entity model key of {key}.");
                }

                var records = repository.GetByEntityAnalysisModelIdOrderById(key);

                var shadowEntityAnalysisModelAdaptations = new Dictionary<int, EntityAnalysisModelHttpAdaptation>();
                foreach (var record in records)
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Adaptation ID ID {record.Id} returned for model {key}.");
                        }

                        if (record.Active != 1)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Adaptation ID ID {record.Id} returned for model {key} is active.");
                        }

                        var entityAnalysisModelAdaptation = new EntityAnalysisModelHttpAdaptation(10, false, 1000)
                        {
                            Id = record.Id
                        };

                        if (record.Name == null)
                        {
                            entityAnalysisModelAdaptation.Name =
                                $"Adaptation_{entityAnalysisModelAdaptation.Id}";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation{entityAnalysisModelAdaptation.Id} set DEFAULT Name as {entityAnalysisModelAdaptation.Name}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelAdaptation.Name = record.Name.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set Name as {entityAnalysisModelAdaptation.Name}.");
                            }
                        }

                        if (!record.ResponsePayload.HasValue)
                        {
                            entityAnalysisModelAdaptation.ResponsePayload = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set DEFAULT Name as {entityAnalysisModelAdaptation.ResponsePayload}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelAdaptation.ResponsePayload = record.ResponsePayload == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set Name as {entityAnalysisModelAdaptation.ResponsePayload}.");
                            }
                        }

                        if (!record.ReportTable.HasValue)
                        {
                            entityAnalysisModelAdaptation.ReportTable = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set DEFAULT Name as {entityAnalysisModelAdaptation.ReportTable}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelAdaptation.ReportTable = record.ReportTable == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Adaptation {entityAnalysisModelAdaptation.Id} set Name as {entityAnalysisModelAdaptation.ReportTable}.");
                            }
                        }

                        if (record.HttpEndpoint == null)
                        {
                            entityAnalysisModelAdaptation.HttpEndpoint = "";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Machine Learning Hook Type ID {entityAnalysisModelAdaptation.Id} set DEFAULT Http Endpoint  as {entityAnalysisModelAdaptation.HttpEndpoint}.");
                            }
                        }
                        else
                        {
                            var validHost = JubeEnvironment.AppSettings("HttpAdaptationUrl").EndsWith("/")
                                ? JubeEnvironment.AppSettings("HttpAdaptationUrl")
                                : JubeEnvironment.AppSettings("HttpAdaptationUrl") + "/";

                            var validUrl = record.HttpEndpoint.StartsWith("/")
                                ? record.HttpEndpoint.Remove(0, 1)
                                : record.HttpEndpoint;

                            var rPlumberEndpoint = $"{validHost}{validUrl}";

                            entityAnalysisModelAdaptation.HttpEndpoint = rPlumberEndpoint;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Machine Learning Hook Type ID {entityAnalysisModelAdaptation.Id} set Http Endpoint as {entityAnalysisModelAdaptation.HttpEndpoint}.");
                            }
                        }

                        shadowEntityAnalysisModelAdaptations.Add(
                            entityAnalysisModelAdaptation.Id,
                            entityAnalysisModelAdaptation);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model {key} and Exhaustive Search Instance Trial Instance ID  {entityAnalysisModelAdaptation.Id} has been added to a shadow list of Adaptations.");
                        }

                        parser.EntityAnalysisModelsHttpAdaptations.Add(entityAnalysisModelAdaptation.Name);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model {key} and Exhaustive Search Instance Trial Instance ID  {entityAnalysisModelAdaptation.Id} has added {entityAnalysisModelAdaptation.Name} to parser.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Adaptation ID ID {record.Id} returned for model {key} as created an error as {ex}.");
                    }
                }

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Model {key} and Adaptations Model  {key} has completed creating the adaptations into a shadow list of adaptations and it will now be allocated the fields in the order that they appeared in model training.");
                }

                value.EntityAnalysisModelAdaptations = shadowEntityAnalysisModelAdaptations;

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Model {key} and Adaptations Model  {key} has completed creating the adaptations into a shadow list of adaptations and it has now be allocated the fields in the order that they appeared in model training from the shadow list of these variables.");
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Completed adding Adaptations and Exhaustive Neural Networks to entity models.");
            }
        }

        private void SyncEntityAnalysisModelInlineScripts(DbContext dbContext)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose determining adding Inline Scripts.");
                }

                var repository = new EntityAnalysisModelInlineScriptRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelInlineScriptRepository.GetByEntityAnalysisModelId and entity model key of {key}.");
                }

                var records = repository.GetByEntityAnalysisModelIdOrderById(key);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug("Returned all inline scripts from the database.");
                }

                var shadowEntityAnalysisModelInlineScripts = new List<EntityAnalysisModelInlineScript>();
                foreach (var record in records)
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId} returned for model {key}.");
                        }

                        if (record.Active != 1)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId} returned for model {key} is Active.");
                        }

                        foreach (var addInlineScriptWithinLoop in inlineScripts)
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Inline Script ID {record.EntityAnalysisInlineScriptId} returned for model {key} checking inline script {addInlineScriptWithinLoop.InlineScriptId}.");
                            }

                            if (!record.EntityAnalysisInlineScriptId.HasValue)
                            {
                                continue;
                            }

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {addInlineScriptWithinLoop.InlineScriptId} checking to see if matched to this model.");
                            }

                            if (addInlineScriptWithinLoop.InlineScriptId !=
                                record.EntityAnalysisInlineScriptId.Value)
                            {
                                continue;
                            }

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {addInlineScriptWithinLoop.InlineScriptId} is matched to this model.  Will now check if there are grouping keys for this inline script that need to be attached to the model.");
                            }

                            foreach (var searchKey in addInlineScriptWithinLoop.GroupingKeys)
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {addInlineScriptWithinLoop.InlineScriptId} grouping ket {searchKey.SearchKey}.");
                                }

                                if (value.DistinctSearchKeys.TryAdd(searchKey.SearchKey, searchKey))
                                {
                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {addInlineScriptWithinLoop.InlineScriptId} grouping key {searchKey.SearchKey} has been matched.");
                                    }
                                }
                                else
                                {
                                    value.DistinctSearchKeys[searchKey.SearchKey] = searchKey;
                                }
                            }

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {addInlineScriptWithinLoop.InlineScriptId} is in the cache.");
                            }

                            if (addInlineScriptWithinLoop == null)
                            {
                                continue;
                            }

                            shadowEntityAnalysisModelInlineScripts.Add(addInlineScriptWithinLoop);

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {addInlineScriptWithinLoop.InlineScriptId} is in the cache and has been added to a shadow list of inline scripts for this model.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} has created an error as {ex}.");
                    }
                }

                value.EntityAnalysisModelInlineScripts = shadowEntityAnalysisModelInlineScripts;
                value.DictionaryNoBoxingInitialSize = CalculateDictionaryNoBoxingInitialSize(value);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        "Entity Start: Inline Scripts have overwritten the main copy with the shadow copy for model {ModelKVP.Key}.");
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Completed adding Inline Scripts to entity models.");
            }
        }

        private void SyncEntityAnalysisModelTtlCounters(DbContext dbContext, Parser parser)
        {
            parser.EntityAnalysisModelsTtlCounters = [];

            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose determining adding TTL Counters.");
                }

                var repository = new EntityAnalysisModelTtlCounterRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelTtlCounterRepository.GetByEntityAnalysisModelId and entity model key of {key}.");
                }

                var records = repository.GetByEntityAnalysisModelIdOrderById(key);

                var shadowEntityAnalysisModelTtlCounters = new List<EntityAnalysisModelTtlCounter>();
                foreach (var record in records)
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: TTL Counter ID {record.Id} returned for model {key}.");
                        }

                        if (record.Active != 1)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: TTL Counter ID {record.Id} returned for model {key} is active.");
                        }

                        var entityAnalysisModelTtlCounter = new EntityAnalysisModelTtlCounter
                        {
                            Id = record.Id
                        };

                        if (record.Guid != Guid.Empty)
                        {
                            entityAnalysisModelTtlCounter.Guid = record.Guid;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set Guid as {entityAnalysisModelTtlCounter.Guid}.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Cross Model Abstraction {entityAnalysisModelTtlCounter.Id} has a missing guid.");
                            }
                        }

                        if (record.Name == null)
                        {
                            entityAnalysisModelTtlCounter.Name =
                                $"TTL_Counter_{entityAnalysisModelTtlCounter.Id}";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set DEFAULT Name as {entityAnalysisModelTtlCounter.Name}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelTtlCounter.Name = record.Name.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Cross Model Abstraction {entityAnalysisModelTtlCounter.Id} set Name as {entityAnalysisModelTtlCounter.Name}.");
                            }
                        }

                        if (record.TtlCounterDataName == null)
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set DEFAULT TTL Counter Data Name as {entityAnalysisModelTtlCounter.TtlCounterDataName}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelTtlCounter.TtlCounterDataName =
                                record.TtlCounterDataName.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Cross Model Abstraction {entityAnalysisModelTtlCounter.Id} set TTL Counter Data Name as {entityAnalysisModelTtlCounter.TtlCounterDataName}.");
                            }
                        }

                        if (record.TtlCounterInterval == null)
                        {
                            entityAnalysisModelTtlCounter.TtlCounterInterval = "d";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set DEFAULT Name as {entityAnalysisModelTtlCounter.TtlCounterInterval}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelTtlCounter.TtlCounterInterval = record.TtlCounterInterval;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set Name as {entityAnalysisModelTtlCounter.TtlCounterInterval}.");
                            }
                        }

                        if (!record.TtlCounterValue.HasValue)
                        {
                            entityAnalysisModelTtlCounter.TtlCounterValue = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set DEFAULT Name as {entityAnalysisModelTtlCounter.TtlCounterValue}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelTtlCounter.TtlCounterValue = record.TtlCounterValue.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set Name as {entityAnalysisModelTtlCounter.TtlCounterValue}.");
                            }
                        }

                        if (!record.ResponsePayload.HasValue)
                        {
                            entityAnalysisModelTtlCounter.ResponsePayload = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set DEFAULT Response Payload as {entityAnalysisModelTtlCounter.ResponsePayload}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelTtlCounter.ResponsePayload = record.ResponsePayload.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set Response Payload as {entityAnalysisModelTtlCounter.ResponsePayload}.");
                            }
                        }

                        if (!record.ReportTable.HasValue)
                        {
                            entityAnalysisModelTtlCounter.ReportTable = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set DEFAULT Promote Report Table as {entityAnalysisModelTtlCounter.ReportTable}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelTtlCounter.ReportTable = record.ReportTable == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set Promote Report Table as {entityAnalysisModelTtlCounter.ReportTable}.");
                            }
                        }

                        if (!record.OnlineAggregation.HasValue)
                        {
                            entityAnalysisModelTtlCounter.OnlineAggregation = true;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Online Aggregation {entityAnalysisModelTtlCounter.Id} set DEFAULT Online Aggregation as {entityAnalysisModelTtlCounter.OnlineAggregation}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelTtlCounter.OnlineAggregation = record.OnlineAggregation == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Online Aggregation {entityAnalysisModelTtlCounter.Id} set Online Aggregation as {entityAnalysisModelTtlCounter.OnlineAggregation}.");
                            }
                        }

                        if (!record.EnableLiveForever.HasValue)
                        {
                            entityAnalysisModelTtlCounter.EnableLiveForever = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Enable Live Forever {entityAnalysisModelTtlCounter.Id} set DEFAULT Live Forever as {entityAnalysisModelTtlCounter.EnableLiveForever}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelTtlCounter.EnableLiveForever = record.EnableLiveForever.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Enable Live Forever {entityAnalysisModelTtlCounter.Id} set Live Forever as {entityAnalysisModelTtlCounter.EnableLiveForever}.");
                            }
                        }

                        shadowEntityAnalysisModelTtlCounters.Add(entityAnalysisModelTtlCounter);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} has been added to a shadow collection of TTL Counters.");
                        }

                        parser.EntityAnalysisModelsTtlCounters.TryAdd(entityAnalysisModelTtlCounter.Name);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} has {entityAnalysisModelTtlCounter.Name} to parser.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: TTL Counter ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }
                }

                value.ModelTtlCounters = shadowEntityAnalysisModelTtlCounters;

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Model {key} and TTL Counter Interval has been added to a shadow collection of TTL Counters.");
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Completed adding TTL Counters to entity models.");
            }
        }

        private void SyncEntityAnalysisModelSanctions(DbContext dbContext, Parser parser)
        {
            parser.EntityAnalysisModelsSanctions = [];

            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose determining adding Sanctions.");
                }

                var repository = new EntityAnalysisModelSanctionRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelSanctionRepository.GetByEntityAnalysisModelId and entity model key of {key}.");
                }

                var records = repository.GetByEntityAnalysisModelIdOrderById(key);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug("Returned all Sanctions from the database.");
                }

                var shadowEntityAnalysisModelSanctions = new List<EntityAnalysisModelSanction>();
                foreach (var record in records)
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Sanctions ID {record.Id} returned for model {key}.");
                        }

                        if (record.Active != 1)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Sanctions ID {record.Id} returned for model {key} is active.");
                        }

                        var entityAnalysisModelSanctions = new EntityAnalysisModelSanction
                        {
                            EntityAnalysisModelSanctionsId = record.Id
                        };

                        if (record.Name == null)
                        {
                            entityAnalysisModelSanctions.Name =
                                $"Sanctions_Name_{entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId}";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {record.Id} set DEFAULT Name as {entityAnalysisModelSanctions.Name}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelSanctions.Name =
                                record.Name.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Name as {entityAnalysisModelSanctions.Name}.");
                            }
                        }

                        if (!record.ResponsePayload.HasValue)
                        {
                            entityAnalysisModelSanctions.ResponsePayload = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Response Payload as {entityAnalysisModelSanctions.ResponsePayload}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelSanctions.ResponsePayload =
                                record.ResponsePayload.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Response Payload as {entityAnalysisModelSanctions.ResponsePayload}.");
                            }
                        }

                        if (!record.ReportTable.HasValue)
                        {
                            entityAnalysisModelSanctions.ReportTable = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Promote Report Table as {entityAnalysisModelSanctions.ReportTable}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelSanctions.ReportTable = record.ReportTable.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Promote Report Table as {entityAnalysisModelSanctions.ReportTable}.");
                            }
                        }

                        if (!record.CacheValue.HasValue)
                        {
                            entityAnalysisModelSanctions.CacheValue = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Cache Interval Value as {entityAnalysisModelSanctions.CacheValue}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelSanctions.CacheValue = record.CacheValue.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Cache Interval Value as {entityAnalysisModelSanctions.CacheValue}.");
                            }
                        }

                        if (!record.Distance.HasValue)
                        {
                            entityAnalysisModelSanctions.Distance = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Distance as {entityAnalysisModelSanctions.Distance}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelSanctions.Distance = record.Distance.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Distance as {entityAnalysisModelSanctions.Distance}.");
                            }
                        }

                        if (record.CacheInterval == null)
                        {
                            entityAnalysisModelSanctions.CacheInterval = 'd';

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Distance as {entityAnalysisModelSanctions.CacheInterval}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelSanctions.CacheInterval = record.CacheInterval.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Distance as {entityAnalysisModelSanctions.CacheInterval}.");
                            }
                        }

                        if (record.MultipartStringDataName != null)
                        {
                            entityAnalysisModelSanctions.MultipartStringDataName =
                                record.MultipartStringDataName.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Multipart String Data Name as {entityAnalysisModelSanctions.MultipartStringDataName}.");
                            }
                        }

                        shadowEntityAnalysisModelSanctions.Add(entityAnalysisModelSanctions);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} has been added to a shadow list of Sanctions.");
                        }

                        parser.EntityAnalysisModelsSanctions.TryAdd(entityAnalysisModelSanctions.Name);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} has added {entityAnalysisModelSanctions.Name} to parser.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Sanctions ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }
                }


                value.EntityAnalysisModelSanctions = shadowEntityAnalysisModelSanctions;

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Model {key} has overwritten the current Sanctions with the shadow list of Sanctions.");
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Completed adding Sanctions to entity models.");
            }
        }

        private void SyncEntityAnalysisModelAbstractionCalculations(DbContext dbContext, string strPath,
            Parser parser)
        {
            parser.EntityAnalysisModelAbstractionCalculations = [];

            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose determining adding Abstraction Calculations.");
                }

                var repository = new EntityAnalysisModelAbstractionCalculationRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelAbstractionCalculationRepository.GetByEntityAnalysisModelId for entity model key of {key}.");
                }

                var records = repository.GetByEntityAnalysisModelIdOrderByIdDesc(key);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug("Returned all Calculation Field from the database.");
                }

                var shadowEntityAnalysisModelAbstractionCalculation =
                    new List<EntityAnalysisModelAbstractionCalculation>();

                foreach (var record in records)
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Abstraction Calculation ID {record.Id} returned for model {key}.");
                        }

                        if (record.Active == 1)
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Abstraction Calculation ID {record.Id} returned for model {key} is active.");
                            }

                            var entityAnalysisModelAbstractionCalculation =
                                new EntityAnalysisModelAbstractionCalculation
                                {
                                    Id = record.Id
                                };

                            if (record.Name == null)
                            {
                                entityAnalysisModelAbstractionCalculation.Name =
                                    $"s{entityAnalysisModelAbstractionCalculation.Id}";

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT Name as {entityAnalysisModelAbstractionCalculation.Name}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelAbstractionCalculation.Name = record.Name.Replace(" ", "_");

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Name as {entityAnalysisModelAbstractionCalculation.Name}.");
                                }
                            }

                            if (record.EntityAnalysisModelAbstractionNameLeft != null)
                            {
                                entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameLeft =
                                    record.EntityAnalysisModelAbstractionNameLeft;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Entity Analysis Model Abstraction Name Left as {entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameLeft}.");
                                }
                            }
                            else
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT,  missing, Entity Analysis Model Abstraction Name Left as {entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameLeft}.");
                                }
                            }

                            if (record.EntityAnalysisModelAbstractionNameRight != null)
                            {
                                entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameRight =
                                    record.EntityAnalysisModelAbstractionNameRight;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Entity Analysis Model Abstraction Name Right as {entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameRight}.");
                                }
                            }
                            else
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT,  missing, Entity Analysis Model Abstraction Name Right as {entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameRight}.");
                                }
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                entityAnalysisModelAbstractionCalculation.ResponsePayload = false;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT Response Payload as {entityAnalysisModelAbstractionCalculation.ResponsePayload}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelAbstractionCalculation.ResponsePayload = record.ResponsePayload == 1;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Response Payload as {entityAnalysisModelAbstractionCalculation.ResponsePayload}.");
                                }
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                entityAnalysisModelAbstractionCalculation.ReportTable = false;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT Promote Report Table as {entityAnalysisModelAbstractionCalculation.ReportTable}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelAbstractionCalculation.ReportTable =
                                    record.ReportTable == 1;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Promote Report Table as {entityAnalysisModelAbstractionCalculation.ReportTable}.");
                                }
                            }

                            if (!record.AbstractionCalculationTypeId.HasValue)
                            {
                                entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId = 1;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT Calculation Type as {entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId =
                                    record.AbstractionCalculationTypeId.Value;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Calculation Type as {entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId}.");
                                }
                            }

                            var hasRuleScript = false;
                            if (record.FunctionScript != null)
                            {
                                var parsedRule = new ParsedRule
                                {
                                    OriginalRuleText = record.FunctionScript,
                                    ErrorSpans = []
                                };
                                parsedRule = parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    entityAnalysisModelAbstractionCalculation.FunctionScript =
                                        parsedRule.ParsedRuleText;
                                    hasRuleScript = true;

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and calculation {entityAnalysisModelAbstractionCalculation.Id} set  script as {entityAnalysisModelAbstractionCalculation.FunctionScript}.");
                                    }
                                }
                            }

                            if (hasRuleScript)
                            {
                                var activationRuleScript = new StringBuilder();
                                activationRuleScript.Append("Imports System.IO\r\n");
                                activationRuleScript.Append("Imports log4net\r\n");
                                activationRuleScript.Append("Imports System.Net\r\n");
                                activationRuleScript.Append("Imports System.Collections.Generic\r\n");
                                activationRuleScript.Append("Imports Jube.Dictionary\r\n");
                                activationRuleScript.Append("Imports System\r\n");
                                activationRuleScript.Append("Public Class CalculationRule\r\n");
                                activationRuleScript.Append(
                                    "Public Shared Function Match(Data As DictionaryNoBoxing,TTLCounter As PooledDictionary(Of String, Integer),Abstraction As PooledDictionary(Of string,double),List as Dictionary(Of String,List(Of String)),KVP As PooledDictionary(Of String, Double),Log as ILog) As Double\r\n");
                                activationRuleScript.Append("Dim Matched as Double\r\n");
                                activationRuleScript.Append("Try\r\n");
                                activationRuleScript.Append(entityAnalysisModelAbstractionCalculation.FunctionScript +
                                                            "\r\n");
                                activationRuleScript.Append("Catch ex As Exception\r\n");
                                activationRuleScript.Append("Log.Info(ex.ToString)\r\n");
                                activationRuleScript.Append("End Try\r\n");
                                activationRuleScript.Append("Return Matched\r\n");
                                activationRuleScript.Append("\r\n");
                                activationRuleScript.Append("End Function\r\n");
                                activationRuleScript.Append("End Class\r\n");

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} class wrapped as {activationRuleScript}.");
                                }

                                var activationRuleScriptHash = Hash.GetHash(activationRuleScript.ToString());

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash}, will now check if it is in the hash cache.");
                                }

                                if (HashCacheAssembly.TryGetValue(activationRuleScriptHash, out var valueHash))
                                {
                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and been located in the hash cache to be assigned to a delegate.");
                                    }

                                    entityAnalysisModelAbstractionCalculation.FunctionCalculationCompile =
                                        valueHash;

                                    var classType =
                                        entityAnalysisModelAbstractionCalculation.FunctionCalculationCompile.GetType(
                                            "CalculationRule");
                                    var methodInfo = classType.GetMethod("Match");
                                    entityAnalysisModelAbstractionCalculation.FunctionCalculationCompileDelegate =
                                        (EntityAnalysisModelAbstractionCalculation.Match)Delegate.CreateDelegate(
                                            typeof(EntityAnalysisModelAbstractionCalculation.Match), methodInfo);

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash}, assigned to a delegate from the hash cache and added to a shadow list of Activation Rules.");
                                    }
                                }
                                else
                                {
                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and has not been located in the hash cache, hence it will be compiled.");
                                    }

                                    var compile = new Compile();
                                    compile.CompileCode(activationRuleScript.ToString(), Log,
                                    [
                                        Path.Combine(strPath ?? throw new InvalidOperationException(), "log4net.dll"),
                                        Path.Combine(strPath, "Jube.Dictionary.dll")
                                    ]);

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: {key} and Abstraction Rule Model {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and compiled with {compile.Errors}.");
                                    }

                                    if (compile.Errors == 0)
                                    {
                                        if (Log.IsDebugEnabled)
                                        {
                                            Log.Debug(
                                                $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  it will now be allocated to a delegate.");
                                        }

                                        entityAnalysisModelAbstractionCalculation.FunctionCalculationCompile =
                                            compile.CompiledAssembly;

                                        var classType =
                                            entityAnalysisModelAbstractionCalculation.FunctionCalculationCompile
                                                .GetType("CalculationRule");
                                        var methodInfo = classType.GetMethod("Match");
                                        entityAnalysisModelAbstractionCalculation.FunctionCalculationCompileDelegate =
                                            (EntityAnalysisModelAbstractionCalculation.Match)
                                            Delegate.CreateDelegate(
                                                typeof(EntityAnalysisModelAbstractionCalculation.Match), methodInfo);

                                        HashCacheAssembly.Add(activationRuleScriptHash, compile.CompiledAssembly);

                                        if (Log.IsDebugEnabled)
                                        {
                                            Log.Debug(
                                                $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  allocated to a delegate and added to a shadow list of Calculations.");
                                        }
                                    }
                                    else
                                    {
                                        if (Log.IsDebugEnabled)
                                        {
                                            Log.Debug(
                                                $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} but has failed to load.");
                                        }
                                    }
                                }
                            }

                            shadowEntityAnalysisModelAbstractionCalculation.Add(
                                entityAnalysisModelAbstractionCalculation);

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been added to a shadow list of Abstraction Calculations.");
                            }

                            parser.EntityAnalysisModelAbstractionCalculations.TryAdd(
                                entityAnalysisModelAbstractionCalculation.Name);

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has added {entityAnalysisModelAbstractionCalculation.Name} to parser.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Calculation {record.Id} has not been added to a shadow list of Abstraction Calculations as it is not active.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Abstraction Calculation ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }
                }

                value.EntityAnalysisModelAbstractionCalculations =
                    shadowEntityAnalysisModelAbstractionCalculation;

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Model {key} has overwritten the current Abstraction Calculations with the shadow list of Abstraction Calculations.");
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Completed adding Abstraction Calculations to entity models.");
            }
        }

        private void SyncEntityAnalysisModelInlineFunctions(DbContext dbContext, string strPath, Parser parser)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose determining adding Inline Functions.");
                }

                var repository = new EntityAnalysisModelInlineFunctionRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelInlineFunctionRepository.GetByEntityAnalysisModelId for entity model key of {key}.");
                }

                var records = repository.GetByEntityAnalysisModelIdOrderById(key);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug("Returned all Inline Functions from the database.");
                }

                var shadowEntityAnalysisModelInlineFunctions = new List<EntityAnalysisModelInlineFunction>();
                foreach (var record in records)
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Inline Function ID {record.Id} returned for model {key}.");
                        }

                        if (record.Active.Value != 1)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Inline_Function ID {record.Id} returned for model {key} is active.");
                        }

                        var entityAnalysisModelInlineFunction = new EntityAnalysisModelInlineFunction
                        {
                            Id = record.Id
                        };

                        if (record.Name == null)
                        {
                            entityAnalysisModelInlineFunction.Name =
                                $"Inline_Function_{entityAnalysisModelInlineFunction.Id}";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Function {entityAnalysisModelInlineFunction.Id} set DEFAULT Name as {entityAnalysisModelInlineFunction.Name}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelInlineFunction.Name = record.Name.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Inline Function {entityAnalysisModelInlineFunction.Id} set Name as {entityAnalysisModelInlineFunction.Name}.");
                            }
                        }

                        if (!record.ResponsePayload.HasValue)
                        {
                            entityAnalysisModelInlineFunction.ResponsePayload = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Function {entityAnalysisModelInlineFunction.Id} set DEFAULT Response Payload as {entityAnalysisModelInlineFunction.ResponsePayload}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelInlineFunction.ResponsePayload = record.ResponsePayload.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Function {entityAnalysisModelInlineFunction.Id} set Response Payload as {entityAnalysisModelInlineFunction.ResponsePayload}.");
                            }
                        }

                        if (!record.ReportTable.HasValue)
                        {
                            entityAnalysisModelInlineFunction.ReportTable = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Function Type {entityAnalysisModelInlineFunction.Id} set DEFAULT Promote Report Table as {entityAnalysisModelInlineFunction.ReportTable}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelInlineFunction.ReportTable = record.ReportTable.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Function Type {entityAnalysisModelInlineFunction.Id} set Promote Report Table as {entityAnalysisModelInlineFunction.ReportTable}.");
                            }
                        }

                        if (!record.ReturnDataTypeId.HasValue)
                        {
                            entityAnalysisModelInlineFunction.ReturnDataTypeId = 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Function Type {entityAnalysisModelInlineFunction.Id} set DEFAULT Return Type as {entityAnalysisModelInlineFunction.ReturnDataTypeId}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelInlineFunction.ReturnDataTypeId = record.ReturnDataTypeId.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Function Type {entityAnalysisModelInlineFunction.Id} set Return Type as {entityAnalysisModelInlineFunction.ReturnDataTypeId}.");
                            }
                        }

                        var hasRuleScript = false;
                        if (record.FunctionScript != null)
                        {
                            var parsedRule = new ParsedRule
                            {
                                OriginalRuleText = record.FunctionScript,
                                ErrorSpans = []
                            };
                            parsedRule = parser.TranslateFromDotNotation(parsedRule);
                            parsedRule = parser.Parse(parsedRule);

                            if (parsedRule.ErrorSpans.Count == 0)
                            {
                                entityAnalysisModelInlineFunction.FunctionScript = parsedRule.ParsedRuleText;
                                hasRuleScript = true;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and calculation {entityAnalysisModelInlineFunction.Id} set  script as {entityAnalysisModelInlineFunction.FunctionScript}.");
                                }
                            }
                            else
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and calculation {entityAnalysisModelInlineFunction.Id} set soft parse security error for script as {entityAnalysisModelInlineFunction.FunctionScript}.");
                                }
                            }
                        }

                        if (hasRuleScript)
                        {
                            var activationRuleScript = new StringBuilder();
                            activationRuleScript.Append("Imports System.IO\r\n");
                            activationRuleScript.Append("Imports log4net\r\n");
                            activationRuleScript.Append("Imports System.Net\r\n");
                            activationRuleScript.Append("Imports System.Collections.Generic\r\n");
                            activationRuleScript.Append("Imports Jube.Dictionary\r\n");
                            activationRuleScript.Append("Imports System\r\n");
                            activationRuleScript.Append("Public Class InlineFunction\r\n");
                            activationRuleScript.Append(
                                "Public Shared Function Match(Data As DictionaryNoBoxing,List As Dictionary(Of String, List(Of String)),KVP As PooledDictionary(Of String, Double),Log as ILog) As Object\r\n");
                            activationRuleScript.Append("Dim Matched as Object = Nothing");
                            activationRuleScript.Append("\r\n");
                            activationRuleScript.Append("Try\r\n");
                            activationRuleScript.Append(entityAnalysisModelInlineFunction.FunctionScript + "\r\n");
                            activationRuleScript.Append("Catch ex As Exception\r\n");
                            activationRuleScript.Append("Log.Info(ex.ToString)\r\n");
                            activationRuleScript.Append("End Try\r\n");
                            activationRuleScript.Append("Return Matched\r\n");
                            activationRuleScript.Append("\r\n");
                            activationRuleScript.Append("End Function\r\n");
                            activationRuleScript.Append("End Class\r\n");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} class wrapped as {activationRuleScript}.");
                            }

                            var activationRuleScriptHash = Hash.GetHash(activationRuleScript.ToString());

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash}, will now check if it is in the hash cache.");
                            }

                            if (HashCacheAssembly.TryGetValue(activationRuleScriptHash, out var valueHash))
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and been located in the hash cache to be assigned to a delegate.");
                                }

                                entityAnalysisModelInlineFunction.FunctionCalculationCompile =
                                    valueHash;

                                var classType =
                                    entityAnalysisModelInlineFunction.FunctionCalculationCompile.GetType(
                                        "InlineFunction");
                                var methodInfo = classType.GetMethod("Match");
                                entityAnalysisModelInlineFunction.FunctionCalculationCompileDelegate =
                                    (EntityAnalysisModelInlineFunction.Match)Delegate.CreateDelegate(
                                        typeof(EntityAnalysisModelInlineFunction.Match),
                                        methodInfo);

                                shadowEntityAnalysisModelInlineFunctions.Add(entityAnalysisModelInlineFunction);

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash}, assigned to a delegate from the hash cache and added to a shadow list of Inline Functions.");
                                }
                            }
                            else
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and has not been located in the hash cache, hence it will be compiled.");
                                }

                                var compile = new Compile();
                                compile.CompileCode(activationRuleScript.ToString(), Log,
                                [
                                    Path.Combine(strPath ?? throw new InvalidOperationException(), "log4net.dll"),
                                    Path.Combine(strPath, "Jube.Dictionary.dll")
                                ]);

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Abstraction Rule Model {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and compiled with {compile.Errors}.");
                                }

                                if (compile.Errors == 0)
                                {
                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: {key} and Calculation {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  it will now be allocated to a delegate.");
                                    }

                                    entityAnalysisModelInlineFunction.FunctionCalculationCompile =
                                        compile.CompiledAssembly;

                                    var classType =
                                        entityAnalysisModelInlineFunction.FunctionCalculationCompile.GetType(
                                            "InlineFunction");
                                    var methodInfo = classType.GetMethod("Match");
                                    entityAnalysisModelInlineFunction.FunctionCalculationCompileDelegate =
                                        (EntityAnalysisModelInlineFunction.Match)Delegate.CreateDelegate(
                                            typeof(EntityAnalysisModelInlineFunction.Match),
                                            methodInfo);

                                    HashCacheAssembly.Add(activationRuleScriptHash, compile.CompiledAssembly);
                                    shadowEntityAnalysisModelInlineFunctions.Add(entityAnalysisModelInlineFunction);

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: {key} and Calculation {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  allocated to a delegate and added to a shadow list of Inline Functions.");
                                    }
                                }
                                else
                                {
                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: {key} and Calculation {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} but has failed to load.");
                                    }
                                }
                            }
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model {key} and Function {entityAnalysisModelInlineFunction.Id} has been added to a shadow list of Inline Functions.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Inline Function ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }
                }

                value.EntityAnalysisModelInlineFunctions = shadowEntityAnalysisModelInlineFunctions;
                value.DictionaryNoBoxingInitialSize = CalculateDictionaryNoBoxingInitialSize(value);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Model {key} has overwritten the current Inline Functions with the shadow list of Inline Functions.");
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Completed adding Inline Functions to entity models.");
            }
        }

        private void SyncEntityAnalysisModelGatewayRules(DbContext dbContext, string strPath, Parser parser)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Checking if model {key} is started for the purpose determining compiling the Gateway rules.");
                }

                var repository = new EntityAnalysisModelGatewayRuleRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelGatewayRuleRepository.GetByEntityAnalysisModelId for entity model key of {key}.");
                }

                var records = repository.GetByEntityAnalysisModelIdOrderById(key);

                var shadowEntityModelGatewayRule = new List<EntityModelGatewayRule>();
                foreach (var record in records)
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Activation Rule ID {record.Id} returned for model {key}.");
                        }

                        if (record.Active != 1)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Activation Rule ID {record.Id} returned for model {key} is active.");
                        }

                        var modelGatewayRule = new EntityModelGatewayRule
                        {
                            EntityAnalysisModelGatewayRuleId = record.Id
                        };

                        if (record.Name != null)
                        {
                            modelGatewayRule.Name = record.Name.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set Model Gateway Rule value set as {modelGatewayRule.Name}.");
                            }
                        }
                        else
                        {
                            modelGatewayRule.Name =
                                $"Gateway_Rule_{modelGatewayRule.EntityAnalysisModelGatewayRuleId}";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set DEFAULT Model Gateway Rule value set as {modelGatewayRule.Name}.");
                            }
                        }

                        if (record.RuleScriptTypeId.HasValue)
                        {
                            modelGatewayRule.RuleScriptTypeId = record.RuleScriptTypeId.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set Model Gateway Rule Script Type ID value set as {modelGatewayRule.RuleScriptTypeId}.");
                            }
                        }
                        else
                        {
                            modelGatewayRule.RuleScriptTypeId = 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set DEFAULT Model Gateway Rule Script Type ID value set as {modelGatewayRule.RuleScriptTypeId}.");
                            }
                        }

                        if (record.MaxResponseElevation.HasValue)
                        {
                            modelGatewayRule.MaxResponseElevation = record.MaxResponseElevation.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set Model Max Response Elevation value set as {modelGatewayRule.MaxResponseElevation}.");
                            }
                        }
                        else
                        {
                            modelGatewayRule.MaxResponseElevation = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set DEFAULT Model Max Response Elevation value set as {modelGatewayRule.MaxResponseElevation}.");
                            }
                        }

                        if (record.GatewaySample.HasValue)
                        {
                            modelGatewayRule.GatewaySample = record.GatewaySample.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set Model Gateway Sample value set as {modelGatewayRule.GatewaySample}.");
                            }
                        }
                        else
                        {
                            modelGatewayRule.GatewaySample = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set DEFAULT Model Gateway Sample value set as {modelGatewayRule.GatewaySample}.");
                            }
                        }

                        var hasRuleScript = false;
                        if (record.BuilderRuleScript != null && modelGatewayRule.RuleScriptTypeId == 1)
                        {
                            var parsedRule = new ParsedRule
                            {
                                OriginalRuleText = record.BuilderRuleScript,
                                ErrorSpans = []
                            };
                            parsedRule = parser.TranslateFromDotNotation(parsedRule);
                            parsedRule = parser.Parse(parsedRule);

                            if (parsedRule.ErrorSpans.Count == 0)
                            {
                                modelGatewayRule.GatewayRuleScript = parsedRule.ParsedRuleText;
                                hasRuleScript = true;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set builder script as {modelGatewayRule.GatewayRuleScript}.");
                                }
                            }
                        }
                        else if (record.CoderRuleScript != null && modelGatewayRule.RuleScriptTypeId == 2)
                        {
                            var parsedRule = new ParsedRule
                            {
                                OriginalRuleText = record.CoderRuleScript,
                                ErrorSpans = []
                            };
                            parsedRule = parser.TranslateFromDotNotation(parsedRule);
                            parsedRule = parser.Parse(parsedRule);

                            if (parsedRule.ErrorSpans.Count == 0)
                            {
                                modelGatewayRule.GatewayRuleScript = parsedRule.ParsedRuleText;
                                hasRuleScript = true;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set coder script as {modelGatewayRule.GatewayRuleScript}.");
                                }
                            }
                        }

                        if (!hasRuleScript)
                        {
                            continue;
                        }

                        var gatewayRuleScript = new StringBuilder();
                        gatewayRuleScript.Append("Imports System.IO\r\n");
                        gatewayRuleScript.Append("Imports log4net\r\n");
                        gatewayRuleScript.Append("Imports System.Net\r\n");
                        gatewayRuleScript.Append("Imports System.Collections.Generic\r\n");
                        gatewayRuleScript.Append("Imports Jube.Dictionary\r\n");
                        gatewayRuleScript.Append("Imports System\r\n");
                        gatewayRuleScript.Append("Public Class GatewayRule\r\n");
                        gatewayRuleScript.Append(
                            "Public Shared Function Match(Data As DictionaryNoBoxing,List As Dictionary(Of String, List(Of String)),KVP As PooledDictionary(Of String, Double),Log as ILog) As Boolean\r\n");
                        gatewayRuleScript.Append("Dim Matched as Boolean\r\n");
                        gatewayRuleScript.Append("Try\r\n");
                        gatewayRuleScript.Append(modelGatewayRule.GatewayRuleScript + "\r\n");
                        gatewayRuleScript.Append("Catch ex As Exception\r\n");
                        gatewayRuleScript.Append("Log.Info(ex.ToString)\r\n");
                        gatewayRuleScript.Append("End Try\r\n");
                        gatewayRuleScript.Append("Return Matched\r\n");
                        gatewayRuleScript.Append("\r\n");
                        gatewayRuleScript.Append("End Function\r\n");
                        gatewayRuleScript.Append("End Class\r\n");

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set class wrap as {gatewayRuleScript}.");
                        }

                        var gatewayRuleScriptHash = Hash.GetHash(gatewayRuleScript.ToString());

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} and will be checked against the hash cache.");
                        }

                        if (HashCacheAssembly.TryGetValue(gatewayRuleScriptHash, out var valueHash))
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} exists in the hash cache and will be allocated to a delegate.");
                            }

                            modelGatewayRule.GatewayRuleCompile = valueHash;

                            var classType = modelGatewayRule.GatewayRuleCompile.GetType("GatewayRule");
                            var methodInfo = classType.GetMethod("Match");
                            modelGatewayRule.GatewayRuleCompileDelegate =
                                (EntityModelGatewayRule.Match)Delegate.CreateDelegate(
                                    typeof(EntityModelGatewayRule.Match), methodInfo);

                            shadowEntityModelGatewayRule.Add(modelGatewayRule);

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} exists in the hash cache, has been allocated a to a delegate and placed in a shadow list of gateway rules.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} has not been found in the hash cache and will now be compiled.");
                            }

                            var compile = new Compile();
                            compile.CompileCode(gatewayRuleScript.ToString(), Log,
                            [
                                Path.Combine(strPath ?? throw new InvalidOperationException(), "log4net.dll"),
                                Path.Combine(strPath, "Jube.Dictionary.dll")
                            ]);

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} has now been compiled with {compile.Errors} errors.");
                            }

                            if (compile.Errors == 0)
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} has now been compiled without error,  a delegate will now be allocated.");
                                }

                                modelGatewayRule.GatewayRuleCompile = compile.CompiledAssembly;

                                var classType = modelGatewayRule.GatewayRuleCompile.GetType("GatewayRule");
                                var methodInfo = classType.GetMethod("Match");
                                modelGatewayRule.GatewayRuleCompileDelegate =
                                    (EntityModelGatewayRule.Match)Delegate.CreateDelegate(
                                        typeof(EntityModelGatewayRule.Match), methodInfo);
                                shadowEntityModelGatewayRule.Add(modelGatewayRule);
                                HashCacheAssembly.Add(gatewayRuleScriptHash, compile.CompiledAssembly);

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} has now been compiled without error,  a delegate has been allocated,  added to hash cache and added to a shadow list of gateway rules.");
                                }
                            }
                            else
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} failed to load.");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Activation Rule ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }
                }

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: {key} is being finished,  proceeding to update gateway rules and close off the cursor.");
                }

                value.ModelGatewayRules = shadowEntityModelGatewayRule;

                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Entity Start: {key} replaced Gateway Rule List with shadow gateway rules.");
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Completed adding Gateway Rules to entity models.");
            }
        }

        private void UpdateGatewayRuleCounter(DbContext dbContext, EntityModelGatewayRule gatewayRule)
        {
            try
            {
                var repository = new EntityAnalysisModelGatewayRuleRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelGatewayRuleRepository.EntityAnalysisModelGatewayRuleId for Gateway Rule ID of {gatewayRule.EntityAnalysisModelGatewayRuleId} and counter of {gatewayRule.Counter}.");
                }

                repository.UpdateCounter(gatewayRule.EntityAnalysisModelGatewayRuleId, gatewayRule.Counter);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Finished EntityAnalysisModelGatewayRuleRepository.EntityAnalysisModelGatewayRuleId for Gateway Rule ID of {gatewayRule.EntityAnalysisModelGatewayRuleId} and has reset counter of {gatewayRule.Counter}.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"Entity Start: Gateway Rule ID {gatewayRule.EntityAnalysisModelGatewayRuleId} has created an error as {ex} on update counter.");
            }
            finally
            {
                gatewayRule.Counter = 0;
            }
        }

        private void SyncEntityAnalysisModelActivationRules(DbContext dbContext, string strPath, Parser parser)
        {
            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Looping through active models {key} is started for the purpose adding the Activation Rules.");
                }

                var repository = new EntityAnalysisModelActivationRuleRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelActivationRuleRepository.GetByEntityAnalysisModelIdInPriorityOrder for entity model key of {key}.");
                }

                var records = repository.GetByEntityAnalysisModelIdInPriorityOrder(key);

                var shadowEntityModelActivationRule = new List<EntityAnalysisModelActivationRule>();
                foreach (var record in records)
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Activation Rule ID {record.Id} returned for model {key}.");
                        }

                        bool active;
                        if (record.Active == 1)
                        {
                            active = true;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Active value set as {active}.");
                            }
                        }
                        else
                        {
                            active = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and DEFAULT Activation Rule {record.Id} set Active value set as {active}.");
                            }
                        }

                        bool approval;
                        if (record.ReviewStatusId.HasValue)
                        {
                            switch (record.ReviewStatusId.Value)
                            {
                                case 0:
                                    approval = false;

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 0 set as {approval}.");
                                    }

                                    break;
                                case 1:
                                    approval = false;

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 1 set as {approval}.");
                                    }

                                    break;
                                case 2:
                                    approval = false;

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 2 set as {approval}.");
                                    }

                                    break;
                                case 3:
                                    approval = false;

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 3 set as {approval}.");
                                    }

                                    break;
                                case 4:
                                    approval = true;

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 4 set as {approval}.");
                                    }

                                    break;
                                default:
                                    approval = false;

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set Approval value 0 set as {approval}.");
                                    }

                                    break;
                            }
                        }
                        else
                        {
                            approval = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {record.Id} set DEFAULT Approval value set as {approval}.");
                            }
                        }

                        if (!active || !approval)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Entity Model {key} and Activation Rule {record.Id} is Active and Approved. Proceeding to build Activation Rule.");
                        }

                        var modelActivationRule = new EntityAnalysisModelActivationRule
                        {
                            Id = record.Id
                        };

                        if (record.ReportTable.HasValue)
                        {
                            modelActivationRule.ReportTable =
                                record.ReportTable.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Promote Report Table value set as {modelActivationRule.ReportTable}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.ReportTable = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Promote Report Table value set as {modelActivationRule.ReportTable}.");
                            }
                        }

                        if (record.Name != null)
                        {
                            modelActivationRule.Name = record.Name.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Name value set as {modelActivationRule.Name}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.Name =
                                $"Activation_Rule_{modelActivationRule.Id}";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Name value set as {modelActivationRule.Name}.");
                            }
                        }

                        if (record.ResponsePayload.HasValue)
                        {
                            modelActivationRule.ResponsePayload = record.ResponsePayload == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Payload value set as {modelActivationRule.ResponsePayload}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.ResponsePayload = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Payload value set as {modelActivationRule.ResponsePayload}.");
                            }
                        }

                        if (record.Visible.HasValue)
                        {
                            modelActivationRule.Visible = record.Visible.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Visible value set as {modelActivationRule.Visible}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.Visible = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Visible value set as {modelActivationRule.Visible}.");
                            }
                        }

                        if (record.EnableReprocessing.HasValue)
                        {
                            modelActivationRule.EnableReprocessing = record.EnableReprocessing == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Reprocessing value set as {modelActivationRule.EnableReprocessing}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.EnableReprocessing = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Reprocessing value set as {modelActivationRule.EnableReprocessing}.");
                            }
                        }

                        if (record.ResponseElevationForeColor != null)
                        {
                            modelActivationRule.ResponseElevationForeColor = record.ResponseElevationForeColor;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Elevation Fore Color value set as {modelActivationRule.ResponseElevationForeColor}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.ResponseElevationForeColor = "#000000";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Elevation Fore Color value set as {modelActivationRule.ResponseElevationForeColor}.");
                            }
                        }

                        if (record.BypassSuspendInterval != null)
                        {
                            modelActivationRule.BypassSuspendInterval = record.BypassSuspendInterval.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Bypass Suspend Interval set as {modelActivationRule.BypassSuspendInterval}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.BypassSuspendInterval = 'd';

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Bypass Suspend Interval set as {modelActivationRule.BypassSuspendInterval}.");
                            }
                        }

                        if (record.BypassSuspendValue.HasValue)
                        {
                            modelActivationRule.BypassSuspendValue = record.BypassSuspendValue.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Bypass Suspend Value set as {modelActivationRule.BypassSuspendValue}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.BypassSuspendValue = 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Bypass Suspend Value set as {modelActivationRule.BypassSuspendValue}.");
                            }
                        }

                        if (record.BypassSuspendSample.HasValue)
                        {
                            modelActivationRule.BypassSuspendSample = record.BypassSuspendSample.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Bypass Suspend Sample set as {modelActivationRule.BypassSuspendSample}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.BypassSuspendSample = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Bypass Suspend Sample set as {modelActivationRule.BypassSuspendSample}.");
                            }
                        }

                        if (record.ResponseElevationBackColor != null)
                        {
                            modelActivationRule.ResponseElevationBackColor = record.ResponseElevationBackColor;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Elevation Back Color value set as {modelActivationRule.ResponseElevationBackColor}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.ResponseElevationBackColor = "#ffffff";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Elevation Back Color value set as {modelActivationRule.ResponseElevationBackColor}.");
                            }
                        }

                        if (record.EnableCaseWorkflow.HasValue)
                        {
                            modelActivationRule.EnableCaseWorkflow = record.EnableCaseWorkflow.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Enable Cases Workflow value set as {modelActivationRule.EnableCaseWorkflow}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.EnableCaseWorkflow = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Enable Cases Workflow value set as {modelActivationRule.EnableCaseWorkflow}.");
                            }
                        }

                        if (record.EnableNotification.HasValue)
                        {
                            modelActivationRule.EnableNotification = record.EnableNotification == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Enable Notification value set as {modelActivationRule.EnableNotification}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.EnableNotification = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Enable Notification value set as {modelActivationRule.EnableNotification}.");
                            }
                        }

                        if (record.NotificationTypeId.HasValue)
                        {
                            modelActivationRule.NotificationTypeId = record.NotificationTypeId.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Notification Type value set as {modelActivationRule.NotificationTypeId}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.NotificationTypeId = 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Notification Type value set as {modelActivationRule.NotificationTypeId}.");
                            }
                        }

                        if (record.CaseKey != null)
                        {
                            modelActivationRule.CaseKey = record.CaseKey;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Case Key value set as {modelActivationRule.CaseKey}.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Case Key value set as {modelActivationRule.CaseKey}.");
                            }
                        }

                        if (record.ResponseElevationKey != null)
                        {
                            modelActivationRule.ResponseElevationKey = record.ResponseElevationKey;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Elevation Key set as {modelActivationRule.ResponseElevationKey}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.ResponseElevationKey = value.EntryName;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Elevation Key set as {modelActivationRule.ResponseElevationKey}.");
                            }
                        }

                        if (record.NotificationDestination != null)
                        {
                            modelActivationRule.NotificationDestination = record.NotificationDestination;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Notification Destination value set as {modelActivationRule.NotificationDestination}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.NotificationDestination = "";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Notification Destination value set as {modelActivationRule.NotificationDestination}.");
                            }
                        }

                        if (record.NotificationSubject != null)
                        {
                            modelActivationRule.NotificationSubject = record.NotificationSubject;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Notification Subject value set as {modelActivationRule.NotificationSubject}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.NotificationSubject = "";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Notification Subject value set as {modelActivationRule.NotificationSubject}.");
                            }
                        }

                        if (record.NotificationBody != null)
                        {
                            modelActivationRule.NotificationBody =
                                HttpUtility.HtmlDecode(record.NotificationBody);

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Notification Body value set as {modelActivationRule.NotificationBody}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.NotificationBody = "";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Notification Body value set as {modelActivationRule.NotificationBody}.");
                            }
                        }

                        if (record.SendToActivationWatcher.HasValue)
                        {
                            modelActivationRule.SendToActivationWatcher = record.SendToActivationWatcher.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Send To Activation Watcher value set as {modelActivationRule.SendToActivationWatcher}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.SendToActivationWatcher = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Send To Activation Watcher value set as {modelActivationRule.SendToActivationWatcher}.");
                            }
                        }

                        if (record.ResponseElevationContent != null)
                        {
                            modelActivationRule.ResponseElevationContent = record.ResponseElevationContent;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Content value set as {modelActivationRule.ResponseElevationContent}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.ResponseElevationContent = "";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT, not supplied, Response Content value set as {modelActivationRule.ResponseElevationContent}.");
                            }
                        }

                        if (record.ResponseElevationRedirect != null)
                        {
                            try
                            {
                                var uri = new Uri(record.ResponseElevationRedirect);

                                modelActivationRule.ResponseElevationRedirect = record.ResponseElevationRedirect;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set ResponseRedirect value set as {uri.AbsoluteUri} from original {modelActivationRule.ResponseElevationRedirect}.");
                                }
                            }
                            catch (Exception ex)
                            {
                                modelActivationRule.ResponseElevationRedirect =
                                    value.FallbackResponseElevationRedirect;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set FAILED PARSE fallback, ResponseRedirect value set as {modelActivationRule.ResponseElevationRedirect} with exception message of {ex.Message}.");
                                }
                            }

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Redirect value set as {modelActivationRule.ResponseElevationRedirect}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.ResponseElevationRedirect = value.FallbackResponseElevationRedirect;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT, not supplied, ResponseRedirect value set as {modelActivationRule.ResponseElevationRedirect}.");
                            }
                        }

                        if (record.ActivationSample.HasValue)
                        {
                            modelActivationRule.ActivationSample = record.ActivationSample.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Activation Sample value set as {modelActivationRule.ActivationSample}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.ActivationSample = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Activation Sample value set as {modelActivationRule.ActivationSample}.");
                            }
                        }

                        if (record.EnableTtlCounter.HasValue)
                        {
                            modelActivationRule.EnableTtlCounter = record.EnableTtlCounter == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Enable TTL Counter value set as {modelActivationRule.EnableTtlCounter}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.EnableTtlCounter = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Enable TTL Counter value set as {modelActivationRule.EnableTtlCounter}.");
                            }
                        }

                        if (record.EnableResponseElevation.HasValue)
                        {
                            modelActivationRule.EnableResponseElevation = record.EnableResponseElevation == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Enable Response Elevation value set as {modelActivationRule.EnableResponseElevation}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.EnableResponseElevation = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Enable Response Elevation value set as {modelActivationRule.EnableResponseElevation}.");
                            }
                        }

                        if (record.EntityAnalysisModelTtlCounterGuid != Guid.Empty)
                        {
                            modelActivationRule.EntityAnalysisModelTtlCounterGuid =
                                record.EntityAnalysisModelTtlCounterGuid;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set TTL Counter Name value set as {modelActivationRule.EntityAnalysisModelTtlCounterGuid}.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT TTL Counter Name, not supplied, value set as {modelActivationRule.EntityAnalysisModelTtlCounterGuid}.");
                            }
                        }

                        if (record.EntityAnalysisModelGuidTtlCounter != Guid.Empty)
                        {
                            modelActivationRule.EntityAnalysisModelGuidTtlCounter =
                                record.EntityAnalysisModelGuidTtlCounter;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Entity Analysis Model Id Ttl Counter value set as {modelActivationRule.EntityAnalysisModelGuidTtlCounter}.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT TTL Counter Name, not supplied, value set as {modelActivationRule.EntityAnalysisModelTtlCounterGuid}.");
                            }
                        }

                        if (record.ResponseElevation.HasValue)
                        {
                            modelActivationRule.ResponseElevation = record.ResponseElevation.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Response Elevation value set as {modelActivationRule.ResponseElevation}.");
                            }
                        }
                        else
                        {
                            modelActivationRule.ResponseElevation = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Response Elevation value set as {modelActivationRule.ResponseElevation}.");
                            }
                        }

                        if (record.CaseWorkflowGuid != Guid.Empty)
                        {
                            modelActivationRule.CaseWorkflowGuid = record.CaseWorkflowGuid;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Cases Workflow ID value set as {modelActivationRule.CaseWorkflowGuid}.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Cases Workflow ID, not supplied, value set as {modelActivationRule.CaseWorkflowGuid}.");
                            }
                        }

                        if (record.CaseWorkflowStatusGuid != Guid.Empty)
                        {
                            modelActivationRule.CaseWorkflowStatusGuid = record.CaseWorkflowStatusGuid;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Cases Workflow Status Name value set as {modelActivationRule.CaseWorkflowStatusGuid}.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Cases Workflow Status Name, not supplied, value set as {modelActivationRule.CaseWorkflowStatusGuid}.");
                            }
                        }

                        if (record.RuleScriptTypeId.HasValue)
                        {
                            modelActivationRule.RuleScriptTypeId = record.RuleScriptTypeId.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set Rule Script Type ID value set as {modelActivationRule.RuleScriptTypeId}.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Activation Rule {modelActivationRule.Id} set DEFAULT Rule Script Type ID, not supplied, value set as {modelActivationRule.RuleScriptTypeId}.");
                            }
                        }

                        var hasRuleScript = false;
                        if (record.BuilderRuleScript != null && modelActivationRule.RuleScriptTypeId == 1)
                        {
                            /*var rule = Parser.TranslateFromDotNotation(record.BuilderRuleScript,value.EntityAnalysisModelRequestXPaths);
                                if (Parser.Parse(rule))*/

                            var parsedRule = new ParsedRule
                            {
                                OriginalRuleText = record.BuilderRuleScript,
                                ErrorSpans = []
                            };
                            parsedRule = parser.TranslateFromDotNotation(parsedRule);
                            parsedRule = parser.Parse(parsedRule);

                            if (parsedRule.ErrorSpans.Count == 0)
                            {
                                modelActivationRule.ActivationRuleScript = parsedRule.ParsedRuleText;
                                hasRuleScript = true;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule Model {modelActivationRule.Id} set builder script as {modelActivationRule.ActivationRuleScript}.");
                                }
                            }
                        }
                        else if (record.CoderRuleScript != null && modelActivationRule.RuleScriptTypeId == 2)
                        {
                            var parsedRule = new ParsedRule
                            {
                                OriginalRuleText = record.CoderRuleScript,
                                ErrorSpans = []
                            };
                            parsedRule = parser.TranslateFromDotNotation(parsedRule);
                            parsedRule = parser.Parse(parsedRule);

                            if (parsedRule.ErrorSpans.Count == 0)
                            {
                                modelActivationRule.ActivationRuleScript = parsedRule.ParsedRuleText;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Activation Rule Model {modelActivationRule.Id} set coder script as {modelActivationRule.ActivationRuleScript}.");
                                }

                                hasRuleScript = true;
                            }
                        }

                        if (!hasRuleScript)
                        {
                            continue;
                        }

                        var activationRuleScript = new StringBuilder();
                        activationRuleScript.Append("Imports System.IO\r\n");
                        activationRuleScript.Append("Imports log4net\r\n");
                        activationRuleScript.Append("Imports System.Net\r\n");
                        activationRuleScript.Append("Imports System.Collections.Generic\r\n");
                        activationRuleScript.Append("Imports Jube.Dictionary\r\n");
                        activationRuleScript.Append("Imports System\r\n");
                        activationRuleScript.Append("Public Class ActivationRule\r\n");
                        activationRuleScript.Append(
                            "Public Shared Function Match(Data As DictionaryNoBoxing,TTLCounter As PooledDictionary(Of String, Integer),Abstraction As PooledDictionary(Of string,double),HttpAdaptation As PooledDictionary(Of String, Double),ExhaustiveAdaptation As PooledDictionary(Of String, Double),List as Dictionary(Of String,List(Of String)),Calculation As PooledDictionary(Of String, Double),Sanctions As PooledDictionary(Of String, Double),KVP As PooledDictionary(Of String, Double),Log as ILog) As Boolean\r\n");
                        activationRuleScript.Append("Dim Matched as Boolean\r\n");
                        activationRuleScript.Append("Try\r\n");
                        activationRuleScript.Append(modelActivationRule.ActivationRuleScript + "\r\n");
                        activationRuleScript.Append("Catch ex As Exception\r\n");
                        activationRuleScript.Append("Log.Info(ex.ToString)\r\n");
                        activationRuleScript.Append("End Try\r\n");
                        activationRuleScript.Append("Return Matched\r\n");
                        activationRuleScript.Append("\r\n");
                        activationRuleScript.Append("End Function\r\n");
                        activationRuleScript.Append("End Class\r\n");

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} class wrapped as {activationRuleScript}.");
                        }

                        var activationRuleScriptHash = Hash.GetHash(activationRuleScript.ToString());

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash}, will now check if it is in the hash cache.");
                        }

                        if (HashCacheAssembly.TryGetValue(activationRuleScriptHash, out var valueHash))
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and been located in the hash cache to be assigned to a delegate.");
                            }

                            modelActivationRule.ActivationRuleCompile =
                                valueHash;

                            var classType = modelActivationRule.ActivationRuleCompile.GetType("ActivationRule");
                            var methodInfo = classType.GetMethod("Match");
                            modelActivationRule.ActivationRuleCompileDelegate =
                                (EntityAnalysisModelActivationRule.Match)Delegate.CreateDelegate(
                                    typeof(EntityAnalysisModelActivationRule.Match), methodInfo);

                            shadowEntityModelActivationRule.Add(modelActivationRule);

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash}, assigned to a delegate from the hash cache and added to a shadow list of Activation Rules.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and has not been located in the hash cache, hence it will be compiled.");
                            }

                            var compile = new Compile();
                            compile.CompileCode(activationRuleScript.ToString(), Log,
                            [
                                Path.Combine(strPath ?? throw new InvalidOperationException(), "log4net.dll"),
                                Path.Combine(strPath, "Jube.Dictionary.dll")
                            ]);

                            if (Log.IsDebugEnabled)
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and compiled with {compile.Errors}.");
                                }
                            }

                            if (compile.Errors == 0)
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  it will now be allocated to a delegate.");
                                }

                                modelActivationRule.ActivationRuleCompile = compile.CompiledAssembly;

                                var classType =
                                    modelActivationRule.ActivationRuleCompile.GetType("ActivationRule");
                                var methodInfo = classType.GetMethod("Match");
                                modelActivationRule.ActivationRuleCompileDelegate =
                                    (EntityAnalysisModelActivationRule.Match)Delegate.CreateDelegate(
                                        typeof(EntityAnalysisModelActivationRule.Match), methodInfo);
                                shadowEntityModelActivationRule.Add(modelActivationRule);
                                HashCacheAssembly.Add(activationRuleScriptHash, compile.CompiledAssembly);

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Activation Rule Model {modelActivationRule.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  allocated to a delegate and added to a shadow list of Activation Rules.");
                                }
                            }
                            else
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: {key} and Activation Rule Model {record.Id} has been hashed to {activationRuleScriptHash} but has failed to load.");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Activation Rule ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }
                }

                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Entity Start: {key} replaced Activation Rule List with shadow activation rules.");
                }

                value.ModelActivationRules = shadowEntityModelActivationRule;

                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Entity Start: {key} finished updating the counters for the Activation Rules.");
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: finished loading the Activation Rules.");
            }
        }

        private void UpdateActivationRuleCounter(DbContext dbContext, EntityAnalysisModelActivationRule activationRule)
        {
            try
            {
                var repository = new EntityAnalysisModelActivationRuleRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelActivationRuleRepository.UpdateCounter for Activation Rule ID of {activationRule.Id} and counter of {activationRule.Counter}.");
                }

                repository.UpdateCounter(activationRule.Id, activationRule.Counter);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Finished Executing EntityAnalysisModelActivationRuleRepository.UpdateCounter for Activation Rule ID of {activationRule.Id} and has reset counter of {activationRule.Counter}.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"Entity Start: Activation Rule ID {activationRule.Id} has created an error as {ex} on update counter.");
            }
            finally
            {
                activationRule.Counter = 0;
            }
        }

        private void SyncEntityAnalysisModelAbstractionRules(DbContext dbContext, string strPath, Parser parser)
        {
            parser.EntityAnalysisModelsAbstractionRule = [];

            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Looping through active models {key} is started for the purpose adding the Abstraction Rules.");
                }

                var repository = new EntityAnalysisModelAbstractionRuleRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelAbstractionRuleRepository.GetByEntityAnalysisModelId for entity model key of {key}.");
                }

                var records = repository.GetByEntityAnalysisModelIdOrderByIdDesc(key);

                var shadowEntityModelAbstractionRule = new List<EntityAnalysisModelAbstractionRule>();
                var shadowDistinctSearchKeys = value.DistinctSearchKeys;

                foreach (var record in records)
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Abstraction Rules ID {record.Id} returned for model {key}.");
                        }

                        if (record.Active != 1)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Abstraction Rules ID {record.Id} returned for model {key} is active.");
                        }

                        var modelAbstractionRule = new EntityAnalysisModelAbstractionRule
                        {
                            Id = record.Id
                        };

                        if (record.Name != null)
                        {
                            modelAbstractionRule.Name = record.Name.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Name value as {modelAbstractionRule.Name}.");
                            }
                        }
                        else
                        {
                            modelAbstractionRule.Name =
                                $"Abstraction_Rule_{modelAbstractionRule.Id}";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Name value as {modelAbstractionRule.Name}.");
                            }
                        }

                        if (record.RuleScriptTypeId.HasValue)
                        {
                            modelAbstractionRule.RuleScriptTypeId = record.RuleScriptTypeId.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Rule Script Type ID value as {modelAbstractionRule.RuleScriptTypeId}.");
                            }
                        }
                        else
                        {
                            modelAbstractionRule.RuleScriptTypeId = 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Rule Script Type ID value as {modelAbstractionRule.RuleScriptTypeId}.");
                            }
                        }

                        if (record.SearchKey != null)
                        {
                            modelAbstractionRule.SearchKey = record.SearchKey;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Grouping Key value as {modelAbstractionRule.SearchKey}.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Grouping Key value as {modelAbstractionRule.SearchKey}.");
                            }
                        }

                        if (record.ReportTable.HasValue)
                        {
                            modelAbstractionRule.ReportTable = record.ReportTable == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Promote Report Table value as {modelAbstractionRule.ReportTable}.");
                            }
                        }
                        else
                        {
                            modelAbstractionRule.ReportTable = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Promote Report Table value as {modelAbstractionRule.ReportTable}.");
                            }
                        }

                        if (record.ResponsePayload.HasValue)
                        {
                            modelAbstractionRule.ResponsePayload = record.ResponsePayload == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Response Payload value as {modelAbstractionRule.ResponsePayload}.");
                            }
                        }
                        else
                        {
                            modelAbstractionRule.ResponsePayload = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Response Payload value as {modelAbstractionRule.ResponsePayload}.");
                            }
                        }

                        if (record.SearchFunctionKey != null)
                        {
                            modelAbstractionRule.SearchFunctionKey = record.SearchFunctionKey;

                            if (Log.IsDebugEnabled)
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Grouping Function Key value as {modelAbstractionRule.SearchFunctionKey}.");
                                }
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Grouping Function Key value as {modelAbstractionRule.SearchFunctionKey}.");
                            }
                        }

                        if (record.Search.HasValue)
                        {
                            modelAbstractionRule.Search = record.Search.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Search Extrapolation Type value is greater than 1, {modelAbstractionRule.Search}.  Checking to see if already added to the distinct search keys available to this abstraction rule.");
                            }

                            foreach (var requestXPath in value.EntityAnalysisModelRequestXPaths)
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} checking {requestXPath.Name}.");
                                }

                                if (requestXPath.Name != modelAbstractionRule.SearchKey)
                                {
                                    continue;
                                }

                                var distinctSearchKey = new DistinctSearchKey();

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} matched {requestXPath.Name}.");
                                }

                                distinctSearchKey.SearchKeyCacheIntervalType =
                                    requestXPath.SearchKeyCacheInterval;
                                distinctSearchKey.SearchKeyCacheIntervalValue =
                                    requestXPath.SearchKeyCacheValue;
                                distinctSearchKey.SearchKeyCacheTtlIntervalValue =
                                    requestXPath.SearchKeyCacheTtlValue;
                                distinctSearchKey.SearchKeyCache = requestXPath.SearchKeyCache;
                                distinctSearchKey.SearchKeyCacheFetchLimit =
                                    requestXPath.SearchKeyCacheFetchLimit;
                                distinctSearchKey.SearchKey = modelAbstractionRule.SearchKey;
                                distinctSearchKey.SearchKeyCacheSample = requestXPath.SearchKeyCacheSample;
                                distinctSearchKey.SearchKeyTtlInterval = requestXPath.SearchKeyTtlInterval;
                                distinctSearchKey.SearchKeyTtlIntervalValue =
                                    requestXPath.SearchKeyTtlIntervalValue;
                                distinctSearchKey.SearchKeyFetchLimit = requestXPath.SearchKeyFetchLimit;

                                if (!shadowDistinctSearchKeys.ContainsKey(modelAbstractionRule
                                        .SearchKey))
                                {
                                    shadowDistinctSearchKeys.Add(distinctSearchKey.SearchKey,
                                        distinctSearchKey);

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Search Extrapolation Type value is greater than 1, {modelAbstractionRule.Search}.  Not added to distinct search keys,  adding.");
                                    }
                                }
                                else
                                {
                                    shadowDistinctSearchKeys[distinctSearchKey.SearchKey] = distinctSearchKey;

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Search Extrapolation Type value is greater than 1, {modelAbstractionRule.Search}.  Already exists.  Updating.");
                                    }
                                }

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} matched {requestXPath.Name} and added the grouping key to the distinct list being used by the rule.  Will not check any further in the available XPath.");
                                }

                                break;
                            }
                        }
                        else
                        {
                            modelAbstractionRule.Search = true;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Abstraction Rule Search Extrapolation Type value as {modelAbstractionRule.Search}.");
                            }
                        }

                        if (record.Offset.HasValue)
                        {
                            modelAbstractionRule.EnableOffset = record.Offset.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Enable Offset value as {modelAbstractionRule.EnableOffset}.");
                            }
                        }
                        else
                        {
                            modelAbstractionRule.EnableOffset = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Enable Offset value as {modelAbstractionRule.EnableOffset}.");
                            }
                        }

                        if (record.OffsetTypeId.HasValue)
                        {
                            modelAbstractionRule.OffsetType = record.OffsetTypeId.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Offset Type value as {modelAbstractionRule.OffsetType}.");
                            }
                        }
                        else
                        {
                            modelAbstractionRule.OffsetType = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Offset Type value as {modelAbstractionRule.OffsetType}.");
                            }
                        }

                        if (record.OffsetValue.HasValue)
                        {
                            modelAbstractionRule.OffsetValue = record.OffsetValue.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Offset value as {modelAbstractionRule.OffsetValue}.");
                            }
                        }
                        else
                        {
                            modelAbstractionRule.OffsetValue = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Offset value as {modelAbstractionRule.OffsetValue}.");
                            }
                        }

                        if (record.SearchInterval != null)
                        {
                            modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType =
                                record.SearchInterval;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Aggregation Function Interval Type value as {modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType}.");
                            }
                        }
                        else
                        {
                            modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType = "d";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Abstraction Rule Aggregation Function Interval Type value as {modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType}.");
                            }
                        }

                        if (record.SearchInterval != null)
                        {
                            modelAbstractionRule.AbstractionHistoryIntervalValue =
                                record.SearchValue;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Aggregation Function Interval Value value as {modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType}.");
                            }
                        }
                        else
                        {
                            modelAbstractionRule.AbstractionHistoryIntervalValue = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Abstraction Rule Aggregation Function Interval Value value as {modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType}.");
                            }
                        }

                        if (record.SearchFunctionTypeId.HasValue)
                        {
                            modelAbstractionRule.AbstractionRuleAggregationFunctionType =
                                record.SearchFunctionTypeId.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Aggregation Function Type Value as {modelAbstractionRule.AbstractionRuleAggregationFunctionType}.");
                            }
                        }
                        else
                        {
                            modelAbstractionRule.AbstractionRuleAggregationFunctionType = 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction History Search Function Type Id as {modelAbstractionRule.AbstractionRuleAggregationFunctionType}.");
                            }
                        }

                        var hasRuleScript = false;
                        var parsedRule = new ParsedRule
                        {
                            ErrorSpans = []
                        };

                        if (record.BuilderRuleScript != null && modelAbstractionRule.RuleScriptTypeId == 1)
                        {
                            parsedRule.OriginalRuleText = record.BuilderRuleScript;
                            parsedRule = parser.TranslateFromDotNotation(parsedRule);
                            parsedRule = parser.Parse(parsedRule);

                            if (parsedRule.ErrorSpans.Count == 0)
                            {
                                modelAbstractionRule.AbstractionRuleScript = parsedRule.ParsedRuleText;
                                hasRuleScript = true;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} set builder script as {modelAbstractionRule.AbstractionRuleScript}.");
                                }
                            }
                        }
                        else if (record.CoderRuleScript != null && modelAbstractionRule.RuleScriptTypeId == 2)
                        {
                            parsedRule.OriginalRuleText = record.CoderRuleScript;
                            parsedRule = parser.TranslateFromDotNotation(parsedRule);
                            parsedRule = parser.Parse(parsedRule);

                            if (parsedRule.ErrorSpans.Count == 0)
                            {
                                modelAbstractionRule.AbstractionRuleScript = parsedRule.ParsedRuleText;

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} set coder script as {modelAbstractionRule.AbstractionRuleScript}.");
                                }

                                hasRuleScript = true;
                            }
                        }

                        if (modelAbstractionRule.Search)
                        {
                            if (shadowDistinctSearchKeys.ContainsKey(modelAbstractionRule.SearchKey))
                            {
                                if (!shadowDistinctSearchKeys[modelAbstractionRule.SearchKey]
                                        .SelectedPayloadData.ContainsKey(modelAbstractionRule.SearchFunctionKey))
                                {
                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} has recognised {modelAbstractionRule.SearchFunctionKey} as being required of the select given function key with a cast of float.");
                                    }

                                    shadowDistinctSearchKeys[modelAbstractionRule.SearchKey]
                                        .SelectedPayloadData.Add(modelAbstractionRule.SearchFunctionKey,
                                            new SelectedPayloadData
                                            {
                                                Name = modelAbstractionRule.SearchFunctionKey,
                                                DatabaseCast = "::float8",
                                                DefaultValue = "0"
                                            }
                                        );
                                }
                                else
                                {
                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} has recognised {modelAbstractionRule.SearchFunctionKey} already exists for select as function key.");
                                    }
                                }

                                foreach (var selectedPayloadData in parsedRule.SelectedPayloadData)
                                {
                                    if (!shadowDistinctSearchKeys[modelAbstractionRule.SearchKey]
                                            .SelectedPayloadData.ContainsKey(selectedPayloadData.Value.Name))
                                    {
                                        if (Log.IsDebugEnabled)
                                        {
                                            Log.Debug(
                                                $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} has recognised {selectedPayloadData.Value.Name} as being required of the select with a cast of {selectedPayloadData.Value.DatabaseCast}.");
                                        }

                                        shadowDistinctSearchKeys[modelAbstractionRule.SearchKey]
                                            .SelectedPayloadData.Add(selectedPayloadData.Value.Name,
                                                selectedPayloadData.Value);
                                    }
                                    else
                                    {
                                        if (Log.IsDebugEnabled)
                                        {
                                            Log.Debug(
                                                $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} has recognised {selectedPayloadData.Value.Name} already exists for select.");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} has looked for search key {modelAbstractionRule.SearchKey} but it is not there.");
                                }
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} does not need to compile data into a search key as it is not a search abstraction rule.");
                            }
                        }

                        if (!hasRuleScript)
                        {
                            continue;
                        }

                        var abstractionRuleScript = new StringBuilder();
                        abstractionRuleScript.Append("Imports System.IO\r\n");
                        abstractionRuleScript.Append("Imports log4net\r\n");
                        abstractionRuleScript.Append("Imports System.Net\r\n");
                        abstractionRuleScript.Append("Imports System.Collections.Generic\r\n");
                        abstractionRuleScript.Append("Imports Jube.Dictionary\r\n");
                        abstractionRuleScript.Append("Imports System\r\n");
                        abstractionRuleScript.Append("Public Class AbstractionRule\r\n");
                        abstractionRuleScript.Append(
                            "Public Shared Function Match(Data As DictionaryNoBoxing,TTLCounter as Dictionary(Of String,Integer),List as Dictionary(Of String,List(Of String)),KVP as PooledDictionary(of String,Double),Log as ILog) As Boolean\r\n");
                        abstractionRuleScript.Append("Dim Matched as Boolean\r\n");
                        abstractionRuleScript.Append("Try\r\n");
                        abstractionRuleScript.Append(modelAbstractionRule.AbstractionRuleScript + "\r\n");
                        abstractionRuleScript.Append("Catch ex As Exception\r\n");
                        abstractionRuleScript.Append("Log.Info(ex.ToString)\r\n");
                        abstractionRuleScript.Append("End Try\r\n");
                        abstractionRuleScript.Append("Return Matched\r\n");
                        abstractionRuleScript.Append("\r\n");
                        abstractionRuleScript.Append("End Function\r\n");
                        abstractionRuleScript.Append("End Class\r\n");

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} set class as {abstractionRuleScript}.");
                        }

                        var abstractionRuleScriptHash = Hash.GetHash(abstractionRuleScript.ToString());

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} calculated hash as {abstractionRuleScriptHash}.  Checking if in hash cache.");
                        }

                        if (HashCacheAssembly.TryGetValue(abstractionRuleScriptHash, out var valueHash))
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} is in the hash cache,  will create the delegate from this.");
                            }

                            modelAbstractionRule.AbstractionRuleCompile =
                                valueHash;
                            var classType =
                                modelAbstractionRule.AbstractionRuleCompile.GetType("AbstractionRule");
                            var methodInfo = classType.GetMethod("Match");
                            modelAbstractionRule.AbstractionRuleCompileDelegate =
                                (EntityAnalysisModelAbstractionRule.Match)Delegate.CreateDelegate(
                                    typeof(EntityAnalysisModelAbstractionRule.Match), methodInfo);
                            shadowEntityModelAbstractionRule.Add(modelAbstractionRule);

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} created delegate.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} is not in the hash cache,  will proceed to compile.");
                            }

                            var compile = new Compile();
                            compile.CompileCode(abstractionRuleScript.ToString(), Log,
                            [
                                Path.Combine(strPath ?? throw new InvalidOperationException(), "log4net.dll"),
                                Path.Combine(strPath, "Jube.Dictionary.dll")
                            ]);

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} has been compiled with {compile.Errors}.");
                            }

                            if (compile.Errors == 0)
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} has been compiled without error and will proceed to create delegate.");
                                }

                                modelAbstractionRule.AbstractionRuleCompile = compile.CompiledAssembly;

                                var classType =
                                    modelAbstractionRule.AbstractionRuleCompile.GetType("AbstractionRule");
                                var methodInfo = classType.GetMethod("Match");

                                modelAbstractionRule.AbstractionRuleCompileDelegate =
                                    (EntityAnalysisModelAbstractionRule.Match)Delegate.CreateDelegate(
                                        typeof(EntityAnalysisModelAbstractionRule.Match), methodInfo);

                                HashCacheAssembly.Add(abstractionRuleScriptHash, compile.CompiledAssembly);

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} has created delegate and added it to the hash cache.");
                                }

                                shadowEntityModelAbstractionRule.Add(modelAbstractionRule);

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} is being added to the shadow list of Abstraction Rules.");
                                }
                            }
                            else
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} has failed to load.");
                                }
                            }
                        }

                        modelAbstractionRule.LogicHash =
                            Hash.GetHash(modelAbstractionRule.AbstractionRuleScript);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {modelAbstractionRule.LogicHash} has been attached to the rule to avoid duplication in execution of abstraction rules.");
                        }

                        parser.EntityAnalysisModelsAbstractionRule.TryAdd(modelAbstractionRule.Name);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} name {modelAbstractionRule.Name} added to parser");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: Abstraction Rules ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }
                }

                foreach (var distinctSearchKey in shadowDistinctSearchKeys)
                {
                    distinctSearchKey.Value.SqlSelect =
                        $"select a.\"CreatedDate\",a.\"ReferenceDate\" AS \"{value.ReferenceDateName}\"";

                    foreach (var selectedPayloadData in distinctSearchKey.Value.SelectedPayloadData)
                    {
                        distinctSearchKey.Value.SqlSelect +=
                            $",COALESCE((a.\"Json\" -> 'payload' ->> '{selectedPayloadData.Key}'){selectedPayloadData.Value.DatabaseCast}," +
                            $"{selectedPayloadData.Value.DefaultValue}) AS \"{selectedPayloadData.Key}\"";
                    }

                    distinctSearchKey.Value.SqlSelectFrom =
                        " from \"Archive\" a inner join \"EntityAnalysisModel\" e on a.\"EntityAnalysisModelId\" = e.\"Id\""
                        + " where e.\"Guid\" = uuid('"
                        + value.Guid + "') and a.\"Json\" -> 'payload' ->> (@key) = (@value) ";

                    distinctSearchKey.Value.SqlSelectOrderBy =
                        " order by a.\"ReferenceDate\" desc limit (@limit)";
                }

                value.ModelAbstractionRules = shadowEntityModelAbstractionRule;
                value.DistinctSearchKeys = shadowDistinctSearchKeys;

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Entity Model {key} and Abstraction Rule Model has replaced the Abstraction Rule list with the shadow values and closed the reader.");
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Completed adding Abstraction Rules to entity models.");
            }
        }

        private void SyncEntityAnalysisModelRequestXPath(DbContext dbContext, Parser parser)
        {
            parser.EntityAnalysisModelRequestXPaths = new Dictionary<string, EntityAnalysisModelRequestXPath>();

            foreach (var (key, value) in ActiveEntityAnalysisModels)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Looping through active models {key} is started for the purpose adding the XPath.");
                }

                var repository = new EntityAnalysisModelRequestXPathRepository(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelRequestXPathRepository.GetByEntityAnalysisModelId for entity model key of {key}.");
                }

                var records =
                    repository.GetByEntityAnalysisModelIdOrderById(
                        key);

                var shadowEntityAnalysisModelRequestXPath = new List<Model.EntityAnalysisModelRequestXPath>();
                var archivePayloadSql = "select a.\"EntityAnalysisModelInstanceEntryGuid\"," +
                                        $"a.\"CreatedDate\"," +
                                        $"a.\"ReferenceDate\" AS \"{value.ReferenceDateName}\"";
                foreach (var record in records)
                {
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: XPath ID {record.Id} returned for model {key}.");
                        }

                        if (record.Active != 1)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: XPath ID {record.Id} returned for model {key} is active.");
                        }

                        var entityAnalysisModelRequestXPath = new Model.EntityAnalysisModelRequestXPath
                        {
                            Id = record.Id
                        };

                        if (record.Name == null)
                        {
                            entityAnalysisModelRequestXPath.Name =
                                $"XPath_Name_{entityAnalysisModelRequestXPath.Id}";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Name value as {entityAnalysisModelRequestXPath.Name}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.Name = record.Name.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Name value as {entityAnalysisModelRequestXPath.Name}.");
                            }
                        }

                        if (!record.DataTypeId.HasValue)
                        {
                            entityAnalysisModelRequestXPath.DataTypeId = record.DataTypeId.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Data Type value as {entityAnalysisModelRequestXPath.DataTypeId}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.DataTypeId = record.DataTypeId.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Data Type value as {entityAnalysisModelRequestXPath.DataTypeId}.");
                            }
                        }

                        if (!record.ReportTable.HasValue)
                        {
                            entityAnalysisModelRequestXPath.ReportTable = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Promote Report Table value as {entityAnalysisModelRequestXPath.ReportTable}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.ReportTable = record.ReportTable.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Promote Report Table value as {entityAnalysisModelRequestXPath.ReportTable}.");
                            }
                        }

                        if (record.XPath == null)
                        {
                            entityAnalysisModelRequestXPath.XPath = "$." + record.Name.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT XPath value as {entityAnalysisModelRequestXPath.XPath}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.XPath = record.XPath;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set XPath value as {entityAnalysisModelRequestXPath.XPath}.");
                            }
                        }

                        if (record.DefaultValue == null)
                        {
                            switch (entityAnalysisModelRequestXPath.DataTypeId)
                            {
                                case 1:
                                    entityAnalysisModelRequestXPath.DefaultValue = "default";

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default String value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                    }

                                    break;
                                case 2:
                                    entityAnalysisModelRequestXPath.DefaultValue = "0";

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Integer value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                    }

                                    break;
                                case 3:
                                    entityAnalysisModelRequestXPath.DefaultValue = "0";

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Float value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                    }

                                    break;
                                case 4:
                                    entityAnalysisModelRequestXPath.DefaultValue = "0";

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Date value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                    }

                                    break;
                                case 5:
                                    entityAnalysisModelRequestXPath.DefaultValue = "0";

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Boolean value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                    }

                                    break;
                                case 6:
                                    entityAnalysisModelRequestXPath.DefaultValue = "0";

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Latitude value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                    }

                                    break;
                                case 7:
                                    entityAnalysisModelRequestXPath.DefaultValue = "0";

                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug(
                                            $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Longitude value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                    }

                                    break;
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.DefaultValue = record.DefaultValue;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Default value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                            }
                        }

                        if (!record.SearchKey.HasValue)
                        {
                            entityAnalysisModelRequestXPath.SearchKey = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key value as {entityAnalysisModelRequestXPath.SearchKey}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.SearchKey = record.SearchKey.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key value as {entityAnalysisModelRequestXPath.SearchKey}.");
                            }
                        }

                        if (!record.ResponsePayload.HasValue)
                        {
                            entityAnalysisModelRequestXPath.ResponsePayload = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set DEFAULT Response Payload value as {entityAnalysisModelRequestXPath.ResponsePayload}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.ResponsePayload = record.ResponsePayload == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set Response Payload value as {entityAnalysisModelRequestXPath.ResponsePayload}.");
                            }
                        }

                        if (!record.EnableSuppression.HasValue)
                        {
                            entityAnalysisModelRequestXPath.EnableSuppression = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set DEFAULT Enable Suppression value as {entityAnalysisModelRequestXPath.EnableSuppression}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.EnableSuppression = record.EnableSuppression == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set Enable Suppression value as {entityAnalysisModelRequestXPath.EnableSuppression}.");
                            }
                        }

                        if (!record.SearchKeyCache.HasValue)
                        {
                            entityAnalysisModelRequestXPath.SearchKeyCache = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Cache value as {entityAnalysisModelRequestXPath.SearchKeyCache}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.SearchKeyCache = record.SearchKeyCache.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Cache value as {entityAnalysisModelRequestXPath.SearchKeyCache}.");
                            }
                        }

                        if (!record.SearchKeyCacheSample.HasValue)
                        {
                            entityAnalysisModelRequestXPath.SearchKeyCacheSample = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Cache Sample value as {entityAnalysisModelRequestXPath.SearchKeyCacheSample}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.SearchKeyCacheSample = record.SearchKeyCacheSample == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Cache Sample value as {entityAnalysisModelRequestXPath.SearchKeyCacheSample}.");
                            }
                        }

                        if (record.SearchKeyCacheInterval == null)
                        {
                            entityAnalysisModelRequestXPath.SearchKeyCacheInterval = "h";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Cache Interval Type value as {entityAnalysisModelRequestXPath.SearchKeyCacheInterval}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.SearchKeyCacheInterval =
                                record.SearchKeyCacheInterval;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Cache Interval Type value as {entityAnalysisModelRequestXPath.SearchKeyCacheInterval}.");
                            }
                        }

                        if (record.SearchKeyTtlInterval == null)
                        {
                            entityAnalysisModelRequestXPath.SearchKeyTtlInterval = "h";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Interval Type value as {entityAnalysisModelRequestXPath.SearchKeyTtlInterval}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.SearchKeyTtlInterval =
                                record.SearchKeyTtlInterval;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Interval Type value as {entityAnalysisModelRequestXPath.SearchKeyTtlInterval}.");
                            }
                        }

                        if (!record.SearchKeyCacheValue.HasValue)
                        {
                            entityAnalysisModelRequestXPath.SearchKeyCacheValue = 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Interval value as {entityAnalysisModelRequestXPath.SearchKeyCacheValue}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.SearchKeyCacheValue =
                                record.SearchKeyCacheValue.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Interval value as {entityAnalysisModelRequestXPath.SearchKeyCacheValue}.");
                            }
                        }

                        if (!record.SearchKeyCacheTtlValue.HasValue)
                        {
                            entityAnalysisModelRequestXPath.SearchKeyCacheTtlValue = 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Cache TTL Interval value as {entityAnalysisModelRequestXPath.SearchKeyCacheTtlValue}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.SearchKeyCacheTtlValue =
                                record.SearchKeyCacheTtlValue.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Cache TTL Interval value as {entityAnalysisModelRequestXPath.SearchKeyCacheTtlValue}.");
                            }
                        }

                        if (!record.SearchKeyTtlIntervalValue.HasValue)
                        {
                            entityAnalysisModelRequestXPath.SearchKeyTtlIntervalValue = 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key TTL Interval value as {entityAnalysisModelRequestXPath.SearchKeyTtlIntervalValue}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.SearchKeyTtlIntervalValue =
                                record.SearchKeyTtlIntervalValue.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Cache TTL Interval value as {entityAnalysisModelRequestXPath.SearchKeyTtlIntervalValue}.");
                            }
                        }

                        if (!record.SearchKeyCacheFetchLimit.HasValue)
                        {
                            entityAnalysisModelRequestXPath.SearchKeyCacheFetchLimit = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Cache Limit value as {entityAnalysisModelRequestXPath.SearchKeyCacheFetchLimit}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.SearchKeyCacheFetchLimit =
                                record.SearchKeyCacheFetchLimit.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Cache Limit value as {entityAnalysisModelRequestXPath.SearchKeyCacheFetchLimit}.");
                            }
                        }

                        if (!record.SearchKeyFetchLimit.HasValue)
                        {
                            entityAnalysisModelRequestXPath.SearchKeyFetchLimit = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT search limit value as {entityAnalysisModelRequestXPath.SearchKeyFetchLimit}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModelRequestXPath.SearchKeyFetchLimit =
                                record.SearchKeyFetchLimit.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set search limit value as {entityAnalysisModelRequestXPath.SearchKeyFetchLimit}.");
                            }
                        }

                        var databaseType = entityAnalysisModelRequestXPath.DataTypeId switch
                        {
                            2 => "::int",
                            3 => "::float8",
                            4 => "::timestamp",
                            5 => "::boolean",
                            6 => "::float8",
                            7 => "::float8",
                            _ => ""
                        };

                        archivePayloadSql +=
                            $",(a.\"Json\" -> 'payload' ->> '{entityAnalysisModelRequestXPath.Name}'){databaseType} AS \"{entityAnalysisModelRequestXPath.Name}\"";

                        shadowEntityAnalysisModelRequestXPath.Add(entityAnalysisModelRequestXPath);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: XPath ID {entityAnalysisModelRequestXPath.Id} added to shadow list for model {key}.");
                        }

                        if (!parser.EntityAnalysisModelRequestXPaths.ContainsKey(entityAnalysisModelRequestXPath
                                .Name))
                        {
                            parser.EntityAnalysisModelRequestXPaths.Add(entityAnalysisModelRequestXPath.Name,
                                new EntityAnalysisModelRequestXPath
                                {
                                    DataTypeId = entityAnalysisModelRequestXPath.DataTypeId,
                                    DefaultValue = entityAnalysisModelRequestXPath.DefaultValue
                                });
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: XPath ID {entityAnalysisModelRequestXPath.Id} added {key} with data type {entityAnalysisModelRequestXPath.DataTypeId} to parser.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            $"Entity Start: XPath ID {record.Id} returned for model {key} has created an error as {ex}.");
                    }
                }

                value.ArchivePayloadSql = archivePayloadSql + " From \"Archive\" a" +
                                          " inner join \"EntityAnalysisModel\" m on a.\"EntityAnalysisModelId\" = m.\"Id\"  where "
                                                            + "m.\"Guid\" = '" + value.Guid + "'::uuid"
                                                            + " and \"ReferenceDate\" >= (@adjustedStartDate) " +
                                                            "order by m.\"Id\" asc limit (@limit) offset (@skip);";

                value.EntityAnalysisModelRequestXPaths = shadowEntityAnalysisModelRequestXPath;
                value.DictionaryNoBoxingInitialSize = CalculateDictionaryNoBoxingInitialSize(value);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Start: Shadow XPath list set to list of xpath for model {key} and reader closed.");
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Completed adding XPath to entity models.");
            }
        }

        private void SyncEntityAnalysisModels(DbContext dbContext, int tenantRegistryId)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Getting all Entity Models from Database.");
            }

            var repository = new EntityAnalysisModelRepository(dbContext, tenantRegistryId);

            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    "Entity Start: Executing EntityAnalysisModelRepository.Get.");
            }

            var records = repository.Get();

            var listEntityAnalysisModelIdsAdded = new List<int>();
            foreach (var record in records)
            {
                try
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: Model {record.Id} has been returned,  checking to see if it is active.");
                    }

                    if (record.Active == 1)
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model {record.Id} has been returned, is active. Proceeding to build model.");
                        }

                        EntityAnalysisModel entityAnalysisModel;

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Checking to see if Model {record.Id} exists in the list of Active Models.");
                        }

                        if (!ActiveEntityAnalysisModels.TryGetValue(record.Id, out var model))
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {record.Id} does not exist in the list of Active Models and is being created.");
                            }

                            entityAnalysisModel = new EntityAnalysisModel
                            {
                                EntityAnalysisInstanceGuid = entityAnalysisInstanceGuid,
                                SanctionsEntries = SanctionsEntries,
                                Log = Log,
                                PersistToActivationWatcherAsync = PersistToActivationWatcherAsync,
                                PendingTagging = PendingTagging,
                                JubeEnvironment = JubeEnvironment,
                                ContractResolver = ContractResolver,
                                CacheService = CacheService
                            };
                        }
                        else
                        {
                            entityAnalysisModel = model;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {record.Id} does exist in the list of Active Models and is being updated.");
                            }
                        }

                        entityAnalysisModel.Id = record.Id;
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Model {record.Id} with Entity Analysis Model ID value {entityAnalysisModel.Id}.");
                        }

                        if (record.Name == null)
                        {
                            entityAnalysisModel.Name = "";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Name value of {entityAnalysisModel.Name}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.Name = record.Name.Replace(" ", "_");

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with Name value of {entityAnalysisModel.Name}.");
                            }
                        }

                        if (record.EntryXPath == null)
                        {
                            entityAnalysisModel.EntryXPath = "";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Entry XPath value of {entityAnalysisModel.EntryXPath}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.EntryXPath = record.EntryXPath;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with Entry XPath value of {entityAnalysisModel.EntryXPath}.");
                            }
                        }

                        if (record.ReferenceDateXPath == null)
                        {
                            entityAnalysisModel.ReferenceDateXpath = "";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Reference Date XPath value of {entityAnalysisModel.ReferenceDateXpath}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.ReferenceDateXpath = record.ReferenceDateXPath;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with Reference Date XPath value of {entityAnalysisModel.ReferenceDateXpath}.");
                            }
                        }

                        if (record.EntryName == null)
                        {
                            entityAnalysisModel.EntryName = "";
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Entry Name value of {entityAnalysisModel.EntryName}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.EntryName = record.EntryName;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with Entry Name value of {entityAnalysisModel.EntryName}.");
                            }
                        }

                        if (record.ReferenceDateName == null)
                        {
                            entityAnalysisModel.ReferenceDateName = "";

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Reference Data Name value of {entityAnalysisModel.ReferenceDateName}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.ReferenceDateName = record.ReferenceDateName;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with Reference Date Name value of {entityAnalysisModel.ReferenceDateName}.");
                            }
                        }

                        if (record.ReferenceDatePayloadLocationTypeId.HasValue)
                        {
                            entityAnalysisModel.ReferenceDatePayloadLocationTypeId =
                                record.ReferenceDatePayloadLocationTypeId.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with Reference Date Payload Location of {entityAnalysisModel.ReferenceDatePayloadLocationTypeId}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.ReferenceDatePayloadLocationTypeId = 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Reference Date Payload Location of {entityAnalysisModel.ReferenceDatePayloadLocationTypeId}.");
                            }
                        }

                        if (record.EnableCache.HasValue)
                        {
                            entityAnalysisModel.EnableCache = record.EnableCache == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with Allow Entity Cache of {entityAnalysisModel.EnableCache}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.EnableCache = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Allow Entity Cache of {entityAnalysisModel.EnableCache}.");
                            }
                        }

                        if (record.EnableActivationWatcher.HasValue)
                        {
                            entityAnalysisModel.EnableActivationWatcher = record.EnableActivationWatcher == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with Enable Activation Watcher of {entityAnalysisModel.EnableActivationWatcher}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.EnableActivationWatcher = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Enable Activation Watcher of {entityAnalysisModel.EnableActivationWatcher}.");
                            }
                        }

                        if (record.EnableResponseElevationLimit.HasValue)
                        {
                            entityAnalysisModel.EnableResponseElevationLimit = record.EnableResponseElevationLimit == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with Enable Response Elevation Limit of {entityAnalysisModel.EnableResponseElevationLimit}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.EnableResponseElevationLimit = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Enable Response Elevation Limit of {entityAnalysisModel.EnableResponseElevationLimit}.");
                            }
                        }

                        if (record.EnableTtlCounter.HasValue)
                        {
                            entityAnalysisModel.EnableTtlCounter = record.EnableTtlCounter == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with Enable Ttl Counter cache of {entityAnalysisModel.EnableTtlCounter}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.EnableTtlCounter = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Enable Ttl Counter cache of {entityAnalysisModel.EnableTtlCounter}.");
                            }
                        }

                        if (record.EnableSanctionCache.HasValue)
                        {
                            entityAnalysisModel.EnableSanctionCache = record.EnableSanctionCache == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with Enable Sanction Cache cache of {entityAnalysisModel.EnableSanctionCache}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.EnableSanctionCache = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Enable Sanction Cache of {entityAnalysisModel.EnableSanctionCache}.");
                            }
                        }

                        if (record.CacheFetchLimit.HasValue)
                        {
                            entityAnalysisModel.CacheTtlLimit = record.CacheFetchLimit.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with Case TTL Limit of {entityAnalysisModel.CacheTtlLimit}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.CacheTtlLimit = 100;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Case TTL Limit of {entityAnalysisModel.CacheTtlLimit}.");
                            }
                        }

                        if (record.MaxResponseElevation.HasValue)
                        {
                            entityAnalysisModel.MaxResponseElevation = record.MaxResponseElevation.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with Max Response Elevation of {entityAnalysisModel.MaxResponseElevation}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.MaxResponseElevation = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} has been set with DEFAULT Max Response Elevation of {entityAnalysisModel.MaxResponseElevation}.");
                            }
                        }

                        if (record.Guid != Guid.Empty)
                        {
                            entityAnalysisModel.Guid = record.Guid;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Entity Analysis Model GUID value {entityAnalysisModel.Guid}.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Entity Analysis Model GUID is empty.");
                            }
                        }

                        if (record.TenantRegistryId.HasValue)
                        {
                            entityAnalysisModel.TenantRegistryId = record.TenantRegistryId.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Tenant Registry ID value {entityAnalysisModel.TenantRegistryId}.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Tenant Registry ID is empty,  which it cannot be.");
                            }
                        }

                        if (record.MaxResponseElevationInterval.HasValue)
                        {
                            entityAnalysisModel.MaxResponseElevationInterval =
                                record.MaxResponseElevationInterval.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Max Response Elevation Frequency Interval value {entityAnalysisModel.MaxResponseElevationInterval}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.MaxResponseElevationInterval = 'n';

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Max Response Elevation Frequency Interval value {entityAnalysisModel.MaxResponseElevationInterval}.");
                            }
                        }

                        if (record.MaxResponseElevationValue.HasValue)
                        {
                            entityAnalysisModel.MaxResponseElevationValue = record.MaxResponseElevationValue.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Max Response Elevation Frequency Value {entityAnalysisModel.MaxResponseElevationValue}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.MaxResponseElevationValue = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Max Response Elevation Frequency Value {entityAnalysisModel.MaxResponseElevationValue}.");
                            }
                        }

                        if (record.CacheTtlInterval.HasValue)
                        {
                            entityAnalysisModel.CacheTtlInterval =
                                record.CacheTtlInterval.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Cache Ttl Interval value {entityAnalysisModel.CacheTtlInterval}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.CacheTtlInterval = 'd';

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Cache Ttl Interval value {entityAnalysisModel.CacheTtlInterval}.");
                            }
                        }

                        if (record.CacheTtlIntervalValue.HasValue)
                        {
                            entityAnalysisModel.CacheTtlIntervalValue = record.CacheTtlIntervalValue.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Cache Ttl Interval Value {entityAnalysisModel.CacheTtlIntervalValue}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.CacheTtlIntervalValue = 3;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Cache Ttl Interval Value {entityAnalysisModel.CacheTtlIntervalValue}.");
                            }
                        }

                        if (record.MaxResponseElevationThreshold.HasValue)
                        {
                            entityAnalysisModel.MaxResponseElevationThreshold =
                                record.MaxResponseElevationThreshold.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Max Response Elevation Frequency Threshold Value {entityAnalysisModel.MaxResponseElevationThreshold}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.MaxResponseElevationThreshold = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Max Response Elevation Frequency Threshold Value {entityAnalysisModel.MaxResponseElevationThreshold}.");
                            }
                        }

                        if (record.MaxActivationWatcherInterval.HasValue)
                        {
                            entityAnalysisModel.MaxActivationWatcherInterval =
                                record.MaxActivationWatcherInterval.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Max Activation Watcher Interval value {entityAnalysisModel.MaxActivationWatcherInterval}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.MaxActivationWatcherInterval = 'n';

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Max Activation Watcher Interval value {entityAnalysisModel.MaxActivationWatcherInterval}.");
                            }
                        }

                        if (record.MaxActivationWatcherValue.HasValue)
                        {
                            entityAnalysisModel.MaxActivationWatcherValue = record.MaxActivationWatcherValue.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Max Activation Watcher value {entityAnalysisModel.MaxActivationWatcherValue}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.MaxActivationWatcherValue = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Max Activation Watcher value {entityAnalysisModel.MaxActivationWatcherValue}.");
                            }
                        }

                        if (record.MaxActivationWatcherThreshold.HasValue)
                        {
                            entityAnalysisModel.MaxActivationWatcherThreshold =
                                record.MaxActivationWatcherThreshold.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Max Activation Watcher Threshold value {entityAnalysisModel.MaxActivationWatcherThreshold}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.MaxActivationWatcherThreshold = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Max Activation Watcher Threshold value {entityAnalysisModel.MaxActivationWatcherThreshold}.");
                            }
                        }

                        if (record.ActivationWatcherSample.HasValue)
                        {
                            entityAnalysisModel.ActivationWatcherSample = record.ActivationWatcherSample.Value;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Activation Watcher Sample value {entityAnalysisModel.ActivationWatcherSample}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.ActivationWatcherSample = 0;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Activation Watcher Sample value {entityAnalysisModel.ActivationWatcherSample}.");
                            }
                        }

                        if (record.EnableActivationArchive.HasValue)
                        {
                            entityAnalysisModel.EnableActivationArchive = record.EnableActivationArchive.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Promote Activation Archive value {entityAnalysisModel.EnableActivationArchive}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.EnableActivationArchive = false;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Promote Activation Archive value {entityAnalysisModel.EnableActivationArchive}.");
                            }
                        }

                        if (record.EnableRdbmsArchive.HasValue)
                        {
                            entityAnalysisModel.EnableRdbmsArchive = record.EnableRdbmsArchive.Value == 1;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with Enable Database value {entityAnalysisModel.EnableRdbmsArchive}.");
                            }
                        }
                        else
                        {
                            entityAnalysisModel.EnableRdbmsArchive = true;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Model {entityAnalysisModel.Id} with DEFAULT Enable Database value {entityAnalysisModel.EnableRdbmsArchive}.");
                            }
                        }

                        listEntityAnalysisModelIdsAdded.Add(entityAnalysisModel.Id);


                        var added = ActiveEntityAnalysisModels.TryAdd(entityAnalysisModel.Id, entityAnalysisModel);

                        Log.Debug(added ? $"Entity Start: Model {entityAnalysisModel.Id} does not exist in the list of active models,  hence it has just been added." : $"Entity Start: Model {entityAnalysisModel.Id} already exists,  hence it has just been updated.");

                        listEntityAnalysisModelIdsAdded.Add(entityAnalysisModel.Id);
                    }
                    else
                    {
                        var removed = ActiveEntityAnalysisModels.Remove(record.Id);
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(removed
                                ? $"Entity Start: Model {record.Id} already exists but is marked as inactive,  hence it has just been removed from the list of active models."
                                : $"Entity Start: Model {record.Id} is marked as inactive but it does not exist in the list in of Active Models.");
                        }
                    }

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: Loaded {ActiveEntityAnalysisModels.Count} active models but need to remove models that have been deleted and are orphaned.");
                    }

                    foreach (var id in ActiveEntityAnalysisModels.Keys.Where(id =>
                                 !listEntityAnalysisModelIdsAdded.Contains(id)))
                    {
                        ActiveEntityAnalysisModels.Remove(id);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug($"Entity Start: Removed orphan {id} from active models.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        $"Entity Start: Model {record.Id} has been returned,  checking to see if it is active has created an error as {ex}.");
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Executed database procedures to get all models.");
            }
        }

        private void HeartbeatThisModel(DbContext dbContext, int tenantRegistryId)
        {
            var repository = new EntityAnalysisModelSyncronisationNodeStatusEntryRepository(dbContext);

            var upsert = new EntityAnalysisModelSynchronisationNodeStatusEntry
            {
                TenantRegistryId = tenantRegistryId,
                Instance = Dns.GetHostName()
            };

            repository.UpsertHeartbeat(upsert);
        }

        private void CompileInlineScripts(string strPathBinary, string strPathFramework, DbContext dbContext)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start: Getting all Inline Scripts from Database.");
            }

            var repository = new EntityAnalysisInlineScriptRepository(dbContext);

            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    "Entity Start: Executing EntityAnalysisInlineScriptRepository.Get.");
            }

            var records = repository.Get();

            foreach (var record in records)
            {
                try
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: Found an inline script with the id of {record.Id} and will proceed to check if already have this inline script available.");
                    }

                    var inlineScript = inlineScripts.Find(x => x.InlineScriptId == record.Id);
                    if (inlineScript == null)
                    {
                        inlineScript = new EntityAnalysisModelInlineScript();

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Have not found an inline script in the available inline scripts, with the id of {record.Id} hence a new one will be created.");
                        }
                    }
                    else
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Found an inline script in the available inline scripts, with the id of {record.Id} and this will be used.");
                        }
                    }

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: Found an inline script {record.Id} has Created Date: {inlineScript.CreatedDate.HasValue} of {inlineScript.CreatedDate}. A check will be made to see if it has changed recently");
                    }

                    if ((!inlineScript.CreatedDate.HasValue ||
                         !(Convert.ToDateTime(record.CreatedDate) > inlineScript.CreatedDate)) &&
                        inlineScript.CreatedDate.HasValue)
                    {
                        continue;
                    }

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has changed recently or is new,  setting created date.");
                    }

                    inlineScript.CreatedDate = record.CreatedDate;
                    inlineScript.InlineScriptId = record.Id;
                    inlineScript.InlineScriptCode = record.Code;
                    
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has rule script of {inlineScript.InlineScriptCode}.");
                    }

                    inlineScript.MethodName = record.MethodName;

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has method specification of {inlineScript.MethodName}.");
                    }

                    inlineScript.ClassName = record.ClassName;

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has method specification of {inlineScript.ClassName}.");
                    }

                    inlineScript.Name = record.Name;

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug($"Entity Start: Inline Script {record.Id} has name of {inlineScript.Name}.");
                    }

                    var dependencyArray = new string[2];
                    dependencyArray[0] = Path.Combine(strPathBinary, "log4net.dll");
                    dependencyArray[1] = Path.Combine(strPathBinary, "Jube.Dictionary.dll");

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: Inline Script {record.Id} is being checked for dll dependencies.");
                    }

                    if (!String.IsNullOrEmpty(record.Dependency))
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has dll dependency specification of {record.Dependency}.");
                        }

                        inlineScript.Dependencies = record.Dependency;

                        foreach (var file in inlineScript.Dependencies.Split(",".ToCharArray()))
                        {
                            Array.Resize(ref dependencyArray, dependencyArray.Length + 1);
                            if (File.Exists(Path.Combine(strPathBinary, file)))
                            {
                                dependencyArray[^1] = Path.Combine(strPathBinary, file);
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Added Inline Script Dependency at binary level {dependencyArray[^1]} for inline script {record.Id}.");
                                }
                            }
                            else
                            {
                                dependencyArray[^1] = Path.Combine(strPathFramework, file);
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Added Inline Script Dependency at framework level {dependencyArray[^1]} for inline script {record.Id}.");
                                }
                            }
                        }
                    }

                    var inlineScriptHash = Hash.GetHash(inlineScript.InlineScriptCode);

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and the hash cache will now be checked.");
                    }

                    if (HashCacheAssembly.TryGetValue(inlineScriptHash, out var value))
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and has been located in the hash cache,  this will be used.  Creating a delegate.");
                        }

                        inlineScript.InlineScriptCompile = value;
                        inlineScript.InlineScriptType =
                            inlineScript.InlineScriptCompile.GetType(inlineScript.ClassName);
                        inlineScript.PreProcessingMethodInfo =
                            inlineScript.InlineScriptType.GetMethod(inlineScript.MethodName,
                                [typeof(DictionaryNoBoxing), typeof(ILog)]);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and has been located in the hash cache and allocated to a delegate.");
                        }
                    }
                    else
                    {
                        bool compiled;

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} but has not been located in the hash cache.  The inline script will now be compiled.");
                        }

                        var compile = new Compile();
                        compile.CompileCode(inlineScript.InlineScriptCode, Log, dependencyArray);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been compiled with {compile.Errors} errors.");
                        }

                        if (compile.Errors == 0)
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} has been compiled with no errors and will now be allocated to a delegate.");
                            }

                            inlineScript.InlineScriptCompile = compile.CompiledAssembly;
                            inlineScript.InlineScriptType =
                                inlineScript.InlineScriptCompile.GetType(inlineScript.ClassName);
                            inlineScript.PreProcessingMethodInfo =
                                inlineScript.InlineScriptType.GetMethod(inlineScript.MethodName,
                                    [typeof(DictionaryNoBoxing), typeof(ILog)]);

                            inlineScripts.Add(inlineScript);
                            HashCacheAssembly.Add(inlineScriptHash, compile.CompiledAssembly);
                            compiled = true;

                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} has been compiled and allocated to a delegate.");
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Could not compile inline script: {inlineScript.InlineScriptCode}.");
                            }

                            compiled = false;
                        }

                        if (!compiled)
                        {
                            continue;
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been compiled will now proceed to inspect the properties exposed by the class.");
                        }

                        var searchKeyAttributesPropertyInfo = inlineScript.InlineScriptCompile.GetTypes()
                            .SelectMany(t => t.GetProperties()).ToArray();
                        foreach (var propertyInfoWithinLoop in searchKeyAttributesPropertyInfo)
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and looking for custom attributes.");
                            }

                            foreach (var customAttributeDataWithinLoop in propertyInfoWithinLoop
                                         .CustomAttributes)
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and is inspecting custom attribute {customAttributeDataWithinLoop.AttributeType.Name}.");
                                }

                                switch (customAttributeDataWithinLoop.AttributeType.Name)
                                {
                                    case "SearchKey":
                                    {
                                        if (Log.IsDebugEnabled)
                                        {
                                            Log.Debug(
                                                $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key.");
                                        }

                                        var groupingKey = new DistinctSearchKey
                                        {
                                            SearchKey = propertyInfoWithinLoop.Name
                                        };

                                        foreach (var customAttributeNamedArgument in
                                                 customAttributeDataWithinLoop
                                                     .NamedArguments)
                                        {
                                            var customAttributeTypedArgument =
                                                customAttributeNamedArgument.TypedValue;
                                            switch (customAttributeNamedArgument.MemberName)
                                            {
                                                case "CacheKey":
                                                    if (Log.IsDebugEnabled)
                                                    {
                                                        Log.Debug(
                                                            $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key, CacheKey with value of {customAttributeTypedArgument.Value}.");
                                                    }

                                                    groupingKey.SearchKeyCache =
                                                        Convert.ToBoolean(customAttributeTypedArgument.Value);
                                                    break;
                                                case "CacheKeyIntervalType":
                                                    if (Log.IsDebugEnabled)
                                                    {
                                                        Log.Debug(
                                                            $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key, CacheKeyIntervalType with value of {customAttributeTypedArgument.Value}.");
                                                    }

                                                    groupingKey.SearchKeyCacheIntervalType =
                                                        customAttributeTypedArgument.Value.ToString();
                                                    break;
                                                case "CacheKeyIntervalValue":
                                                    if (Log.IsDebugEnabled)
                                                    {
                                                        Log.Debug(
                                                            $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key, CacheKeyIntervalValue with value of {customAttributeTypedArgument.Value}.");
                                                    }

                                                    groupingKey.SearchKeyCacheIntervalValue =
                                                        Convert.ToInt32(customAttributeTypedArgument.Value);
                                                    break;
                                            }
                                        }

                                        inlineScript.GroupingKeys.Add(groupingKey);

                                        if (Log.IsDebugEnabled)
                                        {
                                            Log.Debug(
                                                $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key and is adding the grouping key to the model.");
                                        }

                                        break;
                                    }
                                    case "ReportTable":
                                    {
                                        var columnName = propertyInfoWithinLoop.Name;
                                        int columnType;

                                        if (propertyInfoWithinLoop.PropertyType == typeof(string))
                                        {
                                            columnType = 1;
                                        }

                                        else if (propertyInfoWithinLoop.PropertyType == typeof(int))
                                        {
                                            columnType = 2;
                                        }

                                        else if (propertyInfoWithinLoop.PropertyType == typeof(double))
                                        {
                                            columnType = 3;
                                        }

                                        else if (propertyInfoWithinLoop.PropertyType ==
                                                 typeof(DateTime))
                                        {
                                            columnType = 4;
                                        }

                                        else if (propertyInfoWithinLoop.PropertyType == typeof(bool))
                                        {
                                            columnType = 5;
                                        }

                                        else
                                        {
                                            columnType = 1;
                                        }

                                        if (!inlineScript.PromoteReportTableColumns.ContainsKey(
                                                "ColumnName"))
                                        {
                                            inlineScript.PromoteReportTableColumns.Add(columnName,
                                                columnType);
                                        }

                                        break;
                                    }
                                }
                            }
                        }

                        inlineScript.ActivatedObject =
                            Activator.CreateInstance(inlineScript.InlineScriptType, Log);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been created and the method referenced.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        $"Entity Start: Inline script with the id of {record.Id} has created an error {ex}.");
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Entity Start:  Completed creating Inline Scripts and closed the reader.");
            }
        }

        private List<GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery.Dto> GetTenantRegistrySchedule(
            DbContext dbContext)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    "Entity Start: Executing a fetch of all tenant schedules for the entity sub system using GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery.");
            }

            var query = new GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery(dbContext);
            var values = query.Execute(Dns.GetHostName()).ToList();

            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    "Entity Start: Executed GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery.");
            }

            return values;
        }

        private DateTime AddSearchKeyCacheServerTime(DateTime currentDate)
        {
            var value = JubeEnvironment.AppSettings("SearchKeyCacheServerIntervalType") switch
            {
                "n" => currentDate.AddMinutes(
                    Int32.Parse(JubeEnvironment.AppSettings("SearchKeyCacheServerIntervalValue"))),
                "h" => currentDate.AddHours(
                    Int32.Parse(JubeEnvironment.AppSettings("SearchKeyCacheServerIntervalValue"))),
                "d" => currentDate.AddDays(Int32.Parse(JubeEnvironment.AppSettings("SearchKeyCacheServerIntervalValue"))),
                "m" => currentDate.AddMonths(
                    Int32.Parse(JubeEnvironment.AppSettings("SearchKeyCacheServerIntervalValue"))),
                _ => currentDate.AddHours(1)
            };

            return value;
        }

        private async Task AbstractionRuleCachingAsync()
        {
            while (!stopping)
            {
                try
                {
                    foreach (var (key, value) in
                             from modelEntityKvp in ActiveEntityAnalysisModels
                             where modelEntityKvp.Value.Started
                             where AddSearchKeyCacheServerTime(modelEntityKvp.Value.LastModelSearchKeyCacheWritten) <
                                   DateTime.Now
                             select modelEntityKvp)
                    {
                        if (!value.HasCheckedDatabaseForLastSearchKeyCacheDates)
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Abstraction Rule Caching: The startup routine has not run for model {key} and the last dates will be fetched from the database.");
                            }

                            var dbContext =
                                DataConnectionDbContext.GetDbContextDataConnection(
                                    JubeEnvironment.AppSettings("ConnectionString"));
                            try
                            {
                                var query =
                                    new GetEntityAnalysisModelsSearchKeyCalculationInstancesLastSearchKeyDates(
                                        dbContext);

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Abstraction Rule Caching: Is about to lookup the last date the search keys were run for model {key}.");
                                }

                                var records = query.Execute(value.Guid);

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Abstraction Rule Caching: Has executed {key} to look up search keys last executed date.");
                                }

                                foreach (var record in records)
                                {
                                    if (record.SearchKey != null)
                                    {
                                        if (record.DistinctFetchToDate.HasValue)
                                        {
                                            value.LastAbstractionRuleCache.Add(
                                                record.SearchKey ?? String.Empty, record.DistinctFetchToDate.Value);

                                            if (Log.IsDebugEnabled)
                                            {
                                                Log.Debug(
                                                    $"Entity Abstraction Rule Caching: Search Key last date for {record.SearchKey} has been added with a Distinct_Fetch_To_Date of {record.DistinctFetchToDate.Value}.");
                                            }
                                        }
                                        else
                                        {
                                            if (Log.IsDebugEnabled)
                                            {
                                                Log.Debug(
                                                    $"Entity Abstraction Rule Caching: Search Key last date for {record.SearchKey} is null and being stepped over during last distinct fetch dates.");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (Log.IsDebugEnabled)
                                        {
                                            Log.Debug(
                                                "Entity Abstraction Rule Caching: Search Key grouped null and is being stepped over during last distinct fetch dates.");
                                        }
                                    }
                                }

                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Abstraction Rule Caching: Has finished searching for last date the search keys were run for model {key}.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(
                                    $"Entity Abstraction Rule Caching: Error while fetching the last search key cache dates as {ex}.");
                            }
                            finally
                            {
                                value.HasCheckedDatabaseForLastSearchKeyCacheDates = true;

                                await dbContext.CloseAsync().ConfigureAwait(false);
                                await dbContext.DisposeAsync().ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            if (Log.IsDebugEnabled)
                            {
                                Log.Debug(
                                    $"Entity Abstraction Rule Caching: The startup of has run before for model {key} and the database is not being checked for the last date on search key cache.");
                            }
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Abstraction Rule Caching: Entity Model {key} is being started.");
                        }

                        await value.AbstractionRuleCachingAsync().ConfigureAwait(false);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug($"Entity Abstraction Rule Caching: Entity Model {key} has finished.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                finally
                {
                    Thread.Sleep(10000);
                }
            }
        }

        private async Task EntityReprocessingAsync()
        {
            try
            {
                var lastUpdated = default(DateTime);

                while (!stopping)
                {
                    var dbContext =
                        DataConnectionDbContext.GetDbContextDataConnection(
                            JubeEnvironment.AppSettings("ConnectionString"));
                    try
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug("Entity Reprocessing:  About to make a database connection.");
                        }

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                "Entity Reprocessing:  Has made a database connection.  Will now proceed to loop around the models and see if there are any reprocessing requests.");
                        }

                        foreach (var modelKvp in ActiveEntityAnalysisModels)
                        {
                            try
                            {
                                if (Log.IsDebugEnabled)
                                {
                                    Log.Debug(
                                        $"Entity Reprocessing:  Has found model id {modelKvp.Key}.  Will now check to see if the model has been started.");
                                }

                                if (!modelKvp.Value.Started)
                                {
                                    continue;
                                }

                                GetEntityAnalysisModelRuleReprocessingInstance(dbContext, modelKvp,
                                    out var entityAnalysisModelRuleReprocessingInstance, out var foundInstance);

                                if (!foundInstance)
                                {
                                    continue;
                                }

                                if (Log.IsInfoEnabled)
                                {
                                    Log.Info(
                                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} using created date.");
                                }

                                var documentsInitialCounts =
                                    await GetInitialCountsAsync(dbContext, modelKvp.Value.Guid).ConfigureAwait(false);

                                var dateRangeAndCount = EstablishProcessingDateRange(
                                    entityAnalysisModelRuleReprocessingInstance,
                                    documentsInitialCounts);

                                 UpdateEntityAnalysisModelsReprocessingRuleInstanceReferenceDateCount(dbContext,
                                    entityAnalysisModelRuleReprocessingInstance, modelKvp.Value.Guid ,dateRangeAndCount.adjustedStartDate);

                                var limit = Int32.Parse(JubeEnvironment.AppSettings("ReprocessingBulkLimit"));

                                if (Log.IsInfoEnabled)
                                {
                                    Log.Info(
                                        $"Entity Reprocessing:  Is about to build up the cache filter for instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} reprocessing bulk limit has been set to {limit}.");
                                }

                                if (Log.IsInfoEnabled)
                                {
                                    Log.Info(
                                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has created a filter between {dateRangeAndCount.adjustedStartDate} and {dateRangeAndCount.lastReferenceDate}.");
                                }

                                var sampled = 0;
                                var matched = 0;
                                var processed = 0;
                                var errors = 0;
                                // ReSharper disable once RedundantAssignment
                                var deleted = false;

                                var archiveDatabase =
                                    new Postgres(
                                        JubeEnvironment.AppSettings("ConnectionString"));
                                do
                                {
                                    if (Log.IsInfoEnabled)
                                    {
                                        Log.Info(
                                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is about to run a query on cache to bring back all document for filter,  skipping {processed} and limiting {limit}.");
                                    }

                                    var documents =
                                        await archiveDatabase.ExecuteReturnPayloadFromArchiveWithSkipLimitAsync(
                                            modelKvp.Value.ArchivePayloadSql, dateRangeAndCount.adjustedStartDate,
                                            processed,
                                            limit).ConfigureAwait(false);

                                    if (documents.Count == 0)
                                    {
                                        break;
                                    }

                                    foreach (var entry in documents)
                                    {
                                        try
                                        {
                                            if (Log.IsInfoEnabled)
                                            {
                                                Log.Info(
                                                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is about to process document {processed}.");
                                            }

                                            if (entityAnalysisModelRuleReprocessingInstance
                                                    .ReprocessingSample >= Seeded.NextDouble())
                                            {
                                                if (Log.IsInfoEnabled)
                                                {
                                                    Log.Info(
                                                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} and it has passed a random sample.  Will now test the rule.");
                                                }

                                                sampled += 1;

                                                var entityInstanceEntryDictionaryKvPs =
                                                    new PooledDictionary<string, DictionaryNoBoxing>();

                                                if (entityAnalysisModelRuleReprocessingInstance
                                                    .ReprocessingRuleCompileDelegate(entry,
                                                        modelKvp.Value.EntityAnalysisModelLists,
                                                        entityInstanceEntryDictionaryKvPs, Log))
                                                {
                                                    dateRangeAndCount.lastReferenceDate =
                                                        entry[modelKvp.Value.ReferenceDateName];

                                                    await InvokeReprocessingForDocument(modelKvp,
                                                        entityAnalysisModelRuleReprocessingInstance, processed,
                                                        entry).ConfigureAwait(false);

                                                    matched += 1;
                                                }
                                                else
                                                {
                                                    if (Log.IsInfoEnabled)
                                                    {
                                                        Log.Info(
                                                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} but it has not passed the rule.");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (Log.IsInfoEnabled)
                                                {
                                                    Log.Info(
                                                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} but it has failed to obtain a random digit and as been sampled out.");
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            errors += 1;

                                            if (Log.IsInfoEnabled)
                                            {
                                                Log.Info(
                                                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} and has had an error {ex}.");
                                            }
                                        }
                                        finally
                                        {
                                            if (Log.IsInfoEnabled)
                                            {
                                                Log.Info(
                                                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has finished processing {processed}.");
                                            }

                                            processed += 1;
                                        }

                                        if (lastUpdated <= DateTime.Now.AddSeconds(-10))
                                        {
                                            if (LogAndGetTerminate(dbContext,
                                                    entityAnalysisModelRuleReprocessingInstance,
                                                    processed, sampled, matched, errors,
                                                    dateRangeAndCount.lastReferenceDate))
                                            {
                                                if (Log.IsInfoEnabled)
                                                {
                                                    Log.Info(
                                                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been removed, stopping process.");
                                                }

                                                // ReSharper disable once RedundantAssignment
                                                deleted = true;

                                                break;
                                            }

                                            lastUpdated = DateTime.Now;
                                        }
                                        else
                                        {
                                            if (Log.IsInfoEnabled)
                                            {
                                                Log.Info(
                                                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} not updated database as time interval not passed.");
                                            }
                                        }
                                    }

                                    deleted = LogAndGetTerminate(dbContext,
                                        entityAnalysisModelRuleReprocessingInstance,
                                        processed, sampled, matched, errors, dateRangeAndCount.lastReferenceDate);
                                } while (!deleted);

                                FinishReprocessBatchChunk(dbContext,
                                    entityAnalysisModelRuleReprocessingInstance);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Entity Reprocessing: {ex}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Entity Reprocessing: {ex}");
                    }
                    finally
                    {
                        if (Log.IsInfoEnabled)
                        {
                            Log.Info(
                                "Entity Reprocessing: Has finished a cycle and will now sleep for 20 seconds,  the database connection to Database will also be closed.");
                        }

                        await dbContext.CloseAsync().ConfigureAwait(false);
                        await dbContext.DisposeAsync().ConfigureAwait(false);

                        Thread.Sleep(20000);

                        if (Log.IsInfoEnabled)
                        {
                            Log.Info("Entity Reprocessing: Awake again.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Entity Reprocessing: Error outside of loop {ex}");
            }
        }

        private bool LogAndGetTerminate(DbContext dbContext,
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance, int processed,
            int sampled, int matched, int errors, DateTime referenceDate)
        {
            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is about to report back status to the database.");
            }

            var deleted = false;
            try
            {
                var repository = new EntityAnalysisModelReprocessingRuleInstanceRepository(dbContext);

                repository.UpdateCounts(
                    entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId,
                    sampled, matched, processed, errors, referenceDate);
            }
            catch
            {
                deleted = true;
            }

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has updated database with processed {processed}, Sampled {sampled}, Matched {matched}, Errors{errors}.");
            }

            return deleted;
        }

        private async Task InvokeReprocessingForDocument(KeyValuePair<int, EntityAnalysisModel> modelKvp,
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance, int processed,
            DictionaryNoBoxing entry)
        {
            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} and matched the rule and will now invoke.");
            }

            var stopwatch = new Stopwatch();

            ExtractModelFieldsForInvocation(entityAnalysisModelRuleReprocessingInstance, entry,
                out var entityInstanceEntryPayloadStore, modelKvp, out var cachePayloadDocumentStore,
                out var entityModelInvoke, out var cachePayloadDocumentResponse);

            ExtractRequestXPathForInvocation(entityAnalysisModelRuleReprocessingInstance, entry, modelKvp,
                cachePayloadDocumentStore);

            await FinaliseAndInvokeForReprocess(entityAnalysisModelRuleReprocessingInstance,
                entityInstanceEntryPayloadStore,
                modelKvp, cachePayloadDocumentStore, entityModelInvoke,
                cachePayloadDocumentResponse, stopwatch).ConfigureAwait(false);
        }

        private async Task FinaliseAndInvokeForReprocess(
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance,
            EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayloadStore,
            KeyValuePair<int, EntityAnalysisModel> modelKvp,
            DictionaryNoBoxing cachePayloadDocumentStore,
            EntityAnalysisModelInvoke entityAnalysisModelInvoke,
            DictionaryNoBoxing cachePayloadDocumentResponse,
            Stopwatch stopwatch)
        {
            var (_, value) = modelKvp;
            entityAnalysisModelInvoke.CachePayloadDocumentStore = cachePayloadDocumentStore;
            entityAnalysisModelInvoke.CachePayloadDocumentResponse = cachePayloadDocumentResponse;
            entityAnalysisModelInvoke.EntityAnalysisModel = value;
            entityAnalysisModelInvoke.Stopwatch = stopwatch;
            entityAnalysisModelInvoke.EntityAnalysisModelInstanceEntryPayloadStore =
                entityAnalysisModelInstanceEntryPayloadStore;
            entityAnalysisModelInvoke.Reprocess = true;
            await entityAnalysisModelInvoke.Start().ConfigureAwait(false);

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has completed the invoke.");
            }
        }

        private void ExtractRequestXPathForInvocation(
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance,
            DictionaryNoBoxing entry,
            KeyValuePair<int, EntityAnalysisModel> modelKvp, DictionaryNoBoxing cachePayloadDocumentStore)
        {
            foreach (var xPath in
                     from xPathLinq in modelKvp.Value.EntityAnalysisModelRequestXPaths
                     where !cachePayloadDocumentStore.ContainsKey(xPathLinq.Name)
                     select xPathLinq)
            {
                if (!entry.ContainsKey(xPath.Name))
                {
                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has created invoke instance.  XPath {xPath.Name} was not in the original payload.");
                    }
                }
                else
                {
                    if (xPath.DataTypeId == 1)
                    {
                        cachePayloadDocumentStore.TryAdd(xPath.Name, entry[xPath.Name].AsString());
                    }
                    else if (xPath.DataTypeId == 2)
                    {
                        cachePayloadDocumentStore.TryAdd(xPath.Name, entry[xPath.Name].AsInt());
                    }
                    else if (xPath.DataTypeId == 3)
                    {
                        cachePayloadDocumentStore.TryAdd(xPath.Name, entry[xPath.Name].AsDouble());
                    }
                    else if (xPath.DataTypeId == 4)
                    {
                        cachePayloadDocumentStore.TryAdd(xPath.Name, entry[xPath.Name].AsDateTime());
                    }
                    else if (xPath.DataTypeId == 5)
                    {
                        cachePayloadDocumentStore.TryAdd(xPath.Name, entry[xPath.Name].AsBool());
                    }
                    else if (xPath.DataTypeId == 6 || xPath.DataTypeId == 7)
                    {
                        cachePayloadDocumentStore.TryAdd(xPath.Name, entry[xPath.Name].AsDouble());
                    }

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has created invoke instance.  Added {xPath.Name} with value {entry[xPath.Name]} as a report column.");
                    }
                }
            }

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has added all request XPath fields,  will now start the invoke.");
            }
        }

        private void ExtractModelFieldsForInvocation(
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance,
            DictionaryNoBoxing entry,
            out EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayloadStore,
            KeyValuePair<int, EntityAnalysisModel> modelKvp,
            out DictionaryNoBoxing cachePayloadDocumentStore,
            out EntityAnalysisModelInvoke entityAnalysisModelInvoke,
            out DictionaryNoBoxing cachePayloadDocumentResponse)
        {
            entityAnalysisModelInstanceEntryPayloadStore = new EntityAnalysisModelInstanceEntryPayload
            {
                EntityAnalysisModelInstanceEntryGuid = entry["EntityAnalysisModelInstanceEntryGuid"]
            };

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is creating invoke instance. EntityAnalysisModelInstanceEntryGUID is {entityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid}.");
            }

            entityAnalysisModelInvoke = new EntityAnalysisModelInvoke(Log, JubeEnvironment, RabbitMqChannel,
                CacheService,
                PendingNotification, Seeded,
                ActiveEntityAnalysisModels);
            cachePayloadDocumentStore = new DictionaryNoBoxing();
            cachePayloadDocumentResponse = new DictionaryNoBoxing();

            var (_, value) = modelKvp;
            var modelEntryValue = entry[value.EntryName].ToString();

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is creating invoke instance. ModelEntryValue is {modelEntryValue}.");
            }

            var referenceDateValue = entry[value.ReferenceDateName];

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is creating invoke instance. ReferenceDateValue is {referenceDateValue}.");
            }

            entityAnalysisModelInstanceEntryPayloadStore.EntityInstanceEntryId = modelEntryValue;
            entityAnalysisModelInstanceEntryPayloadStore.ReferenceDate = referenceDateValue;
            entityAnalysisModelInstanceEntryPayloadStore.Payload = entry;

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has created invoke instance.  Will now add the XPath values by looping through the XPath values configured for this model.");
            }
        }

        private void FinishReprocessBatchChunk(DbContext dbContext,
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance)
        {
            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} no documents returned so the work is done.  Is about to update the database to show that the process has completed.");
            }

            var repository = new EntityAnalysisModelReprocessingRuleInstanceRepository(dbContext);

            repository.UpdateCompleted(entityAnalysisModelRuleReprocessingInstance
                .EntityAnalysisModelsReprocessingRuleInstanceId);

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} processing completed and database updated.");
            }
        }

        private void UpdateEntityAnalysisModelsReprocessingRuleInstanceReferenceDateCount(DbContext dbContext,
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance,
            Guid entityAnalysisModelGuid,DateTime lastReferenceDate)
        {
            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is about to make initial counts for monitoring as Reference_Date {lastReferenceDate}.");
            }

            var archiveRepository = new ArchiveRepository(dbContext);
            var allCount = archiveRepository.GetCountsByReferenceDate(entityAnalysisModelGuid,lastReferenceDate);
            
            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has returned initial counts of {allCount} for monitoring as Reference_Date {lastReferenceDate} and Available_Count {allCount}.");
            }
            
            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is about to update initial counts for monitoring as Reference_Date {lastReferenceDate} and Available_Count {allCount}.");
            }

            var repository = new EntityAnalysisModelReprocessingRuleInstanceRepository(dbContext);

            repository.UpdateReferenceDateCount(
                entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId,
                allCount, lastReferenceDate);

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has updated initial counts for monitoring as Reference Date {lastReferenceDate} and Available Count {allCount}.");
            }
        }

        private (DateTime lastReferenceDate, long allCount, DateTime adjustedStartDate) EstablishProcessingDateRange(
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance,
            GetArchiveRangeAndCountsQuery.Dto ranges)
        {
            var lastReferenceDate = Convert.ToDateTime(ranges.Max);

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Has found a last reference date of {lastReferenceDate}.");
            }

            var firstReferenceDate = Convert.ToDateTime(ranges.Min);

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Has found a first reference date of {firstReferenceDate}.");
            }

            var allCount = ranges.Count;

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Has found counts of  {allCount}.  Will now proceed to adjust the date to create a between range.");
            }

            DateTime adjustedStartDate = default;
            switch (entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType)
            {
                case "d":
                    adjustedStartDate =
                        lastReferenceDate.AddDays(entityAnalysisModelRuleReprocessingInstance
                            .ReprocessingIntervalValue * -1);

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched d as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");
                    }

                    break;
                case "h":
                    adjustedStartDate =
                        lastReferenceDate.AddHours(entityAnalysisModelRuleReprocessingInstance
                            .ReprocessingIntervalValue * -1);

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched h as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");
                    }

                    break;
                case "n":
                    adjustedStartDate = lastReferenceDate.AddMinutes(entityAnalysisModelRuleReprocessingInstance
                        .ReprocessingIntervalValue * -1);

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched n as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");
                    }

                    break;
                case "s":
                    adjustedStartDate = lastReferenceDate.AddSeconds(entityAnalysisModelRuleReprocessingInstance
                        .ReprocessingIntervalValue * -1);

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched s as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");
                    }

                    break;
                case "m":
                    adjustedStartDate =
                        lastReferenceDate.AddMonths(entityAnalysisModelRuleReprocessingInstance
                            .ReprocessingIntervalValue * -1);

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched m as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");
                    }

                    break;
                case "y":
                    adjustedStartDate =
                        lastReferenceDate.AddYears(entityAnalysisModelRuleReprocessingInstance
                            .ReprocessingIntervalValue * -1);

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info(
                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched y as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");
                    }

                    break;
            }

            if (Log.IsInfoEnabled)
            {
                Log.Info(
                    $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Lower Date for range is {adjustedStartDate}.  Finished getting initial counts.");
            }

            return (lastReferenceDate, allCount, adjustedStartDate);
        }

        private async Task<GetArchiveRangeAndCountsQuery.Dto> GetInitialCountsAsync(DbContext dbContext,
            Guid entityAnalysisModelGuid)
        {
            var getArchiveRangeAndCountsQuery = new GetArchiveRangeAndCountsQuery(dbContext);
            return await getArchiveRangeAndCountsQuery.Execute(entityAnalysisModelGuid).ConfigureAwait(false);
        }

        private void GetEntityAnalysisModelRuleReprocessingInstance(DbContext dbContext,
            KeyValuePair<int, EntityAnalysisModel> modelKvp,
            out EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance,
            out bool foundInstance)
        {
            var (key, _) = modelKvp;

            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    $"Entity Reprocessing:  Has found model id {key}.  The model has been started,  so we will check to see if there is a reprocessing instance for the model.");
            }

            var query = new GetNextEntityAnalysisModelsReprocessingRuleInstanceQuery(dbContext);

            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    $"Entity Reprocessing:  Has found model id {key}.  Has executed the reader to find reprocessing instances.");
            }

            var record = query.Execute(key);

            entityAnalysisModelRuleReprocessingInstance = new EntityAnalysisModelRuleReprocessingInstance();
            foundInstance = false;

            if (record != null)
            {
                entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelId = record.EntityAnalysisModelId;

                entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId
                    = record.Id;

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  Has found a reprocessing instance id of {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId}.");
                }

                if (record.ReprocessingIntervalValue.HasValue)
                {
                    entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue =
                        record.ReprocessingIntervalValue.Value;

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalValue to {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.");
                    }
                }
                else
                {
                    entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue = 1;

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalValue to DEFAULT {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.");
                    }
                }

                if (record.ReprocessingIntervalType != null)
                {
                    entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType =
                        record.ReprocessingIntervalType;

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalType to {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}.");
                    }
                }
                else
                {
                    entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType = "d";

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalType to DEFAULT {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}.");
                    }
                }

                if (record.ReprocessingSample.HasValue)
                {
                    entityAnalysisModelRuleReprocessingInstance.ReprocessingSample = record.ReprocessingSample.Value;

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingSample to {entityAnalysisModelRuleReprocessingInstance.ReprocessingSample}.");
                    }
                }
                else
                {
                    entityAnalysisModelRuleReprocessingInstance.ReprocessingSample = 0;

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingSample to DEFAULT {entityAnalysisModelRuleReprocessingInstance.ReprocessingSample}.");
                    }
                }

                if (record.RuleScriptTypeId.HasValue)
                {
                    entityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId = record.RuleScriptTypeId.Value;

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Reprocessing:  Has found model id {key}.  Has set RuleScriptTypeID to {entityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId}.");
                    }
                }
                else
                {
                    entityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId = 1;

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Reprocessing:  Has found model id {key}.  Has set RuleScriptTypeID to DEFAULT {entityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId}.");
                    }
                }

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  Is loading the rule parser and tokens.");
                }

                var parser = ConfigureTokenParserForSecurity(dbContext);

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  Has loaded the rule parser and tokens.");
                }

                if (record.BuilderRuleScript != null &&
                    entityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId == 1)
                {
                    var parsedRule = new ParsedRule
                    {
                        OriginalRuleText = record.BuilderRuleScript,
                        ErrorSpans = []
                    };
                    parsedRule = parser.TranslateFromDotNotation(parsedRule);
                    parsedRule = parser.Parse(parsedRule);

                    if (parsedRule.ErrorSpans.Count == 0)
                    {
                        entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript = parsedRule.ParsedRuleText;

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Reprocessing: {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} set builder script as {entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript}.");
                        }
                    }
                }
                else if (record.CoderRuleScript != null &&
                         entityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId == 2)
                {
                    var parsedRule = new ParsedRule
                    {
                        OriginalRuleText = record.CoderRuleScript,
                        ErrorSpans = []
                    };
                    parsedRule = parser.TranslateFromDotNotation(parsedRule);
                    parsedRule = parser.Parse(parsedRule);

                    if (parsedRule.ErrorSpans.Count == 0)
                    {
                        entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript = parsedRule.ParsedRuleText;

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Reprocessing: {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} set coder script as {entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript}.");
                        }
                    }
                }

                var gatewayRuleScript = new StringBuilder();
                gatewayRuleScript.Append("Imports System.IO\r\n");
                gatewayRuleScript.Append("Imports log4net\r\n");
                gatewayRuleScript.Append("Imports System.Net\r\n");
                gatewayRuleScript.Append("Imports System.Collections.Generic\r\n");
                gatewayRuleScript.Append("Imports Jube.Dictionary\r\n");
                gatewayRuleScript.Append("Imports System\r\n");
                gatewayRuleScript.Append("Public Class GatewayRule\r\n");
                gatewayRuleScript.Append(
                    "Public Shared Function Match(Data As DictionaryNoBoxing, List As Dictionary(Of String, List(Of String)),KVP As PooledDictionary(Of String, DictionaryNoBoxing),Log As ILog) As Boolean\r\n");
                gatewayRuleScript.Append("Dim Matched As Boolean\r\n");
                gatewayRuleScript.Append("Try\r\n");
                gatewayRuleScript.Append(entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript + "\r\n");
                gatewayRuleScript.Append("Catch ex As Exception\r\n");
                gatewayRuleScript.Append("Log.Info(ex.ToString)\r\n");
                gatewayRuleScript.Append("End Try\r\n");
                gatewayRuleScript.Append("Return Matched\r\n");
                gatewayRuleScript.Append("\r\n");
                gatewayRuleScript.Append("End Function\r\n");
                gatewayRuleScript.Append("End Class\r\n");

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} set class wrap as {gatewayRuleScript}.");
                }

                var gatewayRuleScriptHash = Hash.GetHash(gatewayRuleScript.ToString());

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} and will be checked against the hash cache.");
                }

                if (HashCacheAssembly.TryGetValue(gatewayRuleScriptHash, out var value))
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} exists in the hash cache and will be allocated to a delegate.");
                    }

                    entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile =
                        value;
                    var classType =
                        entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile.GetType("GatewayRule");

                    var methodInfo = classType?.GetMethod("Match");
                    if (methodInfo != null)
                    {
                        entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompileDelegate =
                            (EntityAnalysisModelRuleReprocessingInstance.Match)Delegate.CreateDelegate(
                                typeof(EntityAnalysisModelRuleReprocessingInstance.Match), methodInfo);
                    }

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} exists in the hash cache, has been allocated a to a delegate and placed in a shadow list of gateway rules.");
                    }

                    foundInstance = true;
                }
                else
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has not been found in the hash cache and will now be compiled.");
                    }

                    var codeBase = Assembly.GetExecutingAssembly().Location;

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug($"Entity Model Sync: The code base path has been returned as {codeBase}.");
                    }

                    var strPathBinary = Path.GetDirectoryName(codeBase);

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug($"Entity Model Sync: The code base path has been returned as {codeBase}.");
                    }

                    var compile = new Compile();
                    compile.CompileCode(gatewayRuleScript.ToString(), Log,
                    [
                        Path.Combine(strPathBinary ?? throw new InvalidOperationException(), "log4net.dll"),
                        Path.Combine(strPathBinary, "Jube.Dictionary.dll")
                    ]);

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Entity Start: Model {key} and Gateway Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has now been compiled with {compile.Errors} errors.");
                    }

                    if (compile.Errors == 0)
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has now been compiled without error,  a delegate will now be allocated.");
                        }

                        entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile = compile.CompiledAssembly;

                        var classType =
                            entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile.GetType("GatewayRule");
                        var methodInfo = classType?.GetMethod("Match");
                        if (methodInfo != null)
                        {
                            entityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompileDelegate =
                                (EntityAnalysisModelRuleReprocessingInstance.Match)Delegate.CreateDelegate(
                                    typeof(EntityAnalysisModelRuleReprocessingInstance.Match), methodInfo);
                        }

                        HashCacheAssembly.Add(gatewayRuleScriptHash, compile.CompiledAssembly);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has now been compiled without error,  a delegate has been allocated,  added to hash cache and added to a shadow list of gateway rules.");
                        }

                        foundInstance = true;
                    }
                    else
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} failed to load.");
                        }
                    }
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    $"Entity Reprocessing:  Has finished loading reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId}.  Will now proceed to select the counts and date ranges.");
            }
        }

        private int CalculateDictionaryNoBoxingInitialSize(EntityAnalysisModel entityAnalysisModel)
        {
            return entityAnalysisModel.EntityAnalysisModelRequestXPaths.Count +
                   entityAnalysisModel.EntityAnalysisModelAbstractionCalculations.Count +
                   entityAnalysisModel.EntityAnalysisModelInlineFunctions.Count;
        }

        private void PersistToActivationWatcher()
        {
            var activationWatchers = new List<ActivationWatcher>();
            while (!stopping)
            {
                try
                {
                    PersistToActivationWatcherAsync.TryDequeue(out var payload);

                    if (payload != null)
                    {
                        if (Log.IsInfoEnabled)
                        {
                            Log.Info(
                                "Database Activation Watcher Persist: a message has been received to be persisted to the Database database Activation Watcher.");
                        }

                        try
                        {
                            activationWatchers.Add(payload);

                            if (Log.IsInfoEnabled)
                            {
                                Log.Info(
                                    $"Database Persist: Added record to the data table pending SQL Bulk insert Tenant_Registry_ID {payload.TenantRegistryId},Symbol_Entity_Key {payload.Key},Longitude {payload.Longitude},Latitude {payload.Latitude}, Activation_Rule_Summary {payload.ActivationRuleSummary}, Response_Elevation_Content {payload.ResponseElevationContent}, Response_Elevation {payload.ResponseElevation}, Back_Color {payload.BackColor}, Fore_Color {payload.ForeColor}.");
                            }

                            if (Log.IsInfoEnabled)
                            {
                                Log.Info(
                                    $"Database Activation Watcher Persist: The table count threshold has been set to {activationWatchers.Count} and the bulk copy threshold is {JubeEnvironment.AppSettings("ActivationWatcherBulkCopyThreshold")}.");
                            }

                            if (activationWatchers.Count <
                                Int32.Parse(JubeEnvironment.AppSettings("ActivationWatcherBulkCopyThreshold")))
                            {
                                continue;
                            }

                            var sw = new Stopwatch();
                            sw.Start();

                            if (Log.IsInfoEnabled)
                            {
                                Log.Info(
                                    "Database Activation Watcher Persist: The bulk copy threshold has been exceeded and the SQL Bulk Copy will be executed. A timer has been started.");
                            }

                            var dbContext =
                                DataConnectionDbContext.GetDbContextDataConnection(
                                    JubeEnvironment.AppSettings("ConnectionString"));

                            if (Log.IsInfoEnabled)
                            {
                                Log.Info("Database Activation Watcher Persist: Opened an SQL Bulk Collection.");
                            }

                            var repository = new ActivationWatcherRepository(dbContext);

                            if (Log.IsInfoEnabled)
                            {
                                Log.Info("Database Activation Watcher Persist: Will proceed to bulk copy.");
                            }

                            try
                            {
                                repository.BulkCopy(activationWatchers);

                                if (Log.IsInfoEnabled)
                                {
                                    Log.Info(
                                        $"Database Activation Watcher Persist: The bulk copy has inserted {activationWatchers.Count} Activation Watcher records and cleared the data table.  The time taken is {sw.ElapsedMilliseconds} in ms.");
                                }

                                sw.Reset();
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.ToString());

                                sw.Reset();
                            }
                            finally
                            {
                                activationWatchers.Clear();

                                dbContext.Close();
                                dbContext.Dispose();

                                if (Log.IsInfoEnabled)
                                {
                                    Log.Info("Database Activation Watcher Persist: Closed an SQL Bulk Collection.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Database Activation Watcher Persist: An error has occurred as {ex}");
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Database Activation Watcher Persist: An error has occurred as {ex}");
                }
            }
        }

        private async Task TtlCounterAdministrationAsync()
        {
            while (!stopping)
            {
                try
                {
                    var activeModelsForLoopWithoutEnumError = ActiveEntityAnalysisModels.ToList();
                    foreach (var (key, value) in
                             from modelEntityKvp in activeModelsForLoopWithoutEnumError
                             where modelEntityKvp.Value.Started
                             select modelEntityKvp)
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity TTL Counter Administration: Entity Model {key} is being started.");
                        }

                        await value.TtlCounterServerAsync().ConfigureAwait(false);

                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug(
                                $"Entity TTL Counter Administration: Entity Model {key} has finished will wait for {JubeEnvironment.AppSettings("WaitTtlCounterDecrement")} milliseconds.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                finally
                {
                    Thread.Sleep(Int32.Parse(JubeEnvironment.AppSettings("WaitTtlCounterDecrement")));
                }
            }
        }
    }
}
