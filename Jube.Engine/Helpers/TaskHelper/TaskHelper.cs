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

namespace Jube.Engine.Helpers.TaskHelper
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public enum TaskType
    {
        SanctionsAsync = 1,
        DictionaryKvPsAsync = 2,
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
        ExecuteAbstractionRulesWithSearchKeyAsync = 16
    }
    
    public static class TaskHelper
    {
        [ThreadStatic]
        private static long lastSeenBytes;

        public static async Task<TimedTaskResult> MeasureTaskTimeAndMemoryAllocated(TaskType taskType, Func<Task> taskFunc)
        {
            return await Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                var startBytes = SafeGetAllocatedBytes();

                await taskFunc();

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