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
    using System.Net;
    using System.Threading.Tasks;
    using Dictionary;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using RabbitMQ.Client;

    public static class WriteResponseJsonAndQueueAsynchronousResponseMessageExtension
    {
        public static async Task WriteResponseJsonAndQueueAsynchronousResponseMessageAsync(this Context context)
        {
            context.EntityAnalysisModelInstanceEntryPayload.ArchiveJson = BuildJsonResponses.BuildFullJson(context.EntityAnalysisModelInstanceEntryPayload, context.EntityAnalysisModel.JsonSerializationHelper.ArchiveJsonSerializer);

            await context.EntityAnalysisModel.Services.CacheService.CacheWalRepository.InsertAsync(context.EntityAnalysisModelInstanceEntryPayload.TenantRegistryId, context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelGuid,
                context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid, Dns.GetHostName(),
                context.EntityAnalysisModelInstanceEntryPayload.ArchiveJson);

            if (context.Environment.AppSettings("PartialResponseMessageSerialisation").Equals("True", StringComparison.CurrentCultureIgnoreCase))
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"HTTP Handler Entity: GUID payload {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} model id is {context.EntityAnalysisModel.Instance.Id} has partial serialised response environment variable.");
                }

                context.EntityAnalysisModelInstanceEntryPayload.ResponseJson = BuildJsonResponses.BuildPartialResponsePayloadJson(context, context.EntityAnalysisModel.JsonSerializationHelper.ArchiveJsonSerializer);
            }
            else
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"HTTP Handler Entity: GUID payload {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} model id is {context.EntityAnalysisModel.Instance.Id} has partial serialised response environment variable false and will serialise full response.");
                }
            }

            if (context.Environment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                PublishToAmqp(context);
            }
            else
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"HTTP Handler Entity: GUID payload {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} model id is {context.EntityAnalysisModel.Instance.Id} does not have AMQP configured to dispatch messages to an exchange.");
                }
            }

            if (context.Async)
            {
                await PublishCallbackViaPostgresAsync(context).ConfigureAwait(false);
            }

        }

        private static async Task PublishCallbackViaPostgresAsync(Context context)
        {
            await context.EntityAnalysisModel.Services.CacheService.CacheCallbackPublishSubscribe.PublishAsync(context.EntityAnalysisModelInstanceEntryPayload.ResponseJson.Length > 0 ? context.EntityAnalysisModelInstanceEntryPayload.ResponseJson : context.EntityAnalysisModelInstanceEntryPayload.ArchiveJson,
                context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid).ConfigureAwait(false);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"HTTP Handler Entity: GUID payload {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} model id is {context.EntityAnalysisModel.Instance.Id} will store the callback in the database.");
            }
        }

        private static void PublishToAmqp(Context context)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"HTTP Handler Entity: GUID payload {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} model id is {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelId} is about to publish the response to the Outbound Exchange.");
            }

            var props = context.EntityAnalysisModel.Services.RabbitMqChannel.CreateBasicProperties();
            props.Headers = new PooledDictionary<string, object>();

            context.EntityAnalysisModel.Services.RabbitMqChannel.BasicPublish("jubeOutbound", "", props, context.EntityAnalysisModelInstanceEntryPayload.ResponseJson.Length > 0 ? context.EntityAnalysisModelInstanceEntryPayload.ResponseJson : context.EntityAnalysisModelInstanceEntryPayload.ArchiveJson);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"HTTP Handler Entity: GUID payload {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} model id is {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelId} has published the response to the Outbound Exchange.");
            }
        }
    }
}
