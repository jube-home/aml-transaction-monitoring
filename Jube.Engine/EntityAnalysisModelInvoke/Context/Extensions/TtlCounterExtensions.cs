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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Cache;
    using Data.Poco;
    using TaskCancellation.TaskHelper;
    using EntityAnalysisModelTtlCounter=EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelTtlCounter;

    public static class TtlCounterExtensions
    {
        public static async Task<Context> ExecuteTtlCountersAsync(this Context context)
        {
            try
            {
                await StartAndWaitOnTasksIfTtlCounterEnabledAtModelLevelAsync(context, context.EntityAnalysisModel.Services.CacheService).ConfigureAwait(false);
                StorePerformanceFromStopwatch(context);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Log.Error(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has caused an error in TTL Counters as {ex}.");
            }

            return context;
        }

        private static async Task StartAndWaitOnTasksIfTtlCounterEnabledAtModelLevelAsync(Context context, CacheService cacheService)
        {
            if (context.EntityAnalysisModel.Flags.EnableTtlCounter)
            {
                var tasks = new List<Task>
                {
                    TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.OnlineAggregationOfTtlCountersAsync, async () => await OnlineAggregationOfTtlCountersAsync(context, cacheService).ConfigureAwait(false)),
                    TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.ExecuteOutOfProcessAggregationOfTtlCountersAsync, async () => await OutOfProcessAggregationOfTtlCountersAsync(context, cacheService).ConfigureAwait(false))
                };

                await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
            }
            else
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} TTL Counter cache storage is not enabled so it cannot fetch TTL Counter Aggregation.");
                }
            }
        }

        private static void StorePerformanceFromStopwatch(Context context)
        {

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.TtlCountersAsync =
                (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);
        }

        private static Task OnlineAggregationOfTtlCountersAsync(Context context, CacheService cacheService)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} TTL Counter cache storage is enabled so it will now proceed to return the TTL Counters with online aggregation.");
            }

            return IterateAndProcessAsync(context, cacheService);
        }

        private static async Task IterateAndProcessAsync(Context context, CacheService cacheService)
        {
            var onlineTtlCounters = context.EntityAnalysisModel.Collections.ModelTtlCounters
                .Where(x => x.OnlineAggregation)
                .ToList();

            if (onlineTtlCounters.Count > 0)
            {
                var tasks = onlineTtlCounters.Select(async ttlCounter =>
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} creating predication for TTL Counter {ttlCounter.Id} is online aggregation.");
                    }

                    if (context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(ttlCounter.TtlCounterDataName))
                    {
                        AddToResponse(context, ttlCounter, await PerformOnlineAggregationFromCacheAsync(context, cacheService, ttlCounter).ConfigureAwait(false));

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} creating predication for TTL Counter {ttlCounter.Id} to {context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate}, the TTL Counter Name is {ttlCounter.Name}, the TTL Counter Data Name is {ttlCounter.TtlCounterDataName} and the TTL Counter Data Name Value is {context.EntityAnalysisModelInstanceEntryPayload.Payload[ttlCounter.TtlCounterDataName]}.");
                        }
                    }
                    else
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} was unable to fine a value for TTL Counter Data Name {ttlCounter.TtlCounterDataName} and TTL Counter Name {ttlCounter.Name}.");
                        }
                    }
                }).ToList();

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} Does not have any online TTL Counters.");
                }
            }
        }

        private static async Task<double> PerformOnlineAggregationFromCacheAsync(Context context, CacheService cacheService,
            EntityAnalysisModelTtlCounter ttlCounter)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} creating predication for TTL Counter {ttlCounter.Id} which has an interval type of {ttlCounter.TtlCounterInterval} and interval value of {ttlCounter.TtlCounterValue}.");
            }

            var adjustedTtlCounterDate = ttlCounter.TtlCounterInterval switch
            {
                "d" => context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate.AddDays(
                    ttlCounter.TtlCounterValue * -1),
                "h" => context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate.AddHours(
                    ttlCounter.TtlCounterValue * -1),
                "n" => context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate.AddMinutes(
                    ttlCounter.TtlCounterValue * -1),
                "s" => context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate.AddSeconds(
                    ttlCounter.TtlCounterValue * -1),
                "m" => context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate.AddMonths(
                    ttlCounter.TtlCounterValue * -1),
                "y" => context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate.AddYears(
                    ttlCounter.TtlCounterValue * -1),
                _ => context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate
            };

            var count = await cacheService.CacheTtlCounterEntryRepository.GetAggregationPreferReplicaAsync(
                context.EntityAnalysisModel.Instance.TenantRegistryId,
                context.EntityAnalysisModel.Instance.Guid,
                ttlCounter.Guid,
                ttlCounter.TtlCounterDataName,
                context.EntityAnalysisModelInstanceEntryPayload.Payload[ttlCounter.TtlCounterDataName].AsString(),
                adjustedTtlCounterDate,
                context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate
            ).ConfigureAwait(false);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has performed online aggregation for adjusted reference date {adjustedTtlCounterDate} and returned {count}.");
            }

            return count;
        }

        private static void AddToResponse(Context context, EntityAnalysisModelTtlCounter ttlCounter, double count)
        {

            lock (context.EntityAnalysisModelInstanceEntryPayload.TtlCounter)
            {
                if (context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.TryAdd(ttlCounter.Name, count))
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} TTL Counter {ttlCounter.Id} is missing, so will add this as name {ttlCounter.Name} with value of zero.");
                    }

                    if (!ttlCounter.ReportTable || context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
                    {
                        return;
                    }

                    context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                    {
                        ProcessingTypeId = 5,
                        Key = ttlCounter.Name,
                        KeyValueLong = (int)count,
                        EntityAnalysisModelInstanceEntryGuid =
                            context.EntityAnalysisModelInstanceEntryPayload
                                .EntityAnalysisModelInstanceEntryGuid
                    });

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} TTL Counters have concluded in {context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency} ns.");
                    }

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} TTL Counter {ttlCounter.Id} is missing, added this as name {ttlCounter.Name} with value of zero to the report payload also.");
                    }
                }
                else
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} TTL Counter {ttlCounter.Id} exists already, so nothing more added.");
                    }
                }
            }
        }

        private static Task<TimedTaskResult[]> OutOfProcessAggregationOfTtlCountersAsync(Context context, CacheService cacheService)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} will now look for TTL Counters from the cache.");
            }

            var ttlCounters = context.EntityAnalysisModel.Collections.ModelTtlCounters.FindAll(x => !x.OnlineAggregation);
            var tasks = ttlCounters.Select(ttlCounter =>
            {
                return TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.ExecuteTimeToLiveCounterIterationAsync, async () =>
                {
                    try
                    {
                        AddToResponse(context, ttlCounter, await GetCachedTtlCounterValueAsync(context, cacheService, ttlCounter).ConfigureAwait(false));
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} TTL Counter {ttlCounter.Id} has thrown an error as {ex}.");
                        }
                    }
                });
            }).ToList();

            return Task.WhenAll(tasks);
        }

        private static async Task<double> GetCachedTtlCounterValueAsync(Context context, CacheService cacheService, EntityAnalysisModelTtlCounter ttlCounter)
        {
            var ttlCounterValue = await cacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                    context.EntityAnalysisModel.Instance.Guid,
                    ttlCounter.Guid,
                    ttlCounter.TtlCounterDataName,
                    context.EntityAnalysisModelInstanceEntryPayload.Payload[ttlCounter.TtlCounterDataName].AsString()).ConfigureAwait(false);

            return ttlCounterValue;
        }
    }
}
