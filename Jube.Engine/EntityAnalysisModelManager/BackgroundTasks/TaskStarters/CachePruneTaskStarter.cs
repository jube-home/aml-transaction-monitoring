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
    using System.Linq;
    using System.Threading.Tasks;
    using Context;
    using EntityAnalysisModel;

    public class CachePruneTaskStarter(Context context)
    {
        public async Task StartAsync()
        {
            try
            {
                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        "Cache Prune: Starting task.");
                }

                var waitCachePrune = Int32.Parse(context.Services.DynamicEnvironment.AppSettings("WaitCachePrune"));

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Cache Prune: Active models processed.  Will sleep for {waitCachePrune}.");
                }

                var limit = GetDeletionLimitOrDefaultIfNull();

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Cache Prune: Has built cache references with deletion chunk limit of {limit}.  Will proceed to enter loop for background deletion job.");
                }

                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Cache Prune: There are {context.EntityAnalysisModels.ActiveEntityAnalysisModels.Count} active models.");
                    }

                    foreach (var model in context.EntityAnalysisModels.ActiveEntityAnalysisModels.Values)
                    {
                        context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Cache Prune: For model {model.Instance.Id} the reference date will be looked up.");
                        }

                        var referenceDate = await context.Services.CacheService.CacheReferenceDateRepository.GetReferenceDateAsync(model.Instance.TenantRegistryId, model.Instance.Guid)
                            .ConfigureAwait(false);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Cache Prune: For model {model.Instance.Id} the reference date for {model.References.ReferenceDateName} is {referenceDate}.  " +
                                $"Will test not null before proceeding to delete.  Will move to calculate the threshold date.");
                        }

                        if (!referenceDate.HasValue)
                        {
                            continue;
                        }

                        var thresholdReferenceDatePayload = GetThresholdReferenceDateForDeletion(model, referenceDate);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Cache Prune: For model {model.Instance.Id} the threshold reference date for " +
                                $"{model.References.ReferenceDateName} is {thresholdReferenceDatePayload}.  Will now instruct the delete if threshold date not null.");
                        }

                        if (thresholdReferenceDatePayload == null)
                        {
                            continue;
                        }

                        await context.Services.CacheService.CachePayloadRepository.DeleteByReferenceDatePreferReplicaAsync(model.Instance.TenantRegistryId, model.Instance.Guid,
                            thresholdReferenceDatePayload.Value, limit,
                            context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Cache Prune: For model {model.Instance.Id} deletion routine has returned in the Payload repository.");
                        }

                        await context.Services.CacheService.CachePayloadLatestRepository.DeleteByReferenceDateAsync(model.Instance.TenantRegistryId, model.Instance.Guid,
                            referenceDate.Value, thresholdReferenceDatePayload.Value, limit,
                            model.Collections.DistinctSearchKeys.Select(distinctSearchKey
                                => (distinctSearchKey.Key,
                                    distinctSearchKey.Value.SearchKeyTtlInterval,
                                    distinctSearchKey.Value.SearchKeyTtlIntervalValue)).ToList()).ConfigureAwait(false);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Cache Prune: For model {model.Instance.Id} deletion routine has returned in the Payload Latest " +
                                $"repository.  Will now loop around search keys to begin deletion of sorted sets linking to payload.");
                        }
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
                context.Services.Log.Error($"CachePruneAsync: Has produced an error {ex}");
            }
        }

        private static DateTime? GetThresholdReferenceDateForDeletion(EntityAnalysisModel model,
            DateTime? referenceDate)
        {
            if (referenceDate == null)
            {
                return null;
            }

            var thresholdReferenceDate = model.Cache.CacheTtlInterval switch
            {
                'd' => referenceDate.Value.AddDays(model.Cache.CacheTtlIntervalValue * -1),
                'h' => referenceDate.Value.AddHours(model.Cache.CacheTtlIntervalValue * -1),
                'n' => referenceDate.Value.AddMinutes(model.Cache.CacheTtlIntervalValue * -1),
                's' => referenceDate.Value.AddSeconds(model.Cache.CacheTtlIntervalValue * -1),
                'm' => referenceDate.Value.AddMonths(model.Cache.CacheTtlIntervalValue * -1),
                'y' => referenceDate.Value.AddYears(model.Cache.CacheTtlIntervalValue * -1),
                _ => referenceDate.Value.AddDays(model.Cache.CacheTtlIntervalValue * -1)
            };
            return thresholdReferenceDate;
        }

        private int GetDeletionLimitOrDefaultIfNull()
        {
            var limit = 100;
            if (context.Services.DynamicEnvironment.AppSettings("CacheTtlDeleteLimit") != null)
            {
                limit = Int32.Parse(context.Services.DynamicEnvironment.AppSettings("CacheTtlDeleteLimit"));
            }

            return limit;
        }
    }
}
