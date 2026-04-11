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
    using System.Linq;
    using System.Threading.Tasks;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
    using ReflectionHelpers;

    public static class InlineScriptsExtensions
    {
        public static async Task<Context> ExecuteInlineScriptsAsync(this Context context)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id}.");
            }

            await IterateAndProcessAsync(context);
            StorePerformanceFromStopwatch(context);

            return context;
        }

        private static void StorePerformanceFromStopwatch(Context context)
        {
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.InlineScript = (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} inline script invocation has concluded {context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency} ns.");
            }
        }

        private static async Task IterateAndProcessAsync(Context context)
        {
            foreach (var inlineScript in context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts.Where(s => s.EntityAnalysisModelInlineScriptEvents
                         .Any(e => e.EntityAnalysisModelInlineScriptEventType == EntityAnalysisModelInlineScriptEventTypeEnum.Payload)))
            {
                try
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is going to invoke {inlineScript.InlineScriptCode}.");
                    }

                    await ReflectInlineScriptHelper.ExecuteAsync(inlineScript, context);
                    context.EntityAnalysisModelInstanceEntryPayload.JObject = null;
                    
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has invoked {inlineScript.InlineScriptCode}.");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has tried to invoke inline script {inlineScript.InlineScriptCode} but it has produced an error as {ex}.");
                }
            }
        }
    }
}
