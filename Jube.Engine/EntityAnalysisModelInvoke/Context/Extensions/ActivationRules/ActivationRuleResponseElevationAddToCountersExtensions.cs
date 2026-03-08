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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules
{
    using System;
    using System.Threading;

    public static class ActivationRuleResponseElevationAddToCountersExtensions
    {
        public static void ActivationRuleResponseElevationAddToCounters(this Context context)
        {
            if (!(context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Value > 0))
            {
                return;
            }

            Interlocked.Increment(ref context.EntityAnalysisModel.Counters.ResponseElevationCount);
            context.EntityAnalysisModel.ConcurrentQueues.BillingResponseElevationJournal.Enqueue(DateTime.Now);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} the response elevation is greater than 0 and has incremented counters for throttling.  The Billing Response Elevation Count is {context.EntityAnalysisModel.Counters.ResponseElevationCount}.");
            }
        }
    }
}
