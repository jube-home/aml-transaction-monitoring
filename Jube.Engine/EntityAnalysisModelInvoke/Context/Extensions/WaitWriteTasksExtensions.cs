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

    public static class WaitWriteTasksExtensions
    {
        public static async Task<Context> WaitWriteTasksAsync(this Context context)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                    $" is waiting for {context.PendingWriteTasks.Count} write tasks of which {context.PendingWriteTasks.Count(c => c.IsCompleted)} are completed.");
            }

            await Task.WhenAll(context.PendingWriteTasks.ToArray()).ConfigureAwait(false);

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.WriteTasksPerformance = new WriteTasksPerformance();
            var pendingReadTasksResults = await Task.WhenAll(context.PendingWriteTasks).ConfigureAwait(false);

            foreach (var pendingWriteTasksResult in pendingReadTasksResults)
            {
                switch (pendingWriteTasksResult.TaskType)
                {
                    case TaskType.CachePayloadLatestUpsertAsync:
                        context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.WriteTasksPerformance.CachePayloadLatestUpsertAsync = new TaskPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.CachePayloadUpsertAsync:
                        context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.WriteTasksPerformance.CachePayloadUpsertAsync = new TaskPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.CachePayloadInsertAsync:
                        context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.WriteTasksPerformance.CachePayloadInsertAsync = new TaskPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.CachePayloadLatestInsertAsync:
                        context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.WriteTasksPerformance.CachePayloadLatestInsertAsync = new TaskPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.CacheTtlCounterEntryUpsertAsync:
                        context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.WriteTasksPerformance.CacheTtlCounterEntryUpsertAsync = new TaskPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.CacheTtlCounterEntryIncrementAsync:
                        context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.WriteTasksPerformance.CacheTtlCounterEntryIncrementAsync = new TaskPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.CacheSanctionInsertAsync:
                        context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.WriteTasksPerformance.CacheSanctionInsertAsync = new TaskPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.CacheSanctionUpdateAsync:
                        context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.WriteTasksPerformance.CacheSanctionUpdateAsync = new TaskPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.SanctionsAsync:
                    case TaskType.TtlCountersAsync:
                    case TaskType.AbstractionRulesWithSearchKeysAsync:
                    case TaskType.OnlineAggregationOfTtlCountersAsync:
                    case TaskType.ExecuteOutOfProcessAggregationOfTtlCountersAsync:
                    case TaskType.ExecuteTimeToLiveCounterIterationAsync:
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

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.JoinWriteTasks = (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                    $" completed {context.PendingWriteTasks.Count} write tasks.");
            }

            return context;
        }
    }
}
