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
    using System.Threading.Tasks;
    using Context;
    using Data.Context;
    using Data.Repository;

    public class PersistToActivationWatcherPollingTaskStarter(Context context)
    {
        public async Task StartAsync()
        {
            try
            {
                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        context.ConcurrentQueues.PersistToActivationWatcher.TryDequeue(out var payload);

                        if (payload != null)
                        {
                            try
                            {
                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Info(
                                        "Database Activation Watcher Persist: a message has been received to be persisted to the Database database Activation Watcher.");
                                }

                                var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                                        context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);

                                var repository = new ActivationWatcherRepository(dbContext);
                                try
                                {
                                    await repository.InsertAsync(payload, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                                }
                                catch (Exception ex) when (ex is not OperationCanceledException)
                                {
                                    context.Services.Log.Error(ex.ToString());
                                }
                                finally
                                {
                                    await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                                    await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info("Database Activation Watcher Persist: Closed and Disposed Connection.");
                                    }
                                }
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                context.Services.Log.Error($"Database Activation Watcher Persist: An error has occurred as {ex}.");
                            }
                        }
                        else
                        {
                            await Task.Delay(100, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Services.Log.Error($"Database Activation Watcher Persist: An error has occurred as {ex}.");
                    }
                }

                context.ConcurrentQueues.PersistToActivationWatcher.Clear();
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation PersistToActivationWatcherPollingAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"PersistToActivationWatcherPollingAsync: has produced an error {ex}");
            }
        }
    }
}
