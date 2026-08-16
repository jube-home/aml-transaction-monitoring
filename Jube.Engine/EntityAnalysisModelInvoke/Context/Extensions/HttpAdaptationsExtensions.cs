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
    using System.Diagnostics;
    using System.Threading.Tasks;
    using HttpAdaptations;

    public static class HttpAdaptationsExtensions
    {
        public static async Task<Context> ExecuteHttpAdaptationsAsync(this Context context)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info($"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} will begin processing adaptations.");
            }

            await IterateAndProcessAsync(context).ConfigureAwait(false);
            StorePerformanceFromStopwatch(context);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info($"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} Adaptations have concluded.");
            }

            return context;
        }
        private static void StorePerformanceFromStopwatch(Context context)
        {

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.ExecuteHttpAdaptation = (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);
        }

        private static async Task IterateAndProcessAsync(Context context)
        {
            foreach (var modelAdaptation in context.EntityAnalysisModel.Collections.EntityAnalysisModelAdaptations)
            {
                try
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating {modelAdaptation.Id} is about to serialise the Entity Analysis Model Instance Entry Payload for the HTTP Adaptation POST.");
                    }

                    var adaptation = await context.RecallHttpEndpointAsync(modelAdaptation,
                        context.EntityAnalysisModel.JsonSerializationHelper.DefaultJsonSerializerSettingsSettings).ConfigureAwait(false);

                    context.EntityAnalysisModelInstanceEntryPayload.HttpAdaptation[modelAdaptation.Name] = adaptation;

                    if (adaptation.IsSuppressed && context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating {modelAdaptation.Id} and the HTTP Adaptation response for {modelAdaptation.Name} is suppressed (Error: {adaptation.Error ?? "none"}); Value will read as null to any rule evaluating it.");
                    }

                    context.ArchiveHttpAdaptation(modelAdaptation);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating {modelAdaptation.Id} produced an error {ex}.");
                    }
                }
            }
        }
    }
}
