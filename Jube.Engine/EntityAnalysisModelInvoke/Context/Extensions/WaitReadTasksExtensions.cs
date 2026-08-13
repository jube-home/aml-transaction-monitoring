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
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
    using TaskCancellation.TaskHelper;

    public static class WaitReadTasksExtensions
    {
        public static async Task<Context> WaitReadTasksAsync(this Context context)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                    $" is waiting for {context.PendingReadTasks.Count} read tasks of which {context.PendingReadTasks.Count(c => c.IsCompleted)} are completed.");
            }

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.ReadTasksPerformance = new ReadTasksPerformance();
            var pendingReadTasksResults = await Task.WhenAll(context.PendingReadTasks).ConfigureAwait(false);
            foreach (var pendingReadTasksResult in pendingReadTasksResults)
            {
                switch (pendingReadTasksResult.TaskType)
                {
                    case TaskType.SanctionsAsync:
                        context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.ReadTasksPerformance.SanctionsAsync = new TaskPerformance(pendingReadTasksResult.ThreadMemory, pendingReadTasksResult.ComputeTime);
                        break;
                    case TaskType.TtlCountersAsync:
                        context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.ReadTasksPerformance.TtlCountersAsync = new TaskPerformance(pendingReadTasksResult.ThreadMemory, pendingReadTasksResult.ComputeTime);
                        break;
                    case TaskType.AbstractionRulesWithSearchKeysAsync:
                        context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.ReadTasksPerformance.AbstractionRulesWithSearchKeysAsync = new TaskPerformance(pendingReadTasksResult.ThreadMemory, pendingReadTasksResult.ComputeTime);
                        break;
                    case TaskType.CachePayloadLatestUpsertAsync:
                    case TaskType.CachePayloadUpsertAsync:
                    case TaskType.CachePayloadInsertAsync:
                    case TaskType.CacheTtlCounterEntryUpsertAsync:
                    case TaskType.CacheTtlCounterEntryIncrementAsync:
                    case TaskType.CacheSanctionUpdateAsync:
                    case TaskType.OnlineAggregationOfTtlCountersAsync:
                    case TaskType.ExecuteOutOfProcessAggregationOfTtlCountersAsync:
                    case TaskType.ExecuteTimeToLiveCounterIterationAsync:
                    case TaskType.CachePayloadLatestInsertAsync:
                    case TaskType.CacheSanctionInsertAsync:
                    case TaskType.ExecuteAbstractionRulesWithSearchKeyAsync:
                    case TaskType.BulkInsertCachePayloadRemovalBatchEntry:
                    case TaskType.SortedSetRemoveReferenceDate:
                    case TaskType.SetRemoveAsync:
                    case TaskType.PublishAsync:
                    case TaskType.HashDecrementBytes:
                    case TaskType.HashDecrementCount:
                    case TaskType.HashDeletePayload:
                    case TaskType.HashDeletePayloadBulk:
                    case TaskType.AppendBulkCleanupOfPayloadGuids:
                    case TaskType.SortedSetRemoveReferenceDateLatest:
                    case TaskType.HashDecrementLatestCount:
                    case TaskType.HashDeletePayloadLatest:
                    case TaskType.CachePayloadLatestRemovalBatchEntry:
                    case TaskType.ProcessTtlCounterDeprecation:
                    case TaskType.BulkInsertTtlCounterEntryRemovalBatchResponseTime:
                    case TaskType.BulkInsertCachePayloadLatestRemovalBatchEntry:
                    default:
                        break;
                }
            }

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.JoinReadTasks =
                (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                    $" completed {context.PendingReadTasks.Count}.");
            }

            return context;
        }
    }
}
