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

namespace Jube.App.Code
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class SendHttpEndpoint
    {
        public void Send(string httpEndpoint, byte httpEndpointTypeId, Dictionary<string, string> values)
        {
            if (String.IsNullOrEmpty(httpEndpoint))
            {
                return;
            }

            var tokenization = new Tokenisation();
            var urlTokens = tokenization.ReturnTokens(httpEndpoint);
            var replacedUrl = httpEndpoint;
            foreach (var token in urlTokens)
            {
                if (!values.ContainsKey(token))
                {
                    continue;
                }

                var replaceToken = $"[@{token}@]";
                replacedUrl = replacedUrl.Replace(replaceToken, values[token]);
            }

            if (httpEndpointTypeId == 1)
            {
                var stringContent = new StringContent(
                    JsonConvert.SerializeObject(values),
                    Encoding.UTF8,
                    "application/json");

                var client = new HttpClient();
                var response = client.PostAsync(replacedUrl, stringContent);
                var valueTask = Task.Run(() => response.Result.Content.ReadAsStringAsync());
                valueTask.Wait();
            }
            else
            {
                var client = new HttpClient();
                var response = client.GetAsync(replacedUrl);

                Task.Run(() => response.Result.Content.ReadAsStringAsync());
            }
        }
    }
}
