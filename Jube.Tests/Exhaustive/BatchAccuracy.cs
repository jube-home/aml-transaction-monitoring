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

namespace Jube.Test.Exhaustive
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using FluentAssertions;
    using Newtonsoft.Json;
    using Xunit;
    using Xunit.Abstractions;

    public class Accuracy(ITestOutputHelper testOutputHelper)
    {

        [Theory]
        [InlineData("https://localhost:5001/api/Invoke/ExhaustiveSearchInstance/111c17f5-01d8-46a6-a940-273660a93a17",
            10000000, 0.8)]
        public async void BatchAccuracy(string uriString, int httpTimeout, double passThreshold)
        {
            try
            {
                var uri = new Uri(uriString);

                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.MaxConnectionsPerServer = 30;
                httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

                using var client = new HttpClient(httpClientHandler);
                client.Timeout = TimeSpan.FromMilliseconds(httpTimeout);

                await using var fileStream = File.OpenRead("Exhaustive/Mock.csv");
                using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true);

                var i = 0;
                var fields = new List<string>();
                var outcomes = new List<int>();
                var countCorrect = 0;
                while (await streamReader.ReadLineAsync() is {} line)
                {
                    if (i == 0)
                    {
                        var splits = line.Split(",");
                        fields.AddRange(splits.Select(split => "Abstraction." + split));
                    }
                    else
                    {
                        var splits = line.Split(",");
                        var model = new Dictionary<string, double>();
                        for (var j = 0; j < fields.Count; j++)
                        {
                            model.Add(fields[j], Double.Parse(splits[j]));
                        }

                        var stringContent = new StringContent(
                            JsonConvert.SerializeObject(model),
                            Encoding.UTF8,
                            "application/json");

                        var request = new HttpRequestMessage
                        {
                            Method = HttpMethod.Post,
                            RequestUri = uri,
                            Content = stringContent
                        };

                        var task = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);

                        var recallString = await task.Content.ReadAsStringAsync();
                        var recall = Double.Parse(recallString);
                        outcomes.Add(recall > 0.5 ? 1 : 0);
                        if (outcomes.Last() == (int)model["Abstraction.Dependent"])
                        {
                            countCorrect += 1;
                        }
                    }

                    i += 1;
                }

                var percentageCorrect = (double)countCorrect / i;
                percentageCorrect.Should().BeGreaterThan(passThreshold);
            }
            catch (Exception ex)
            {
                testOutputHelper.WriteLine(ex.ToString());
            }
        }
    }
}
