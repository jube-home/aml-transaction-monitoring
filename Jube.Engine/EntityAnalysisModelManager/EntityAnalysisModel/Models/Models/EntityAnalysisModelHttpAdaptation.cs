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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using log4net;
    using Newtonsoft.Json;
    using EntityAnalysisModelInstanceEntryPayload=EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryPayload;

    public class EntityAnalysisModelHttpAdaptation
    {
        private readonly HttpClient httpClient;
        private Uri uri;

        public EntityAnalysisModelHttpAdaptation(int maxConnections, bool validateSsl, int timeout)
        {
            var httpClientHandler = new HttpClientHandler();
            if (!validateSsl)
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            }

            httpClientHandler.MaxConnectionsPerServer = maxConnections;

            httpClient = new HttpClient(httpClientHandler);
            httpClient.Timeout = TimeSpan.FromMilliseconds(timeout);
        }

        public int Id { get; init; }
        public string Name { get; set; }
        public bool ResponsePayload { get; set; }
        public bool ReportTable { get; set; }
        public double Priority { get; set; }

        public string HttpEndpoint
        {
            get
            {
                return uri.ToString();
            }
            set
            {
                uri = new Uri(value);
            }
        }

        public async Task<string> PostAsync(EntityAnalysisModelInstanceEntryPayload payload, JsonSerializerSettings jsonSerializerSettings, ILog log)
        {
            if (log.IsInfoEnabled)
            {
                log.Info($"R Plumber Hook: Is about to send to {uri}.");
            }

            var stringContent = new StringContent(
                JsonConvert.SerializeObject(payload, jsonSerializerSettings),
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = uri,
                Content = stringContent
            };

            var task = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"R Plumber Hook: Has received data from {uri} with status {task.StatusCode}.");
            }

            var valueString = await task.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"R Plumber Hook: Has received data from {uri} with payload {valueString}. This is returned unparsed, as the HTTP Adaptation Protocol response shape (bare number or Adaptation object) is decided by the caller.");
            }

            return valueString;
        }
    }
}
