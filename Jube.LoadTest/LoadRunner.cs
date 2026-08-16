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

namespace Jube.LoadTest
{
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Net;
    using System.Text;
    using System.Threading.Channels;

    public sealed class LoadRunner(LoadSettings settings)
    {
        private static readonly TimeSpan SampleInterval = TimeSpan.FromMinutes(1);
        private readonly ConcurrentBag<double> allResponseTimesMs = new ConcurrentBag<double>();
        private readonly ContainerStatsSampler containerStatsSampler = new ContainerStatsSampler(settings.ContainerRoles, settings.ContainerNamePatternsAreRegex);
        private readonly HostStatsSampler hostStatsSampler = new HostStatsSampler();

        private readonly PostgresStatsSampler? postgresStatsSampler = settings.PostgresConnectionString is { Length: > 0 } pgConnectionString
            ? new PostgresStatsSampler(pgConnectionString)
            : null;

        private readonly RedisStatsSampler? redisStatsSampler = settings.RedisConnectionString is { Length: > 0 } redisConnectionString
            ? new RedisStatsSampler(redisConnectionString)
            : null;

        private readonly Channel<ResponseSample> responseSampleChannel = Channel.CreateUnbounded<ResponseSample>();

        private readonly Lock statsLock = new Lock();
        private readonly Stopwatch swTotal = new Stopwatch();
        private readonly Lock windowLock = new Lock();

        private int errors;
        private int inFlightCount;
        private IReadOnlyDictionary<string, (double CpuPercent, double MemMiB)>? latestContainerStats;
        private (double CpuPercent, double MemUsedMiB)? latestHostStats;

        private (double Connections, double TxnPerSec, double CacheHitPct, double TempMiBPerSec,
            double ReplicationLagSeconds, double ReplicaCount)? latestPostgresStats;

        private (double ConnectedClients, double UsedMemMiB, double OpsPerSec, double HitRatePct)? latestRedisStats;
        private int requests;

        private int throttledTicks;
        private List<double> windowResponseTimesMs = [];

        public async Task RunAsync()
        {
            long txnId = 0;
            long simulatedElapsedMs = 0;
            var random = new Random();
            var referenceDate = DateTime.Now.AddYears(-10);

            var stringTemplate = await File.ReadAllTextAsync(settings.MockTemplatePath);

            var keyGenerator = settings.UseUniformKeyDistribution
                ? null
                : new ZipfianKeyGenerator(settings.KeyPoolSize, settings.KeySkew, random);

            PrintKeyDistributionSummary(keyGenerator);

            using var clientHandler = new HttpClientHandler();
            clientHandler.MaxConnectionsPerServer = settings.MaxConnectionsPerServer;
            clientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

            using var client = new HttpClient(clientHandler);
            client.Timeout = TimeSpan.FromSeconds(settings.HttpTimeoutSeconds);
            client.DefaultRequestVersion = HttpVersion.Version11;
            client.DefaultRequestHeaders.Add("X-API-KEY", settings.ApiKey);

            using var durationCts = new CancellationTokenSource(TimeSpan.FromSeconds(settings.DurationSeconds));
            using var writerCts = new CancellationTokenSource();
            using var concurrencyGate = new SemaphoreSlim(settings.MaxConcurrentInFlight, settings.MaxConcurrentInFlight);

            swTotal.Start();

            var writerToken = writerCts.Token;

            var writerTask = Task.Run(async () =>
            {
                try { await WriteTpsEstimatesAsync(writerToken); }
                catch (Exception ex) { await Console.Error.WriteLineAsync(ex.ToString()); }
            }, writerToken);

            var statsSamplerTask = Task.Run(async () =>
            {
                try { await SampleStatsAsync(writerToken); }
                catch (Exception ex) { await Console.Error.WriteLineAsync(ex.ToString()); }
            }, writerToken);

            var responseSampleWriterTask = Task.Run(async () =>
            {
                try { await WriteResponseSampleAsync(); }
                catch (Exception ex) { await Console.Error.WriteLineAsync(ex.ToString()); }
            });

            var inFlight = new List<Task>();
            var intervalTicks = TimeSpan.FromSeconds(1.0 / settings.TargetRequestsPerSecond);
            using var timer = new PeriodicTimer(intervalTicks);

            try
            {
                while (await timer.WaitForNextTickAsync(durationCts.Token))
                {
                    var accountId = keyGenerator?.Next() ?? random.NextInt64(1, settings.KeyPoolSize + 1);
                    var ip = $"{random.Next(1, 255)}.{random.Next(0, 255)}.{random.Next(0, 255)}.{random.Next(1, 255)}";

                    var deviceIdBytes = new byte[8];
                    random.NextBytes(deviceIdBytes);
                    var deviceId = Convert.ToHexString(deviceIdBytes);
                    var randomProbability = Math.Round(random.NextDouble(), 3);

                    var currentTxnId = Interlocked.Increment(ref txnId);

                    var replacements = new Dictionary<string, string>
                    {
                        ["[@AccountId@]"] = accountId.ToString(),
                        ["[@TxnId@]"] = currentTxnId.ToString(),
                        ["[@TxnDateTime@]"] = referenceDate
                            .AddMilliseconds(Interlocked.Add(ref simulatedElapsedMs, settings.TimeDriftMs))
                            .ToString("o"),
                        ["[@IP@]"] = ip,
                        ["[@DeviceId@]"] = deviceId,
                        ["[@RandomProbability@]"] = randomProbability.ToString("F3")
                    };

                    var payload = replacements.Aggregate(stringTemplate,
                        (current, kvp) => current.Replace(kvp.Key, kvp.Value));

                    if (!await concurrencyGate.WaitAsync(0, durationCts.Token).ConfigureAwait(false))
                    {
                        Interlocked.Increment(ref throttledTicks);
                        await concurrencyGate.WaitAsync(durationCts.Token).ConfigureAwait(false);
                    }

                    Interlocked.Increment(ref inFlightCount);
                    inFlight.Add(DispatchAsync(payload, settings.Uri, client, concurrencyGate, currentTxnId, accountId));
                    inFlight.RemoveAll(t => t.IsCompleted);
                }
            }
            catch (OperationCanceledException)
            {
                // duration elapsed - fall through to drain in-flight requests
            }

            await Task.WhenAll(inFlight);

            responseSampleChannel.Writer.Complete();

            await writerCts.CancelAsync();
            await Task.WhenAll(writerTask, statsSamplerTask, responseSampleWriterTask);

            if (postgresStatsSampler != null)
            {
                await postgresStatsSampler.DisposeAsync();
            }

            redisStatsSampler?.Dispose();

            WriteFinalSummary(txnId);
        }

        private void PrintKeyDistributionSummary(ZipfianKeyGenerator? keyGenerator)
        {
            if (keyGenerator == null)
            {
                Console.WriteLine($"Key distribution: uniform-random over pool={settings.KeyPoolSize:N0} keys.");
                return;
            }

            const double topFraction = 0.01;
            var topShare = ZipfianKeyGenerator.EstimateTopFractionShare(settings.KeyPoolSize, settings.KeySkew, topFraction);
            var topCount = Math.Max(1, (long)(settings.KeyPoolSize * topFraction));

            Console.WriteLine(
                $"Key distribution: Zipfian s={settings.KeySkew:F2}, pool={settings.KeyPoolSize:N0} keys; " +
                $"top {topFraction * 100:F0}% of keys (~{topCount:N0}) carry ~{topShare * 100:F1}% of volume.");
        }

        private async Task DispatchAsync(string payload, Uri uri, HttpClient client, SemaphoreSlim concurrencyGate,
            long txnId, long accountId)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await SendToJubeAndAwaitResponseAsync(payload, uri, client);
                sw.Stop();

                var elapsedMs = sw.Elapsed.TotalMilliseconds;
                Interlocked.Increment(ref requests);
                allResponseTimesMs.Add(elapsedMs);
                lock (windowLock)
                {
                    windowResponseTimesMs.Add(elapsedMs);
                }

                if (settings.ResponseSampleRate > 0 && Random.Shared.NextDouble() < settings.ResponseSampleRate)
                {
                    responseSampleChannel.Writer.TryWrite(
                        new ResponseSample(DateTime.Now, txnId, accountId, elapsedMs));
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref errors);
                await Console.Error.WriteLineAsync(ex.ToString());
            }
            finally
            {
                Interlocked.Decrement(ref inFlightCount);
                concurrencyGate.Release();
            }
        }

        private static async Task SendToJubeAndAwaitResponseAsync(string payload, Uri uri, HttpClient client)
        {
            var stringContent = new StringContent(
                payload,
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = uri,
                Content = stringContent
            };

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Request failed with status {response.StatusCode}");
            }
        }

        private async Task WriteTpsEstimatesAsync(CancellationToken token = default)
        {
            await using var outputFileTpsSnapshot = new StreamWriter(settings.OutputPath);
            outputFileTpsSnapshot.AutoFlush = true;

            await outputFileTpsSnapshot.WriteLineAsync(
                "Timestamp,ElapsedSeconds,RequestsCompleted,ErrorsCompleted,InFlight,ThrottledTicks,MinMs,MaxMs,MedianMs,AvgMs," +
                BuildContainerColumnsHeader() + ",Host_CpuPct,Host_MemUsedMiB," +
                "Postgres_Connections,Postgres_TxnPerSec,Postgres_CacheHitPct,Postgres_TempMiBPerSec," +
                "Postgres_ReplicationLagSeconds,Postgres_ReplicaCount," +
                "Redis_ConnectedClients,Redis_UsedMemMiB,Redis_OpsPerSec,Redis_HitRatePct");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(SampleInterval, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                var snapshotRequests = Interlocked.Exchange(ref requests, 0);
                var snapshotErrors = Interlocked.Exchange(ref errors, 0);
                var snapshotThrottled = Interlocked.Exchange(ref throttledTicks, 0);
                var currentInFlight = Interlocked.CompareExchange(ref inFlightCount, 0, 0);
                var stats = SwapWindowAndSummarise();

                IReadOnlyDictionary<string, (double CpuPercent, double MemMiB)>? containerStats;
                (double CpuPercent, double MemUsedMiB)? hostStats;
                (double Connections, double TxnPerSec, double CacheHitPct, double TempMiBPerSec,
                    double ReplicationLagSeconds, double ReplicaCount)? postgresStats;
                (double ConnectedClients, double UsedMemMiB, double OpsPerSec, double HitRatePct)? redisStats;

                lock (statsLock)
                {
                    containerStats = latestContainerStats;
                    hostStats = latestHostStats;
                    postgresStats = latestPostgresStats;
                    redisStats = latestRedisStats;
                }

                await outputFileTpsSnapshot.WriteLineAsync(
                    $"{DateTime.Now:o},{Math.Round(swTotal.Elapsed.TotalSeconds)},{snapshotRequests},{snapshotErrors}," +
                    $"{currentInFlight},{snapshotThrottled}," +
                    $"{stats.min:F1},{stats.max:F1},{stats.median:F1},{stats.avg:F1}," +
                    BuildContainerColumnsRow(containerStats) + "," +
                    BuildHostColumnsRow(hostStats) + "," +
                    BuildPostgresColumnsRow(postgresStats) + "," +
                    BuildRedisColumnsRow(redisStats));
            }
        }

        private async Task SampleStatsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var containerStats = settings.SampleContainerStats
                    ? await containerStatsSampler.SampleAsync(CancellationToken.None)
                    : null;
                var hostStats = settings.SampleHostStats ? hostStatsSampler.Sample() : null;
                var postgresStats = postgresStatsSampler != null
                    ? await postgresStatsSampler.SampleAsync(CancellationToken.None)
                    : null;
                var redisStats = redisStatsSampler != null
                    ? await redisStatsSampler.SampleAsync()
                    : null;

                lock (statsLock)
                {
                    latestContainerStats = containerStats;
                    latestHostStats = hostStats;
                    latestPostgresStats = postgresStats;
                    latestRedisStats = redisStats;
                }

                try
                {
                    await Task.Delay(SampleInterval, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task WriteResponseSampleAsync()
        {
            await using var outputFileResponseSample = new StreamWriter(settings.ResponseSampleOutputPath);
            outputFileResponseSample.AutoFlush = true;

            await outputFileResponseSample.WriteLineAsync("Timestamp,TxnId,AccountId,ResponseMs");

            await foreach (var sample in responseSampleChannel.Reader.ReadAllAsync())
            {
                await outputFileResponseSample.WriteLineAsync(
                    $"{sample.Timestamp:o},{sample.TxnId},{sample.AccountId},{sample.ResponseMs:F1}");
            }
        }

        private string BuildContainerColumnsHeader()
        {
            return String.Join(',', settings.ContainerRoles.SelectMany(role =>
                new[]
                {
                    $"{role.Name}_CpuPct", $"{role.Name}_MemMiB"
                }));
        }

        private string BuildContainerColumnsRow(IReadOnlyDictionary<string, (double CpuPercent, double MemMiB)>? sample)
        {
            return String.Join(',', settings.ContainerRoles.SelectMany(role =>
            {
                if (sample == null || !sample.TryGetValue(role.Name, out var stat))
                {
                    return new[]
                    {
                        "", ""
                    };
                }

                return new[]
                {
                    stat.CpuPercent.ToString("F2"), stat.MemMiB.ToString("F1")
                };
            }));
        }

        private static string BuildHostColumnsRow((double CpuPercent, double MemUsedMiB)? sample)
        {
            return sample is {} stat
                ? $"{stat.CpuPercent:F2},{stat.MemUsedMiB:F1}"
                : ",";
        }

        private static string BuildPostgresColumnsRow(
            (double Connections, double TxnPerSec, double CacheHitPct, double TempMiBPerSec,
                double ReplicationLagSeconds, double ReplicaCount)? sample)
        {
            return sample is {} stat
                ? $"{stat.Connections:F0},{stat.TxnPerSec:F2},{stat.CacheHitPct:F2},{stat.TempMiBPerSec:F3}," +
                  $"{stat.ReplicationLagSeconds:F3},{stat.ReplicaCount:F0}"
                : ",,,,,";
        }

        private static string BuildRedisColumnsRow(
            (double ConnectedClients, double UsedMemMiB, double OpsPerSec, double HitRatePct)? sample)
        {
            return sample is {} stat
                ? $"{stat.ConnectedClients:F0},{stat.UsedMemMiB:F1},{stat.OpsPerSec:F1},{stat.HitRatePct:F2}"
                : ",,,";
        }

        private (double min, double max, double median, double avg) SwapWindowAndSummarise()
        {
            List<double> snapshot;
            lock (windowLock)
            {
                snapshot = windowResponseTimesMs;
                windowResponseTimesMs = [];
            }

            return Summarise(snapshot);
        }

        private static (double min, double max, double median, double avg) Summarise(List<double> values)
        {
            if (values.Count == 0)
            {
                return (0, 0, 0, 0);
            }

            values.Sort();
            var min = values[0];
            var max = values[^1];
            var avg = values.Average();
            var mid = values.Count / 2;
            var median = values.Count % 2 == 0
                ? (values[mid - 1] + values[mid]) / 2.0
                : values[mid];

            return (min, max, median, avg);
        }

        private void WriteFinalSummary(long totalDispatched)
        {
            var allTimes = allResponseTimesMs.ToList();
            var stats = Summarise(allTimes);

            Console.WriteLine();
            Console.WriteLine("Load test summary");
            Console.WriteLine("------------------");
            Console.WriteLine($"Target rate:        {settings.TargetRequestsPerSecond}/s over {settings.DurationSeconds}s");
            Console.WriteLine($"Dispatched:         {totalDispatched}");
            Console.WriteLine($"Succeeded:          {allTimes.Count}");
            Console.WriteLine($"Errors:             {totalDispatched - allTimes.Count}");
            Console.WriteLine($"Response time (ms): min={stats.min:F1} max={stats.max:F1} median={stats.median:F1} avg={stats.avg:F1}");
            Console.WriteLine($"TPS CSV written to: {Path.GetFullPath(settings.OutputPath)}");

            if (settings.ResponseSampleRate > 0)
            {
                Console.WriteLine(
                    $"Response sample ({settings.ResponseSampleRate * 100:F1}%) written to: " +
                    $"{Path.GetFullPath(settings.ResponseSampleOutputPath)}");
            }

            Console.WriteLine();
        }

        private readonly record struct ResponseSample(DateTime Timestamp, long TxnId, long AccountId, double ResponseMs);
    }
}
