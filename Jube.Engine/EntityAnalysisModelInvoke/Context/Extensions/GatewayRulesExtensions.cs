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
    using System.Threading;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;

    public static class GatewayRulesExtensions
    {
        public static Context ExecuteGatewayRules(this Context context)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is going to invoke Gateway Rules.");
            }

            IterateAndProcess(context);
            StorePerformanceFromStopwatch(context);

            return context;
        }

        private static void StorePerformanceFromStopwatch(Context context)
        {
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.Gateway = (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} Gateway Rules have concluded {context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency} ns.");
            }
        }

        private static void IterateAndProcess(Context context)
        {
            var gatewaySample = context.Random.NextDouble();

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has created a Gateway Sample of {gatewaySample}.");
            }

            var rulesCount = context.EntityAnalysisModel.Collections.ModelGatewayRules.Count;
            for (var i = 0; i < rulesCount; i++)
            {
                var gatewayRule = context.EntityAnalysisModel.Collections.ModelGatewayRules[i];
                try
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is going to invoke Gateway Rule {gatewayRule.EntityAnalysisModelGatewayRuleId} with a gateway sample of {gatewaySample}.  The model's Gateway Sample is {gatewayRule.GatewaySample} to be tested against {gatewaySample} .");
                    }

                    if (gatewaySample >= gatewayRule.GatewaySample)
                    {
                        continue;
                    }

                    IncrementEvaluationCounter(gatewayRule);

                    if (!gatewayRule.GatewayRuleCompileDelegate(context.EntityAnalysisModelInstanceEntryPayload.Payload,
                            context.EntityAnalysisModel.Dependencies.EntityAnalysisModelLists,
                            context.EntityAnalysisModelInstanceEntryPayload.Dictionary,
                            context.Log))
                    {
                        continue;
                    }

                    context.EntityAnalysisModelInstanceEntryPayload.MatchedGatewayRule = true;
                    context.EntityAnalysisModelInstanceEntryPayload.ResponseElevationLimit = gatewayRule.MaxResponseElevation;

                    IncrementGatewayRuleCounters(context, gatewayRule);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is going to invoke Gateway Rule {gatewayRule.EntityAnalysisModelGatewayRuleId} as it has matched. The max response elevation has been set to {context.EntityAnalysisModelInstanceEntryPayload.ResponseElevationLimit} and Model Invoke Gateway Counter has been set to {context.EntityAnalysisModel.Counters.ModelInvokeGatewayCounter}. The Entity Model Gateway Rule Counter has been set to {gatewayRule.ActivationCounter}.");
                    }

                    break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has tried to invoke Gateway Rule {gatewayRule.EntityAnalysisModelGatewayRuleId} but it has caused an error as {ex}.");
                }
            }
        }

        private static void IncrementEvaluationCounter(EntityModelGatewayRule gatewayRule)
        {
            Interlocked.Increment(ref gatewayRule.EvaluationCounter);
        }

        private static void IncrementGatewayRuleCounters(Context context, EntityModelGatewayRule gatewayRule)
        {
            Interlocked.Increment(ref context.EntityAnalysisModel.Counters.ModelInvokeGatewayCounter);
            Interlocked.Increment(ref gatewayRule.ActivationCounter);
            gatewayRule.ActivationCounterDate = DateTime.UtcNow;
        }
    }
}
