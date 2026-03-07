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
    using System.Threading.Tasks;
    using EntityAnalysisModelManager.BackgroundTasks.TaskStarters.Archiver;

    public static class ActivationRuleBuildArchivePayloadExtensions
    {
        public static async Task<Context> ActivationRuleBuildArchivePayloadAsync(this Context context)
        {
            context.EntityAnalysisModelInstanceEntryPayload.ArchiveEnqueueDate = DateTime.Now;

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} " +
                    $"has been selected for sampling or case creation is been specified. " +
                    $"Is building the XML payload from the payload created. ArchiveEnqueueDate set to {context.EntityAnalysisModelInstanceEntryPayload.ArchiveEnqueueDate}.");
            }

            CalculateMemoryUsedInThreadForPayload(context);

            context.Log.Info(
                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                $"and model {context.EntityAnalysisModel.Instance.Id} a payload has been created for archive.");

            if (context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
            {
                await ArchiverProcessing.CaseCreationAndArchiveStorageAsync(context.EntityAnalysisModelInstanceEntryPayload,
                    context.EntityAnalysisModel.JsonSerializationHelper,
                    null, null, context.Environment, context.EntityAnalysisModel.Services.CacheService, context.Log).ConfigureAwait(false);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} a payload has been added for archive synchronously as it is set for reprocessing.");
                }
            }
            else
            {
                context.EntityAnalysisModel.ConcurrentQueues.PersistToDatabaseAsync.Enqueue(context.EntityAnalysisModelInstanceEntryPayload);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} a payload has been added for archive asynchronously.");
                }
            }

            return context;
        }

        private static void CalculateMemoryUsedInThreadForPayload(Context context)
        {

            if (context.StartBytesUsed.HasValue)
            {
                var currentBytes = GC.GetAllocatedBytesForCurrentThread();
                context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Memory = context.StartBytesUsed.Value - GC.GetAllocatedBytesForCurrentThread();

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $"and model {context.EntityAnalysisModel.Instance.Id} has start bytes of {context.StartBytesUsed} and currentBytes {currentBytes}. " +
                        $"The used bytes is {context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Memory}.");
                }
            }
            else
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $"and model {context.EntityAnalysisModel.Instance.Id} does not have start bytes recorded in the context so can't " +
                        $"calculate memory usage.");
                }
            }
        }
    }
}
