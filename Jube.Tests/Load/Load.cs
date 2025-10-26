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

namespace Jube.Test.Load
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class Load
    {
        private readonly ConcurrentQueue<(double, long)> responseTimes = new ConcurrentQueue<(double, long)>();
        private readonly Stopwatch swTotal = new Stopwatch();
        private int requests;
        private bool stop;
        private Thread? writerThreadTps;

        [Theory]
        [InlineData("http://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc",
            10000, 100000000, false, 10, 10)]
        public async Task LoadTest(string uriString, int httpTimeout, long iteration, bool async,
            int maxConnectionsPerServer, int timeDriftSeconds)
        {
            var random = new Random();
            var referenceDate = DateTime.Now.AddYears(-10);
            var uri = async ? new Uri(uriString + "/async") : new Uri(uriString);
            var stringTemplate = Helpers.ReadFileContents("Load/Mock.json");

            var clientCount = 6;
            var baseIterationsPerClient = iteration / clientCount;
            var remainder = iteration % clientCount;

            writerThreadTps = new Thread(WriteTpsEstimates);
            writerThreadTps.Start();
            swTotal.Start();

            var tasks = new List<Task>();
            for (var clientIndex = 0; clientIndex < clientCount; clientIndex++)
            {
                var iterationsForThisClient = baseIterationsPerClient + (clientIndex == clientCount - 1 ? remainder : 0);
                var startingIteration = clientIndex * baseIterationsPerClient;

                tasks.Add(Task.Run(async () =>
                {
                    var clientHandler = new HttpClientHandler
                    {
                        MaxConnectionsPerServer = maxConnectionsPerServer,
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                    };

                    using var client = new HttpClient(clientHandler);
                    client.Timeout = TimeSpan.FromMilliseconds(httpTimeout);

                    var myReferenceDate = referenceDate.AddSeconds(startingIteration * timeDriftSeconds);

                    for (var i = 0; i < iterationsForThisClient; i++)
                    {
                        var globalIteration = startingIteration + i;
                        var replacements = new Dictionary<string, string>
                        {
                            ["[@AccountId@]"] = random.NextInt64(1, 100000).ToString(),
                            ["[@TxnId@]"] = globalIteration.ToString(),
                            //["[@TxnDateTime@]"] = myReferenceDate.AddSeconds(timeDriftSeconds).ToString("o")
                            ["[@TxnDateTime@]"] = DateTime.Now.ToString("o")
                        };

                        var payload = stringTemplate;
                        foreach (var kvp in replacements)
                        {
                            payload = payload.Replace(kvp.Key, kvp.Value);
                        }

                        myReferenceDate = myReferenceDate.AddSeconds(timeDriftSeconds);

                        await SendToJubeAndAwaitResponse(payload, uri, client, responseTimes, swTotal);

                        Interlocked.Increment(ref requests);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            stop = true;
        }

        private static async Task SendToJubeAndAwaitResponse(string stringReplaced, Uri uri, HttpClient client,
            ConcurrentQueue<(double, long)> responseTimes, Stopwatch swTotal)
        {
            var stringContent = new StringContent(
                stringReplaced,
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = uri,
                Content = stringContent
            };

            var sw = new Stopwatch();
            sw.Start();
            await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            sw.Stop();
            responseTimes.Enqueue((swTotal.Elapsed.TotalSeconds, (int)(sw.ElapsedTicks * 1000000 / Stopwatch.Frequency)));
        }

        private async void WriteTpsEstimates()
        {
            try
            {
                var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var outputFileTpsSnapshot = new StreamWriter(Path.Combine(docPath, "WriteLinesTpsSnapshot.txt"));
                outputFileTpsSnapshot.AutoFlush = true;

                while (!stop)
                {
                    Thread.Sleep(1000);
                    await outputFileTpsSnapshot.WriteLineAsync(
                        $"{Math.Round(swTotal.Elapsed.TotalSeconds)},{requests}");
                    requests = 0;
                }
            }
            catch (Exception)
            {
                //Not implemented
            }
        }
    }
}
