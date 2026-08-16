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

namespace Jube.TaskCancellation.TaskHelper
{
    using System.Diagnostics;

    public enum TaskType
    {
        SanctionsAsync = 1,
        TtlCountersAsync = 3,
        AbstractionRulesWithSearchKeysAsync = 4,
        CachePayloadLatestUpsertAsync = 5,
        CachePayloadUpsertAsync = 6,
        CachePayloadInsertAsync = 7,
        CacheTtlCounterEntryUpsertAsync = 8,
        CacheTtlCounterEntryIncrementAsync = 9,
        CacheSanctionUpdateAsync = 10,
        OnlineAggregationOfTtlCountersAsync = 11,
        ExecuteOutOfProcessAggregationOfTtlCountersAsync = 12,
        ExecuteTimeToLiveCounterIterationAsync = 13,
        CachePayloadLatestInsertAsync = 14,
        CacheSanctionInsertAsync = 15,
        ExecuteAbstractionRulesWithSearchKeyAsync = 16,
        BulkInsertCachePayloadRemovalBatchEntry = 17,
        SortedSetRemoveReferenceDate = 18,
        SetRemoveAsync = 19,
        PublishAsync = 20,
        HashDecrementBytes = 21,
        HashDecrementCount = 22,
        HashDeletePayload = 23,
        HashDeletePayloadBulk = 24,
        AppendBulkCleanupOfPayloadGuids = 25,
        SortedSetRemoveReferenceDateLatest = 26,
        HashDecrementLatestCount = 27,
        HashDeletePayloadLatest = 28,
        CachePayloadLatestRemovalBatchEntry = 29,
        ProcessTtlCounterDeprecation = 30,
        BulkInsertTtlCounterEntryRemovalBatchResponseTime = 31,
        BulkInsertCachePayloadLatestRemovalBatchEntry = 32,
        SortedSetLruJournalRemove = 33,
        BulkTtlCounterIdempotencyRemovalBatchEntry = 34,
        UpsertReferenceDateAsync = 35
    }

    public static class TaskHelper
    {
        [ThreadStatic]
        private static long lastSeenBytes;

        public static Task<TimedTaskResult> MeasureTaskTimeAndMemoryAllocatedAsync(TaskType taskType, Func<Task> taskFunc)
        {
            return Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                var startBytes = SafeGetAllocatedBytes();

                await taskFunc().ConfigureAwait(false);

                var endBytes = SafeGetAllocatedBytes();
                sw.Stop();

                var bytesAllocated = endBytes - startBytes;
                if (bytesAllocated < 0)
                {
                    bytesAllocated = endBytes;
                }

                var elapsedMicroseconds = (long)(sw.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
                return new TimedTaskResult(taskType, elapsedMicroseconds, bytesAllocated);
            });
        }

        private static long SafeGetAllocatedBytes()
        {
            var current = GC.GetAllocatedBytesForCurrentThread();

            if (current < lastSeenBytes)
            {
                lastSeenBytes = current + (lastSeenBytes - current);
            }
            else
            {
                lastSeenBytes = current;
            }

            return lastSeenBytes;
        }
    }
}
