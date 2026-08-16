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

    public class LruJournalPruneTaskStarter(Context context)
    {
        public async Task StartAsync()
        {
            try
            {
                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        "Lru Journal Prune: Starting task.");
                }

                var lruJournalMaxAgeInterval = context.Services.DynamicEnvironment.AppSettings("LruJournalMaxAgeInterval");
                var lruJournalMaxAgeValue = context.Services.DynamicEnvironment.AppSettings("LruJournalMaxAgeValue");

                if (!Double.TryParse(lruJournalMaxAgeValue, out var value))
                {
                    value = 1;
                }

                var lruJournalMaxAgeTimeSpan = lruJournalMaxAgeInterval switch
                {
                    "n" =>
                        TimeSpan.FromMinutes(value),
                    "h" =>
                        TimeSpan.FromHours(value),
                    _ => TimeSpan.FromDays(value)
                };

                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Lru Journal Prune: There are {context.EntityAnalysisModels.ActiveEntityAnalysisModels.Count} active models.");
                    }

                    foreach (var model in context.EntityAnalysisModels.ActiveEntityAnalysisModels.Values)
                    {
                        context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Lru Journal Prune: For model {model.Instance.Id} calling the cache service prune method.");
                        }

                        var removed = await context.Services.CacheService.CachePayloadRepository
                            .PurgeExpiredLruJournalEntriesAsync
                            (model.Instance.TenantRegistryId,
                                model.Instance.EntityAnalysisInstanceGuid,
                                lruJournalMaxAgeTimeSpan);

                        if (removed > 0)
                        {
                            context.Services.Log.Info(
                                $"Lru Journal Prune: For model {model.Instance.Id} has removed {removed}.");
                        }
                    }

                    var waitCachePrune = Int32.Parse(context.Services.DynamicEnvironment.AppSettings("WaitLruJournalPrune"));

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Lru Journal Prune: Active models processed.  Will sleep for {waitCachePrune}.");
                    }

                    await Task.Delay(waitCachePrune, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation CachePruneAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"Lru Journal Prune: Has produced an error {ex}");
            }
        }
    }
}
