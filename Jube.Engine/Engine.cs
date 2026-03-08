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
    using System.Threading;
    using System.Threading.Tasks;
    using BackgroundTasks.TaskStarters;
    using Cache;
    using DynamicEnvironment;
    using Exhaustive;
    using Helpers;
    using log4net;
    using RabbitMQ.Client;
    using TaskCancellation;
    using Context=BackgroundTasks.Context.Context;

    public class Engine(
        DynamicEnvironment dynamicEnvironment,
        ILog log,
        IConnection rabbitMqConnection,
        CacheService cacheService,
        JsonSerializationHelper jsonSerializationHelper,
        ITaskCoordinator taskCoordinator,
        string reportConnectionString = null)
    {
        public readonly Context Context = new Context
        {
            JsonSerializationHelper = jsonSerializationHelper,
            Services =
            {
                DynamicEnvironment = dynamicEnvironment,
                Log = log,
                RabbitMqConnection = rabbitMqConnection,
                CacheService = cacheService,
                TaskCoordinator = taskCoordinator,
                ReportConnectionString = reportConnectionString
            }
        };

        public async Task StartAsync()
        {
            try
            {
                await SpinWaitAndConvergeCacheServiceAsync(Context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                await StartSanctionsTaskAsync().ConfigureAwait(false);
                await StartEntityModelServerAsync().ConfigureAwait(false);
                await Task.WhenAll(SpinWaitAndConvergeSanctionsAsync(Context.Services.TaskCoordinator.CancellationToken), SpinWaitEntityModelsAsync(Context.Services.TaskCoordinator.CancellationToken)).ConfigureAwait(false);

                if (Context.Services.RabbitMqConnection != null)
                {
                    StartAmqp();
                    StartNotificationsViaAmqp();
                }
                else
                {
                    StartNotificationsViaConcurrentQueue();
                }

                StartCaseAutomationServer();
                StartAsyncEntityThreadsInLoop();
                StartManageCounters();
                StartTaggingStorage();
                StartExhaustiveTrainingServer();
                StartCaseCreation();

                if (Context.Services.Log.IsInfoEnabled)
                {
                    Context.Services.Log.Info("Start: The start routine has without error completed. Running.  Use cancel token to quit.");
                }

                Context.Ready = true;
            }
            catch (OperationCanceledException ex)
            {
                Context.Services.Log.Info($"Graceful Cancellation StartAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                Context.Services.Log.Error($"StartAsync: has produced an error {ex}");
            }
        }

        private void StartCaseCreation()
        {
            try
            {
                int i;

                if (Context.Services.Log.IsDebugEnabled)
                {
                    Context.Services.Log.Debug(
                        $"Start Case Creation: There are {Context.Services.DynamicEnvironment.AppSettings("CaseCreationThreads")} threads about to start.");
                }

                var threadCount = Int32.Parse(Context.Services.DynamicEnvironment.AppSettings("CaseCreationThreads"));
                for (i = 1; i <= threadCount; i++)
                {
                    var caseCreationTaskStarter = new CaseCreationTaskStarter(Context);
                    Context.Tasks.CaseCreationTasks.Add(Context.Services.TaskCoordinator.RunAsync("CaseCreationTask",
                        _ => caseCreationTaskStarter.StartAsync()));

                    if (Context.Services.Log.IsDebugEnabled)
                    {
                        Context.Services.Log.Debug($"Start Case Creation: Started Database Persist Thread {i}.");
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                Context.Services.Log.Info($"Graceful Cancellation Archiver: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                Context.Services.Log.Error($"Start Case Creation: has produced an error {ex}");
            }
        }

        private async Task SpinWaitEntityModelsAsync(CancellationToken token = default)
        {
            while (!Context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels.EntityModelsHasLoadedForStartup)
            {
                await Task.Delay(100, token).ConfigureAwait(false);
            }
        }

        private async Task SpinWaitAndConvergeSanctionsAsync(CancellationToken token = default)
        {
            while (!Context.Sanctions.SanctionsLoadedForStartup)
            {
                await Task.Delay(100, token).ConfigureAwait(false);
            }
        }

        private async Task SpinWaitAndConvergeCacheServiceAsync(CancellationToken token = default)
        {
            while (!Context.Services.CacheService.Ready)
            {
                await Task.Delay(100, token).ConfigureAwait(false);
            }
        }

        private void StartAmqp()
        {
            if (!Context.Services.DynamicEnvironment.AppSettings("EnableCallback").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var amqpTaskStarter = new AmqpTaskStarter(Context);
            Context.Tasks.AmqpTask = Context.Services.TaskCoordinator.RunAsync("AmqpTask", _ => amqpTaskStarter.StartAsync());
        }

        private void StartNotificationsViaAmqp()
        {
            if (!Context.Services.DynamicEnvironment.AppSettings("EnableCallback").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var notificationsViaAmqpStarter = new NotificationsViaAmqpStarter(Context);
            Context.Tasks.NotificationsViaAmqp = Context.Services.TaskCoordinator.RunAsync("NotificationsViaAmqpTask", _ => notificationsViaAmqpStarter.StartAsync());
        }

        private Task StartSanctionsTaskAsync()
        {
            if (!Context.Services.DynamicEnvironment.AppSettings("EnableSanction")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Start: Starting Sanctions routine.");
            }

            var sanctionsTaskStarter = new SanctionsTaskStarter(Context);
            Context.Tasks.SanctionsTask = Context.Services.TaskCoordinator.RunAsync("SanctionsTask", _ => sanctionsTaskStarter.StartAsync());

            if (Context.Services.Log.IsInfoEnabled)
            {
                Context.Services.Log.Info("Start: Starting Sanctions routine.");
            }

            return Task.CompletedTask;
        }

        private void StartExhaustiveTrainingServer()
        {
            if (!Context.Services.DynamicEnvironment.AppSettings("EnableExhaustiveTraining")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Start: Starting Exhaustive Training Server.");
            }

            var exhaustiveTrainingStarter = new Training(Context.Services.Log, Context.Services.DynamicEnvironment, Context.JsonSerializationHelper);
            Context.Tasks.ExhaustiveTrainingTask = Context.Services.TaskCoordinator.RunAsync("ExhaustiveTrainingTask", token => exhaustiveTrainingStarter.StartAsync(token));

            if (Context.Services.Log.IsInfoEnabled)
            {
                Context.Services.Log.Info("Start: Starting Exhaustive Training Server.");
            }
        }

        private void StartTaggingStorage()
        {
            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Start: Starting Tagging routine.");
            }

            var taggingStarter = new TaggingStarter(Context);
            Context.Tasks.TaggingTask = Context.Services.TaskCoordinator.RunAsync("TaggingTask", _ => taggingStarter.StartAsync());

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Start: Started Tagging routine.");
            }
        }

        private void StartManageCounters()
        {
            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Start: Starting Counters routine.");
            }

            var manageCountersStarter = new ManageCountersStarter(Context);
            Context.Tasks.ManageCountersTask = Context.Services.TaskCoordinator.RunAsync("CountersTask", _ => manageCountersStarter.StartAsync());

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Start: Started Counters Thread in start routine.");
            }
        }

        private void StartAsyncEntityThreadsInLoop()
        {
            var asyncThreads = Int32.Parse(Context.Services.DynamicEnvironment.AppSettings("ModelInvokeAsynchronousThreads"));

            for (var i = 1; i <= asyncThreads; i++)
            {
                if (Context.Services.Log.IsDebugEnabled)
                {
                    Context.Services.Log.Debug($"Starting Async Context routine for thread {i}.");
                }

                var asyncHttpContextCorrelationStarter = new AsyncHttpContextCorrelationStarter(Context);
                Context.Tasks.AsyncHttpContextCorrelationTasks.Add(Context.Services.TaskCoordinator.RunAsync("AsyncHttpContextCorrelationTask", _ => asyncHttpContextCorrelationStarter.StartAsync()));

                if (Context.Services.Log.IsDebugEnabled)
                {
                    Context.Services.Log.Debug($"Started Async Context in start routine for thread {i}.");
                }
            }
        }

        private void StartCaseAutomationServer()
        {
            if (!Context.Services.DynamicEnvironment.AppSettings("EnableCasesAutomation")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Entity Start: Starting the Cases Automation Thread.");
            }

            var caseAutomationStarter = new CaseAutomationStarter(Context);
            Context.Tasks.CaseAutomationTask = Context.Services.TaskCoordinator.RunAsync("CasesAutomationTask", _ => caseAutomationStarter.StartAsync());

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Entity Start: Started the Cases Automation Thread.");
            }
        }

        private void StartNotificationsViaConcurrentQueue()
        {
            if (!Context.Services.DynamicEnvironment.AppSettings("EnableNotification")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Entity Start: Starting the Notifications Thread.");
            }

            var notificationsViaConcurrentQueueStarter = new NotificationsViaConcurrentQueueStarter(Context);
            Context.Tasks.NotificationsViaConcurrentQueueTask = Context.Services.TaskCoordinator.RunAsync("NotificationRelayFromConcurrentQueueTask", _ => notificationsViaConcurrentQueueStarter.StartAsync());

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Entity Start: Started the Cases Automation Thread.");
            }
        }

        private Task StartEntityModelServerAsync()
        {
            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug(
                    $"Start: Checking if this is an entity model server, the conf332iguration key is set to {Context.Services.DynamicEnvironment.AppSettings("EnableEntityModel")}.");
            }

            if (!Context.Services.DynamicEnvironment.AppSettings("EnableEntityModel")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Start: Starting the entity subsystem.");
            }

            Context.Tasks.EntityAnalysisModelManager = new EntityAnalysisModelManager.EntityAnalysisModelManager(Context);
            Context.Tasks.EntityAnalysisModelManagerTask = Context.Services.TaskCoordinator.RunAsync("EntityAnalysisModelManagerTask", _ => Context.Tasks.EntityAnalysisModelManager.StartAsync());

            if (Context.Services.Log.IsDebugEnabled)
            {
                Context.Services.Log.Debug("Start: Started the entity subsystem.");
            }
            return Task.CompletedTask;
        }
    }
}
