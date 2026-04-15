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

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Context;
    using Data.Context;
    using EntityAnalysisModel.Context.Extensions;
    using EntityAnalysisModel.Context.Helpers;
    using RabbitMQ.Client;

    public class ModelSyncTaskStarter
    {
        private readonly Context context;
        private readonly IModel rabbitMqChannel;
        public ModelSyncTaskStarter(Context context)
        {
            this.context = context;

            if (context.Services.RabbitMqConnection != null)
            {
                rabbitMqChannel = context.Services.RabbitMqConnection.CreateModel();
                rabbitMqChannel.QueueDeclare("jubeNotifications", false, false, false, null);
                rabbitMqChannel.ExchangeDeclare("jubeActivations", ExchangeType.Fanout);
                rabbitMqChannel.ExchangeDeclare("jubeOutbound", ExchangeType.Fanout);
            }
        }

        public async Task StartAsync()
        {
            try
            {
                var startupTenantRegistrySchedule = true;

                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug("Entity Model Sync: Making a connection to the Database database.");
                    }

                    var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                        context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);

                    try
                    {
                        var binaryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        var frameworkPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug("Entity Model Sync: Connected to the Database database.");
                        }

                        var scheduledModels = await TenantRegistryScheduleHelpers.GetScheduledAsync(context.Services.Log, dbContext, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        
                        foreach (var scheduledModel in scheduledModels)
                        {
                            context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                            var entityAnalysisModelContext = new EntityAnalysisModel.Context.Context
                            {
                                JsonSerializationHelper = context.JsonSerializationHelper,
                                Services =
                                {
                                    CancellationToken = context.Services.TaskCoordinator.CancellationToken,
                                    Log = context.Services.Log,
                                    DbContext = dbContext,
                                    DynamicEnvironment = context.Services.DynamicEnvironment,
                                    RabbitMqChannel = rabbitMqChannel,
                                    CacheService = context.Services.CacheService
                                },
                                Paths =
                                {
                                    BinaryPath = binaryPath,
                                    FrameworkPath = frameworkPath
                                },
                                EntityAnalysisModels =
                                {
                                    EntityAnalysisModelInlineScripts = context.EntityAnalysisModels.InlineScripts,
                                    ActiveEntityAnalysisModels = context.EntityAnalysisModels.ActiveEntityAnalysisModels,
                                    EntityAnalysisInstanceGuid = context.EntityAnalysisModels.EntityAnalysisInstanceGuid,
                                    SanctionsEntries = context.Caching.SanctionsEntries
                                },
                                Caching =
                                {
                                    HashCacheAssembly = context.Caching.HashCacheAssembly
                                },
                                ConcurrentQueues =
                                {
                                    PersistToActivationWatcherAsync = context.ConcurrentQueues.PersistToActivationWatcher,
                                    PendingNotifications = context.ConcurrentQueues.PendingNotifications,
                                    Callbacks = context.ConcurrentQueues.Callbacks,
                                    PendingEntityInvoke = context.ConcurrentQueues.PendingEntityInvoke
                                }
                            };

                            await entityAnalysisModelContext.SyncEntityAnalysisInlineScriptAsync().ConfigureAwait(false);
                            await entityAnalysisModelContext.ConfigureTokenParserForSecurityAsync().ConfigureAwait(false);

                            if (scheduledModel.SynchronisationPending || startupTenantRegistrySchedule)
                            {
                                context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                                await entityAnalysisModelContext.SyncEntityAnalysisModelsAsync(scheduledModel.TenantRegistryId).ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelListsAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelDictionariesAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelRequestXPathAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelInlineScriptsAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelInlineFunctionsAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelGatewayRulesAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelSanctionsAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelAbstractionRulesAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelAbstractionCalculationsAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelTtlCountersAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelHttpAdaptationAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncExhaustiveSearchInstancesAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelActivationRulesAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelTagsAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.ConfirmSyncAsync(scheduledModel.TenantRegistryId).ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelListsAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelDictionariesAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncSuppressionAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncActivationRuleSuppressionAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelApiUsersAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.StartupModelAsync().ConfigureAwait(false);
                                context.EntityAnalysisModels.EntityModelsHasLoadedForStartup = true;
                            }
                            else
                            {
                                await entityAnalysisModelContext.SyncExhaustiveSearchInstancesAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelListsAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelDictionariesAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncSuppressionAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncActivationRuleSuppressionAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.SyncEntityAnalysisModelApiUsersAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.StoreRuleCounterValuesAsync().ConfigureAwait(false);
                                await entityAnalysisModelContext.HeartbeatThisModelAsync(scheduledModel.TenantRegistryId).ConfigureAwait(false);
                            }
                        }
                        
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug("Entity Start: Closing the database connection. Waiting.");
                        }
                        
                        if (startupTenantRegistrySchedule)
                        {
                            startupTenantRegistrySchedule = false;
                        }

                        await Task.Delay(Int32.Parse(context.Services.DynamicEnvironment.AppSettings("ModelSynchronisationWait")), context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                        throw;
                    }
                    catch (Exception ex)
                    {
                        await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                        context.Services.Log.Error($"ModelSyncAsync: Has produced an error {ex} waiting.");

                        await Task.Delay(Int32.Parse(context.Services.DynamicEnvironment.AppSettings("ModelSynchronisationWait")), context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation ModelSyncAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"ModelSyncAsync: has produced an error {ex}");
            }
        }
    }
}
