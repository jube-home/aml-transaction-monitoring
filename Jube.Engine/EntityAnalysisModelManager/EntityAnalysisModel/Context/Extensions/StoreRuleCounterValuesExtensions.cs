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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Repository;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;

    public static class StoreRuleCounterValuesExtensions
    {
        public static async Task<Context> StoreRuleCounterValuesAsync(this Context context)
        {
            try
            {
                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose Synchronisation of the model counters.  Will now start with the Gateway Rule Counters.");
                    }

                    foreach (var gatewayRule in value.Collections.ModelGatewayRules)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        if (gatewayRule.ActivationCounter > 0 || gatewayRule.EvaluationCounter > 0)
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Checking if model {key} is about to update gateway rule id {gatewayRule.EntityAnalysisModelGatewayRuleId} and counter of {gatewayRule.ActivationCounter}.");
                            }

                            await UpdateGatewayRuleCounterAsync(context, gatewayRule).ConfigureAwait(false);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Checking if model {key} has finished processing updating gateway rule id {gatewayRule.EntityAnalysisModelGatewayRuleId} and counter of {gatewayRule.ActivationCounter}.");
                            }
                        }
                        else
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Checking if model {key} will not update gateway rule id {gatewayRule.EntityAnalysisModelGatewayRuleId} as counter is 0.");
                            }
                        }
                    }

                    foreach (var activationRule in value.Collections.ModelActivationRules)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        if (activationRule.ActivationCounter > 0 || activationRule.EvaluationCounter > 0)
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Checking if model {key} is about to update activation rule id {activationRule.Id} and counter of {activationRule.ActivationCounter}.");
                            }

                            await UpdateActivationRuleCounterAsync(context, activationRule).ConfigureAwait(false);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Checking if model {key} has finished processing updating activation rule id {activationRule.Id} and counter of {activationRule.ActivationCounter}.");
                            }
                        }
                        else
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Checking if model {key} will not update activation rule id {activationRule.Id} as counter is 0.");
                            }
                        }
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is finished Synchronisation of the model counters.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"StoreRuleCounterValuesAsync: Has produced an error {ex}");
            }

            return context;
        }

        private static async Task UpdateActivationRuleCounterAsync(Context context, EntityAnalysisModelActivationRule activationRule)
        {
            try
            {
                var evaluationCounter = activationRule.EvaluationCounter;
                var activationCounter = activationRule.ActivationCounter;
                var activationCounterDate = activationRule.ActivationCounterDate;

                var repository = new EntityAnalysisModelActivationRuleRepository(context.Services.DbContext);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelActivationRuleRepository.UpdateCounter for Activation Rule ID of {activationRule.Id} and counter of {activationCounter}.");
                }

                await repository.UpdateCounterAsync(activationRule.Id, evaluationCounter, activationCounter, activationCounterDate,
                    context.Services.CancellationToken).ConfigureAwait(false);

                Interlocked.Add(ref activationRule.EvaluationCounter, -evaluationCounter);
                Interlocked.Add(ref activationRule.ActivationCounter, -activationCounter);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Entity Start: Finished Executing EntityAnalysisModelActivationRuleRepository.UpdateCounter for Activation Rule ID of {activationRule.Id} and has drained counter of {activationCounter}.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error(
                    $"UpdateActivationRuleCounterAsync: Activation Rule ID {activationRule.Id} has created an error as {ex} on update counter.");
            }
        }

        private static async Task UpdateGatewayRuleCounterAsync(Context context, EntityModelGatewayRule gatewayRule)
        {
            try
            {
                var evaluationCounter = gatewayRule.EvaluationCounter;
                var activationCounter = gatewayRule.ActivationCounter;
                var activationCounterDate = gatewayRule.ActivationCounterDate;

                var repository = new EntityAnalysisModelGatewayRuleRepository(context.Services.DbContext);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Entity Start: Executing EntityAnalysisModelGatewayRuleRepository.EntityAnalysisModelGatewayRuleId for Gateway Rule ID of {gatewayRule.EntityAnalysisModelGatewayRuleId} and counter of {activationCounter}.");
                }

                await repository.UpdateCounterAsync(gatewayRule.EntityAnalysisModelGatewayRuleId, evaluationCounter, activationCounter, activationCounterDate,
                    context.Services.CancellationToken).ConfigureAwait(false);

                Interlocked.Add(ref gatewayRule.EvaluationCounter, -evaluationCounter);
                Interlocked.Add(ref gatewayRule.ActivationCounter, -activationCounter);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Entity Start: Finished EntityAnalysisModelGatewayRuleRepository.EntityAnalysisModelGatewayRuleId for Gateway Rule ID of {gatewayRule.EntityAnalysisModelGatewayRuleId} and has drained counter of {activationCounter}.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error(
                    $"UpdateGatewayRuleCounterAsync: Gateway Rule ID {gatewayRule.EntityAnalysisModelGatewayRuleId} has created an error as {ex} on update counter.");
            }
        }
    }
}
