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

namespace Jube.Engine.EntityAnalysisModelManager
{
    using System;
    using System.Threading.Tasks;
    using BackgroundTasks.TaskStarters;
    using EntityAnalysisModel.Context.Extensions;
    using Jube.Engine.BackgroundTasks.Context;

    public class EntityAnalysisModelManager(Context context)
    {
        public BackgroundTasks.Context.Context Context { get; } = new BackgroundTasks.Context.Context
        {
            JsonSerializationHelper = context.JsonSerializationHelper,
            Services =
            {
                Log = context.Services.Log,
                DynamicEnvironment = context.Services.DynamicEnvironment,
                RabbitMqConnection = context.Services.RabbitMqConnection,
                CacheService = context.Services.CacheService,
                TaskCoordinator = context.Services.TaskCoordinator,
                ReportConnectionString = context.Services.ReportConnectionString
            },
            Caching =
            {
                HashCacheAssembly = context.Services.HashCacheAssembly,
                SanctionsEntries = context.Sanctions.SanctionsEntries
            },
            EntityAnalysisModels =
            {
                EntityAnalysisInstanceGuid = Guid.NewGuid()
            },
            ConcurrentQueues =
            {
                PendingNotifications = context.ConcurrentQueues.PendingNotifications,
                PendingEntityInvoke = context.ConcurrentQueues.PendingEntityInvoke,
                PendingCases = context.ConcurrentQueues.PendingCases
            }
        };

        public async Task StartAsync()
        {
            await Context.LogStartInstanceAsync().ConfigureAwait(false);

            StartModelSync();
            StartArchiver();
            StartActivationWatcherArchive();
            StartAbstractionRuleCaching();
            StartTtlCounterServer();
            StartReprocessing();
            StartCachePrune();
        }

        private void StartCachePrune()
        {
            if (!Context.Services.DynamicEnvironment.AppSettings("CachePruneServer").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var cachePruneTaskStarter = new CachePruneTaskStarter(Context);
            Context.Tasks.CachePruneAsyncTask = Context.Services.TaskCoordinator.RunAsync("CachePruneTask", _ => cachePruneTaskStarter.StartAsync());
        }

        private void StartReprocessing()
        {
            if (Context.Services.DynamicEnvironment.AppSettings("EnableReprocessing").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                int i;
                var threadCount = Int32.Parse(Context.Services.DynamicEnvironment.AppSettings("ReprocessingThreads"));

                for (i = 1; i <= threadCount; i++)
                {
                    if (Context.Services.Log.IsDebugEnabled)
                    {
                        Context.Services.Log.Debug($"Entity Start: Starting Reprocessing routine for thread {i}.");
                    }

                    var reprocessingTaskStarter = new ReprocessingTaskStarter(Context);
                    Context.Tasks.ReprocessingAsyncTasks.Add(Context.Services.TaskCoordinator.RunAsync("EntityReprocessingTask", _ => reprocessingTaskStarter.StartAsync()));

                    if (Context.Services.Log.IsDebugEnabled)
                    {
                        Context.Services.Log.Debug($"Entity Start: Started Reprocessing in start routine for thread {i}.");
                    }
                }
            }
            else
            {
                if (Context.Services.Log.IsDebugEnabled)
                {
                    Context.Services.Log.Debug("Entity Start: Has not started reprocessing as it is disabled.");
                }
            }
        }

        private void StartTtlCounterServer()
        {
            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug(
                    $"Entity Start: TTL Counter Administration is set to be {Context.Services.DynamicEnvironment.AppSettings("EnableTtlCounter")} on this node.");
            }

            if (!Context.Services.DynamicEnvironment.AppSettings("EnableTtlCounter").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug(
                    $"Entity Start: Starting the TTL Counter Administration with a polling rate of {Context.Services.DynamicEnvironment.AppSettings("WaitTtlCounterDecrement")}.");
            }

            var ttlCounterAdministrationTaskStarter = new TtlCounterAdministrationTaskStarter(Context);
            Context.Tasks.TtlCounterAdministrationTask = Context.Services.TaskCoordinator.RunAsync("TtlCounterAdministrationTask", _ => ttlCounterAdministrationTaskStarter.StartAsync());

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Entity Start: TTL Counter Administration.");
            }
        }

        private void StartAbstractionRuleCaching()
        {
            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug(
                    $"Entity Start: The Rule Cache Server is set to be {Context.Services.DynamicEnvironment.AppSettings("EnableSearchKeyCache")} on this node.");
            }

            if (!Context.Services.DynamicEnvironment.AppSettings("EnableSearchKeyCache")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Entity Start: Starting the Rule Cache Engine.");
            }

            var abstractionRuleCachingTaskStarter = new AbstractionRuleCachingTaskStarter(Context);
            Context.Tasks.AbstractionRuleCachingTask = Context.Services.TaskCoordinator.RunAsync("AbstractionRuleCachingTask", _ => abstractionRuleCachingTaskStarter.StartAsync());

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Entity Start: Started the Rule Cache Engine.");
            }
        }

        private void StartActivationWatcherArchive()
        {
            if (!Context.Services.DynamicEnvironment.AppSettings("ActivationWatcherAllowPersist")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int i;
            var threadCount = Int32.Parse(Context.Services.DynamicEnvironment.AppSettings("ActivationWatcherPersistThreads"));
            for (i = 1; i <= threadCount; i++)
            {
                var persistToActivationWatcherPollingTaskStarter = new PersistToActivationWatcherPollingTaskStarter(Context);
                Context.Tasks.PersistToActivationWatcherPollingTasks.Add(Context.Services.TaskCoordinator.RunAsync("ActivationWatcherTask", _ => persistToActivationWatcherPollingTaskStarter.StartAsync()));

                if (Context.Services.Log.IsDebugEnabled)
                {
                    Context.Services.Log.Debug(String.Format("Entity Start: Started Activation Watcher Persist Thread " + i + "."));
                }
            }
        }

        private void StartArchiver()
        {
            try
            {
                int i;

                if (Context.Services.Log.IsDebugEnabled)
                {
                    Context.Services.Log.Debug(
                        $"Entity Start: There are {Context.Services.DynamicEnvironment.AppSettings("ArchiverPersistThreads")} SQL Persist threads about to start.");
                }

                var threadCount = Int32.Parse(Context.Services.DynamicEnvironment.AppSettings("ArchiverPersistThreads"));
                for (i = 1; i <= threadCount; i++)
                {
                    var archiverTaskStarter = new ArchiverTaskStarter(Context, i);
                    Context.Tasks.ArchiverTasks.Add(Context.Services.TaskCoordinator.RunAsync("ArchiverTask",
                        _ => archiverTaskStarter.StartAsync()));

                    if (Context.Services.Log.IsDebugEnabled)
                    {
                        Context.Services.Log.Debug($"Entity Start: Started Database Persist Thread {i}.");
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                Context.Services.Log.Info($"Graceful Cancellation Archiver: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                Context.Services.Log.Error($"Archiver: has produced an error {ex}");
            }
        }

        private void StartModelSync()
        {
            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Entity Start: Starting Model Sync.");
            }

            var modelSyncTaskStarter = new ModelSyncTaskStarter(Context);
            Context.Tasks.ModelSyncTask = Context.Services.TaskCoordinator.RunAsync("ModelSyncTask", _ => modelSyncTaskStarter.StartAsync());

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Entity Start: Started Model Sync in start routine.");
            }
        }
    }
}
