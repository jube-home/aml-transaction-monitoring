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

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;

namespace Jube.Engine.Model;

public class EntityAnalysisModelHttpAdaptation
{
    private readonly HttpClient _httpClient;
    private Uri _uri;

    public EntityAnalysisModelHttpAdaptation(int maxConnections, bool validateSsl, int timeout)
    {
        var httpClientHandler = new HttpClientHandler();
        if (!validateSsl) httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        httpClientHandler.MaxConnectionsPerServer = maxConnections;

        _httpClient = new HttpClient(httpClientHandler);
        _httpClient.Timeout = TimeSpan.FromMilliseconds(timeout);
    }

    public int Id { get; init; }
    public string Name { get; set; }
    public bool ResponsePayload { get; set; }
    public bool ReportTable { get; set; }

    public string HttpEndpoint
    {
        get => _uri.ToString();
        set => _uri = new Uri(value);
    }

    public async Task<double> Post(Dictionary<string, object> jsonForPlumberAsync, ILog log)
    {
        log.Info($"R Plumber Hook: Is about to send to {_uri}.");

        var stringContent = new StringContent(
            JsonConvert.SerializeObject(jsonForPlumberAsync),
            Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = _uri,
            Content = stringContent
        };

        var task = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

        log.Info(
            $"R Plumber Hook: Has received data from {_uri} with status {task.StatusCode}.");

        var valueString = await task.Content.ReadAsStringAsync().ConfigureAwait(false);

        log.Info(
            $"R Plumber Hook: Has received data from {_uri} with payload {valueString}. The JSON decoration will now be removed.");

        valueString = valueString.Replace("[", "");
        valueString = valueString.Replace("]", "");
        var valueDouble = double.Parse(valueString);

        log.Info($"R Plumber Hook: Is returning {valueDouble}.");

        return valueDouble;
    }
}