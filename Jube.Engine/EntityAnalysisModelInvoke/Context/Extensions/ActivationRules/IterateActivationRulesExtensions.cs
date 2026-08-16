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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Cache;
    using Data.Poco;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
    using Models.CaseManagement;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using RabbitMQ.Client;
    using ReflectionHelpers;
    using EntityAnalysisModel=EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
    using EntityAnalysisModelActivationRule=EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelActivationRule;

    public static class IterateActivationRulesExtensions
    {
        public static async Task<(int activationRuleCount, CreateCase createCase, int? prevailingActivationRuleId)> IterateAndProcessAsync(this Context context,
            CacheService cacheService, Dictionary<int, EntityAnalysisModel> availableModels,
            IModel rabbitMqChannel)
        {
            var rulesCount = context.EntityAnalysisModel.Collections.ModelActivationRules.Count;
            var prevailingActivationRuleName = String.Empty;
            var responseElevationHighWaterMark = 0d;
            var suppressedActivationRules = new List<string>(rulesCount);
            var activationRuleCount = 0;
            CreateCase createCase = null;
            int? prevailingActivationRuleId = null;

            foreach (var evaluateActivationRule in context.EntityAnalysisModel.Collections.ModelActivationRules)
            {
                try
                {
                    var suppressed = false;
                    if (context.ActivationRuleGetSuppressedModel(ref suppressedActivationRules) || context.CheckSuppressedResponseElevation())
                    {
                        suppressed = true;

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info($"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} activation rule {evaluateActivationRule.Id} is suppressed at the model level or has exceeded response elevation counter at {context.EntityAnalysisModel.ConcurrentQueues.ResponseElevationEntries.Count}.");
                        }
                    }
                    else
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} activation rule {evaluateActivationRule.Id} is not suppressed at the model level, will test at rule level.");
                        }

                        if (!evaluateActivationRule.EnableReprocessing && context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
                        {
                            suppressed = true;

                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} activation rule {evaluateActivationRule.Id} is suppressed at the activation rule level because of reprocessing.");
                            }
                        }
                        else if (suppressedActivationRules is { Count: > 0 })
                        {
                            suppressed = suppressedActivationRules.Contains(evaluateActivationRule.Name);

                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    suppressed
                                        ? $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} activation rule {evaluateActivationRule.Id} is suppressed at the activation rule level."
                                        : $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} activation rule {evaluateActivationRule.Id} is not suppressed at the activation rule level.");
                            }
                        }
                    }

                    var activationSample = evaluateActivationRule.ActivationSample >= context.Random.NextDouble();
                    if (!activationSample)
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id}  has failed in sampling so certain activations will not take place even if there is a match on the activation rule.");
                        }

                        continue;
                    }

                    UpdateEvaluationCount(evaluateActivationRule);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id}  has passed sampling and is eligible for activation.");
                    }

                    var matched = ReflectRuleHelper.Execute(
                        evaluateActivationRule,
                        context.EntityAnalysisModel,
                        context.EntityAnalysisModelInstanceEntryPayload,
                        context.EntityAnalysisModelInstanceEntryPayload.Dictionary,
                        context.Log);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id}  has finished testing the activation rule and it has a matched status of {matched}.");
                    }

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is checking for Activation Rule Evaluation InlineScripts looking for context Guid:{evaluateActivationRule.Guid} and or Name:{evaluateActivationRule.Name}.");
                    }

                    foreach (var inlineScript in context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts.Where(s => s.EntityAnalysisModelInlineScriptEvents
                                 .Any(e => e.EntityAnalysisModelInlineScriptEventType == EntityAnalysisModelInlineScriptEventTypeEnum.AbstractionRuleOverride
                                           && (e.Guid == evaluateActivationRule.Guid || e.Guid == Guid.Empty)
                                           && (e.Name == evaluateActivationRule.Name || String.IsNullOrEmpty(e.Name))
                                 )))
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is about to execute Inline Script Id: {inlineScript.Id}.");
                        }

                        if (await ReflectInlineScriptHelper.ExecuteAsync(inlineScript, context).ConfigureAwait(false))
                        {
                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} matched Inline Script Id: {inlineScript.Id}.");
                            }

                            matched = true;
                        }
                        else
                        {
                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} did not match Inline Script Id: {inlineScript.Id}.");
                            }
                        }
                    }

                    if (!matched)
                    {
                        continue;
                    }

                    UpdateActivationCounter(evaluateActivationRule);

                    if (context.EntityAnalysisModelInstanceEntryPayload.Activation.ContainsKey(evaluateActivationRule.Name))
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} and has already added the activation rule {evaluateActivationRule.Id} on {evaluateActivationRule.Name} for processing.");
                        }

                        continue;
                    }

                    context.EntityAnalysisModelInstanceEntryPayload.Activation.Add(
                        evaluateActivationRule.Name,
                        new EntityModelActivationRulePayload
                        {
                            Visible = evaluateActivationRule.Visible
                        });

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} and has added the activation rule {evaluateActivationRule.Id} flag on {evaluateActivationRule.Name} to the activation buffer for processing.");
                    }

                    if (evaluateActivationRule.ReportTable)
                    {
                        context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                        {
                            ProcessingTypeId = 11,
                            Key = evaluateActivationRule.Name,
                            KeyValueBoolean = 1,
                            EntityAnalysisModelInstanceEntryGuid =
                                context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                        });

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} and has added the activation rule {evaluateActivationRule.Id} flag to the response payload also.");
                        }
                    }

                    context.ProcessResponseElevation(evaluateActivationRule, ref responseElevationHighWaterMark, suppressed);
                    await context.ActivationRuleNotificationAsync(evaluateActivationRule, suppressed, rabbitMqChannel, cacheService);

                    context.ActivationRuleCountsAndArchiveHighWatermark(evaluateActivationRule, suppressed, ref activationRuleCount,
                        ref prevailingActivationRuleId, ref prevailingActivationRuleName);

                    await context.ActivationRuleActivationWatcherAsync(evaluateActivationRule, suppressed, rabbitMqChannel);

                    createCase ??= await context.ActivationRuleCreateCaseObjectAsync(evaluateActivationRule, suppressed, cacheService);

                    await context.ActivationRuleTtlCounterAsync(evaluateActivationRule, availableModels, cacheService);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} error in TTL Counter processing as {ex} .");
                }
            }

            return (activationRuleCount, createCase, prevailingActivationRuleId);
        }

        private static void UpdateActivationCounter(EntityAnalysisModelActivationRule evaluateActivationRule)
        {

            Interlocked.Increment(ref evaluateActivationRule.ActivationCounter);
            evaluateActivationRule.ActivationCounterDate = DateTime.UtcNow;
        }

        private static void UpdateEvaluationCount(EntityAnalysisModelActivationRule evaluateActivationRule)
        {

            Interlocked.Increment(ref evaluateActivationRule.EvaluationCounter);
        }
    }
}
