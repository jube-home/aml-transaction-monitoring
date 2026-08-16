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
    using System.Threading.Tasks;
    using Cache;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Models.CaseManagement;

    public static class ActivationRuleCreateCaseObjectExtensions
    {
        public static async Task<CreateCase> ActivationRuleCreateCaseObjectAsync(this Context context,
            EntityAnalysisModelActivationRule evaluateActivationRule,
            bool suppressed, CacheService cacheService)
        {
            if (!evaluateActivationRule.EnableCaseWorkflow || suppressed)
            {
                return null;
            }

            if (context.Environment.AppSettings("ActivationRuleIdempotency").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (!await cacheService.CacheActivationCaseIdempotencyRepository.CheckAndClaimIdempotencyAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                        context.EntityAnalysisModel.Instance.Guid,
                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid,
                        evaluateActivationRule.Guid))
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid}, model {context.EntityAnalysisModel.Instance.Id} and activation rule guid {evaluateActivationRule.Guid} has failed case idempotency check.");
                    }

                    return null;
                }
            }
            else
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid}, model {context.EntityAnalysisModel.Instance.Id} and activation rule guid {evaluateActivationRule.Guid} won't be checked for ActivationRuleIdempotency.");
                }
            }

            var createCase = new CreateCase
            {
                TenantRegistryId = context.EntityAnalysisModel.Instance.TenantRegistryId,
                EntityAnalysisModelInstanceEntryGuid = context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid,
                CaseWorkflowGuid = evaluateActivationRule.CaseWorkflowGuid,
                CaseWorkflowStatusGuid = evaluateActivationRule.CaseWorkflowStatusGuid
            };

            if (evaluateActivationRule.BypassSuspendSample > context.Random.NextDouble())
            {
                createCase.SuspendBypass = true;
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has been selected for bypass.");
                }

                switch (evaluateActivationRule.BypassSuspendInterval)
                {
                    case 'n':
                        createCase.SuspendBypassDate =
                            DateTime.UtcNow.AddMinutes(evaluateActivationRule.BypassSuspendValue);

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has a bypass interval of n to create a date of {createCase.SuspendBypassDate}.");
                        }

                        break;
                    case 'h':
                        createCase.SuspendBypassDate =
                            DateTime.UtcNow.AddHours(evaluateActivationRule.BypassSuspendValue);

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has a bypass interval of h to create a date of {createCase.SuspendBypassDate}.");
                        }

                        break;
                    case 'd':
                        createCase.SuspendBypassDate =
                            DateTime.UtcNow.AddDays(evaluateActivationRule.BypassSuspendValue);

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has a bypass interval of d to create a date of {createCase.SuspendBypassDate}.");
                        }

                        break;
                    case 'm':
                        createCase.SuspendBypassDate =
                            DateTime.UtcNow.AddMonths(evaluateActivationRule.BypassSuspendValue);

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has a bypass interval of m to create a date of {createCase.SuspendBypassDate}.");
                        }

                        break;
                }
            }
            else
            {
                createCase.SuspendBypass = false;

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has been selected for open.");
                }

                createCase.SuspendBypassDate = DateTime.UtcNow;
            }

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    String.IsNullOrEmpty(evaluateActivationRule.CaseKey)
                        ? $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} which is an entry foreign key."
                        : $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} which is not a entry foreign key.");
            }

            if (evaluateActivationRule.CaseKey != null &&
                context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(evaluateActivationRule.CaseKey))
            {
                createCase.CaseKey = evaluateActivationRule.CaseKey;
                createCase.CaseKeyValue = context.EntityAnalysisModelInstanceEntryPayload.Payload[evaluateActivationRule.CaseKey].ToString();

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} and case key value is {context.EntityAnalysisModelInstanceEntryPayload.Payload[evaluateActivationRule.CaseKey]}.");
                }
            }
            else
            {
                createCase.CaseKeyValue = context.EntityAnalysisModelInstanceEntryPayload.EntityInstanceEntryId;
                createCase.CaseKey = null;

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} does not have a value,  has fallen back to the entity id of {context.EntityAnalysisModelInstanceEntryPayload.EntityInstanceEntryId}.");
                }
            }

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has flagged that a case needs to be created for case workflow id {createCase.CaseWorkflowGuid} and case status id {createCase.CaseWorkflowStatusGuid}.  The case will be queued later after the archive XML has been created.");
            }

            return createCase;
        }
    }
}
