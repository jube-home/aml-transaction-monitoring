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
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Poco;
    using Newtonsoft.Json;
    using RabbitMQ.Client;
    using StackExchange.Redis;
    using EntityAnalysisModelActivationRule=EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelActivationRule;

    public static class ActivationRuleActivationWatcherExtensions
    {
        public static async Task ActivationRuleActivationWatcherAsync(this Context context, EntityAnalysisModelActivationRule evaluateActivationRule,
            bool suppressed, IModel rabbitMqChannel)
        {
            if (!evaluateActivationRule.SendToActivationWatcher || suppressed || context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue ||
                !context.EntityAnalysisModel.Flags.EnableActivationWatcher)
            {
                return;
            }

            try
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} the current activation watch count is {context.EntityAnalysisModel.Counters.ActivationWatcherCount} which will be tested against the threshold {context.EntityAnalysisModel.Counters.MaxActivationWatcherThreshold}.");
                }

                if (!(context.EntityAnalysisModel.Counters.ActivationWatcherCount <
                      context.EntityAnalysisModel.Counters.MaxActivationWatcherThreshold) ||
                    !(context.EntityAnalysisModel.Counters.ActivationWatcherSample >= context.Random.NextDouble()))
                {
                    return;
                }

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} the current activation watch count is {context.EntityAnalysisModel.Counters.ActivationWatcherCount} which will be tested against the threshold {context.EntityAnalysisModel.Counters.MaxActivationWatcherThreshold} and selected via random sampling.");
                }

                var activationWatcher = new ActivationWatcher
                {
                    BackColor = evaluateActivationRule.ResponseElevationBackColor,
                    ForeColor = evaluateActivationRule.ResponseElevationForeColor,
                    ResponseElevation = evaluateActivationRule.ResponseElevation,
                    ResponseElevationContent = evaluateActivationRule.ResponseElevationContent,
                    ActivationRuleSummary = evaluateActivationRule.Name,
                    TenantRegistryId = context.EntityAnalysisModel.Instance.TenantRegistryId,
                    CreatedDate = DateTime.UtcNow,
                    Latitude = GetLatitude(context),
                    Longitude = GetLongitude(context),
                    Key = "",
                    KeyValue = ""
                };

                if (context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(evaluateActivationRule
                        .ResponseElevationKey))
                {
                    activationWatcher.Key = evaluateActivationRule.ResponseElevationKey;
                    activationWatcher.KeyValue =
                        context.EntityAnalysisModelInstanceEntryPayload.Payload[evaluateActivationRule
                            .ResponseElevationKey].AsString();

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} found key of {activationWatcher.Key} and key value of {activationWatcher.KeyValue}.");
                    }
                }
                else
                {
                    activationWatcher.Key = evaluateActivationRule.ResponseElevationKey;
                    activationWatcher.KeyValue = "Missing";

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} fallen back to key of {activationWatcher.Key} and key value of {activationWatcher.KeyValue}.");
                    }
                }

                var jsonString = JsonConvert.SerializeObject(activationWatcher, context.EntityAnalysisModel.JsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

                var bodyBytes = Encoding.UTF8.GetBytes(jsonString);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has serialized the Activation Watcher Object to be dispatched.");
                }

                if (context.Environment.AppSettings("ActivationWatcherAllowPersist")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Enqueue(activationWatcher);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} replay is  allowed so it has been sent to the database. {context.EntityAnalysisModel.Counters.ActivationWatcherCount}.");
                    }
                }
                else
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} replay is not allowed so it has not been sent to the database. {context.EntityAnalysisModel.Counters.ActivationWatcherCount}.");
                    }
                }

                if (context.Environment.AppSettings("StreamingActivationWatcher")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    await context.EntityAnalysisModel.Services.CacheService.ResilientRedisResilientRedisDatabase.PublishAsync($"ActivationWatcher:{context.EntityAnalysisModel.Instance.TenantRegistryId}", bodyBytes, CommandFlags.FireAndForget);
                    Interlocked.Increment(ref context.EntityAnalysisModel.Counters.ActivationWatcherCount);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} streaming is allowed so it has been sent to the database as a notification in the activation channel. {context.EntityAnalysisModel.Counters.ActivationWatcherCount}.");
                    }
                }
                else
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} streaming is not allowed so it has not been sent to the database as a notification in the activation channel. {context.EntityAnalysisModel.Counters.ActivationWatcherCount}.");
                    }
                }

                if (context.Environment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    var properties = rabbitMqChannel.CreateBasicProperties();

                    rabbitMqChannel.BasicPublish("jubeActivations", "", properties, bodyBytes);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} AMQP is  allowed so it has been published to the RabbitMQ.");
                    }
                }
                else
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} AMQP is not allowed, so publish has been stepped over.");
                    }
                }

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has sent a message to the watcher as {jsonString} the activation watcher counter has been incremented and is currently {context.EntityAnalysisModel.Counters.ActivationWatcherCount}.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} there has been an error in Activation Watcher processing {ex}.");
                }
            }
        }

        private static double GetLongitude(Context context)
        {
            var longitudeValue = 0d;
            var longitudeFieldName = context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.FirstOrDefault(f => f.DataTypeId == 7)?.Name;

            if (String.IsNullOrEmpty(longitudeFieldName))
            {
                foreach (var entityAnalysisModelInlineScriptPropertyAttribute in
                         context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts
                             .SelectMany(entityAnalysisModelInlineScript => entityAnalysisModelInlineScript.EntityAnalysisModelInlineScriptPropertyAttributes
                                 .Where(entityAnalysisModelInlineScriptPropertyAttribute => entityAnalysisModelInlineScriptPropertyAttribute.Value.Latitude)))
                {
                    longitudeFieldName = entityAnalysisModelInlineScriptPropertyAttribute.Key;

                    if (String.IsNullOrEmpty(longitudeFieldName))
                    {
                        return longitudeValue;
                    }
                }

                return longitudeValue;
            }

            if (context.EntityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(longitudeFieldName, out var value))
            {
                longitudeValue = value;
            }

            return longitudeValue;
        }

        private static double GetLatitude(Context context)
        {
            var latitudeValue = 0d;
            var latitudeFieldName = context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.FirstOrDefault(f => f.DataTypeId == 6)?.Name;

            if (String.IsNullOrEmpty(latitudeFieldName))
            {
                foreach (var entityAnalysisModelInlineScriptPropertyAttribute in
                         context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts
                             .SelectMany(entityAnalysisModelInlineScript => entityAnalysisModelInlineScript.EntityAnalysisModelInlineScriptPropertyAttributes
                                 .Where(entityAnalysisModelInlineScriptPropertyAttribute => entityAnalysisModelInlineScriptPropertyAttribute.Value.Latitude)))
                {
                    latitudeFieldName = entityAnalysisModelInlineScriptPropertyAttribute.Key;

                    if (String.IsNullOrEmpty(latitudeFieldName))
                    {
                        return latitudeValue;
                    }
                }

                return latitudeValue;
            }

            if (context.EntityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(latitudeFieldName, out var value))
            {
                latitudeValue = value;
            }

            return latitudeValue;
        }
    }
}
