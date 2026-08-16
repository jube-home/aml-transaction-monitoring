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
    using System.Text;
    using System.Threading.Tasks;
    using Cache;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Models;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload.Extensions;
    using Newtonsoft.Json;
    using RabbitMQ.Client;

    public static class ActivationRuleNotificationExtensions
    {
        public static async Task ActivationRuleNotificationAsync(this Context context, EntityAnalysisModelActivationRule evaluateActivationRule,
            bool suppressed, IModel rabbitMqChannel, CacheService cacheService)
        {
            if (context.Environment.AppSettings("EnableNotification").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (suppressed || !evaluateActivationRule.EnableNotification || context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
                {
                    return;
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
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid}, model {context.EntityAnalysisModel.Instance.Id} and activation rule guid {evaluateActivationRule.Guid} has failed notification idempotency check.");
                        }

                        return;
                    }
                }
                else
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid}, model {context.EntityAnalysisModel.Instance.Id} and activation rule guid {evaluateActivationRule.Guid} won't check ActivationRuleIdempotency.");
                    }
                }

                var notification = new Notification
                {
                    NotificationBody = context.EntityAnalysisModelInstanceEntryPayload.ReplaceTokens(evaluateActivationRule.NotificationBody),
                    NotificationDestination = context.EntityAnalysisModelInstanceEntryPayload.ReplaceTokens(evaluateActivationRule.NotificationDestination),
                    NotificationSubject = context.EntityAnalysisModelInstanceEntryPayload.ReplaceTokens(evaluateActivationRule.NotificationSubject),
                    NotificationTypeId = evaluateActivationRule.NotificationTypeId
                };

                if (context.Environment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    var jsonString = JsonConvert.SerializeObject(notification, context.EntityAnalysisModel.JsonSerializationHelper.DefaultJsonSerializerSettingsSettings);
                    var bodyBytes = Encoding.UTF8.GetBytes(jsonString);
                    rabbitMqChannel.BasicPublish("", "jubeNotifications", null, bodyBytes);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has sent a message to the notification dispatcher as {jsonString}.");
                    }
                }
                else
                {
                    context.EntityAnalysisModel.ConcurrentQueues.PendingNotifications.Enqueue(notification);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has not sent a message to the internal notification dispatcher because AMQP is not enabled.");
                    }
                }
            }
            else
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has not sent a message as notification disabled.");
                }
            }
        }
    }
}
