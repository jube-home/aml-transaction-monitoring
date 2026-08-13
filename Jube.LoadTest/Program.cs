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
    public static class EntryPoint
    {
        public static async Task<int> Main(string[] args)
        {
            if (args.Contains("-h") || args.Contains("--help"))
            {
                PrintUsage();
                return 0;
            }

            string? uriString = null;
            string? apiKey = null;
            var httpTimeout = 20;
            var targetRequestsPerSecond = 174;
            var durationSeconds = 86400;
            var maxConnectionsPerServer = 500;
            var maxConcurrentInFlight = 500;
            var timeDriftMs = 172;
            var mockTemplatePath = "Mock.json";
            var outputPath = "WriteLinesTpsSnapshot.txt";
            var responseSampleRate = 0.02;
            var responseSampleOutputPath = "ResponseTimeSample.csv";
            var keyPoolSize = 1_000_000L;
            var keySkew = 0.8;
            var useUniformKeyDistribution = false;
            List<ContainerRole>? containerRoles = null;
            var containerNamePatternsAreRegex = false;
            var sampleContainerStats = true;
            var sampleHostStats = true;
            string? postgresConnectionString = null;
            string? redisConnectionString = null;

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-uri":
                        uriString = args[++i];
                        break;
                    case "-apikey":
                        apiKey = args[++i];
                        break;
                    case "-timeout":
                        httpTimeout = Int32.Parse(args[++i]);
                        break;
                    case "-rps":
                        targetRequestsPerSecond = Int32.Parse(args[++i]);
                        break;
                    case "-duration":
                        durationSeconds = Int32.Parse(args[++i]);
                        break;
                    case "-maxconn":
                        maxConnectionsPerServer = Int32.Parse(args[++i]);
                        break;
                    case "-maxinflight":
                        maxConcurrentInFlight = Int32.Parse(args[++i]);
                        break;
                    case "-drift":
                        timeDriftMs = Int32.Parse(args[++i]);
                        break;
                    case "-template":
                        mockTemplatePath = args[++i];
                        break;
                    case "-output":
                        outputPath = args[++i];
                        break;
                    case "-response-sample-rate":
                        responseSampleRate = Double.Parse(args[++i]);
                        break;
                    case "-response-sample-output":
                        responseSampleOutputPath = args[++i];
                        break;
                    case "-key-pool":
                        keyPoolSize = Int64.Parse(args[++i]);
                        break;
                    case "-key-skew":
                        keySkew = Double.Parse(args[++i]);
                        break;
                    case "-uniform-keys":
                        useUniformKeyDistribution = true;
                        break;
                    case "-containers":
                        containerRoles = ParseContainerRoles(args[++i]);
                        break;
                    case "-containers-regex":
                        containerNamePatternsAreRegex = true;
                        break;
                    case "-no-container-stats":
                        sampleContainerStats = false;
                        break;
                    case "-no-host-stats":
                        sampleHostStats = false;
                        break;
                    case "-pgconn":
                        postgresConnectionString = args[++i];
                        break;
                    case "-redisconn":
                        redisConnectionString = args[++i];
                        break;
                }
            }

            if (String.IsNullOrEmpty(uriString))
            {
                await Console.Error.WriteLineAsync("Missing required argument: -uri <endpoint>");
                PrintUsage();
                return 1;
            }

            if (String.IsNullOrEmpty(apiKey))
            {
                await Console.Error.WriteLineAsync("Missing required argument: -apikey <key>");
                PrintUsage();
                return 1;
            }

            var settings = new LoadSettings
            {
                Uri = new Uri(uriString),
                ApiKey = apiKey,
                HttpTimeoutSeconds = httpTimeout,
                TargetRequestsPerSecond = targetRequestsPerSecond,
                DurationSeconds = durationSeconds,
                MaxConnectionsPerServer = maxConnectionsPerServer,
                MaxConcurrentInFlight = maxConcurrentInFlight,
                TimeDriftMs = timeDriftMs,
                MockTemplatePath = mockTemplatePath,
                OutputPath = outputPath,
                ResponseSampleRate = responseSampleRate,
                ResponseSampleOutputPath = responseSampleOutputPath,
                KeyPoolSize = keyPoolSize,
                KeySkew = keySkew,
                UseUniformKeyDistribution = useUniformKeyDistribution,
                SampleContainerStats = sampleContainerStats,
                SampleHostStats = sampleHostStats,
                ContainerNamePatternsAreRegex = containerNamePatternsAreRegex,
                ContainerRoles = containerRoles ?? LoadSettings.DefaultContainerRoles,
                PostgresConnectionString = postgresConnectionString,
                RedisConnectionString = redisConnectionString
            };

            var runner = new LoadRunner(settings);
            await runner.RunAsync();

            return 0;
        }

        private static List<ContainerRole> ParseContainerRoles(string spec)
        {
            var roles = new List<ContainerRole>();

            foreach (var roleSpec in spec.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = roleSpec.Split('=', 2);
                if (parts.Length != 2)
                {
                    throw new FormatException(
                        $"Invalid -containers role '{roleSpec}', expected Name=pattern1,pattern2");
                }

                var patterns = parts[1]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                roles.Add(new ContainerRole(parts[0].Trim(), patterns));
            }

            return roles;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("""
                              Jube.LoadTest - fixed-rate HTTP load generator for the Jube invoke API.

                              Required:
                                -uri <url>              Target invoke endpoint, e.g. http://jube.webapi:5001/api/invoke/EntityAnalysisModel/<id>
                                -apikey <key>           X-API-KEY header value

                              Optional:
                                -timeout <seconds>      HTTP client timeout (default 20)
                                -rps <n>                Target requests per second (default 174 - sustained-load default:
                                                        a month of volume at 500,000 req/day, 15,000,000 total, replayed
                                                        over the default 24h duration). Keep -drift matched if you change
                                                        this or -duration (see -drift below).
                                -duration <seconds>     Run duration (default 86400, i.e. 24 hours)
                                -maxconn <n>            Max HTTP connections per server (default 500). Keep this >=
                                                        -maxinflight or the HTTP connection pool becomes the bottleneck.
                                -maxinflight <n>        Max concurrent in-flight requests (default 500). Achievable
                                                        throughput is capped at roughly this / avg-response-time-seconds;
                                                        if the TPS CSV shows ThrottledTicks tracking completed requests
                                                        with InFlight pinned at this value alongside HttpClient.Timeout
                                                        exceptions, that's the backend's real capacity ceiling, not just
                                                        a low value here - raising this further will not fix it.
                                -drift <ms>             Milliseconds of simulated calendar time each dispatched request
                                                        advances the templated TxnDateTime by, cumulatively (default 172).
                                                        Lets a run's wall-clock -duration be much shorter than the
                                                        calendar span it represents - e.g. replaying a month of activity
                                                        within a 24h run - so time-windowed/velocity rules in the model
                                                        under test see a realistic spread of transaction dates rather
                                                        than everything landing in the same real-time window. Compute as
                                                        simulatedSpanMs / (-rps * -duration) for other targets.
                                -template <path>        Path to the JSON payload template (default Mock.json)
                                -output <path>          Path to the TPS snapshot CSV (default WriteLinesTpsSnapshot.txt)
                                -response-sample-rate <fraction>
                                                        Fraction of successful requests whose individual response time
                                                        (with timestamp, TxnId, AccountId) is written to
                                                        -response-sample-output, for offline histogram/scatter analysis
                                                        that the per-minute TPS CSV is too coarse for (default 0.02, i.e.
                                                        2%). 0 disables it. Only meaningful after the run finishes.
                                -response-sample-output <path>
                                                        Path to the response-time sample CSV (default
                                                        ResponseTimeSample.csv)
                                -key-pool <n>           Unique entity key (AccountId) pool size (default 1,000,000)
                                -key-skew <s>           Zipfian skew parameter s in P(rank) ~ 1/rank^s (default 0.8, puts
                                                        the top 1% of keys at ~36% of volume for a 1,000,000-key pool;
                                                        reasonable test range 0.8-1.3). Higher values concentrate more
                                                        volume onto fewer keys. Ignored with -uniform-keys.
                                -uniform-keys           Select AccountId uniformly at random over the key pool instead of
                                                        Zipfian - a baseline mode for isolated rules-correctness testing
                                                        where hot keys and lock contention would be a confound.
                                -containers <spec>      Semicolon separated Name=pattern1,pattern2 roles to sample RAM/CPU for.
                                                        Each role gets its own {Name}_CpuPct and {Name}_MemMiB columns in the
                                                        TPS report; containers matching more than one pattern for a role are
                                                        summed into it. Default covers both the single-node dev compose and
                                                        the HA swarm stack's container/service names:
                                                          Postgres=postgres,patroni;Redis=redis;Sentinel=sentinel;
                                                          Etcd=etcd;HAProxy=haproxy;
                                                          Jube.Jobs=jube.jobs,jube-jobs;
                                                          Jube.WebApi=jube.webapi,jube-api,jube-ui
                                -containers-regex       Treat -containers patterns as regex instead of substring match
                                -no-container-stats     Disable sampling `docker stats` for the per-container columns
                                -no-host-stats          Disable sampling /proc for the Host_CpuPct/Host_MemUsedMiB columns
                                -pgconn <connstring>    Npgsql connection string to sample pg_stat_database from, e.g.
                                                        Host=postgres;Port=5432;Database=postgres;Username=postgres;Password=...
                                                        Adds Postgres_Connections, Postgres_TxnPerSec, Postgres_CacheHitPct,
                                                        Postgres_TempMiBPerSec, Postgres_ReplicationLagSeconds,
                                                        Postgres_ReplicaCount. Omit to leave those columns blank.
                                -redisconn <connstring> StackExchange.Redis connection string to sample INFO from, e.g. redis:6379
                                                        Adds Redis_ConnectedClients, Redis_UsedMemMiB, Redis_OpsPerSec,
                                                        Redis_HitRatePct. Omit to leave those columns blank.

                              Container stats require the docker CLI and /var/run/docker.sock to be reachable from
                              wherever this runs (e.g. bind-mount the socket when running this tool itself in a container).

                              Host stats read /proc/stat and /proc/meminfo directly - reachable by default even when
                              this tool runs inside a container (Docker doesn't namespace /proc), but skipped rather
                              than failing the run if unavailable (e.g. non-Linux hosts).

                              Postgres/Redis stats connect directly to the database, independent of container/host
                              sampling, so they still work against a managed or remote instance with no docker access.

                              Postgres_ReplicationLagSeconds reads whichever side of a primary/standby pair -pgconn
                              points at: the age of the last replayed transaction if it's a standby (only meaningful
                              under a steady write stream, which a load test provides), or the worst connected
                              standby's lag from pg_stat_replication if it's a primary. Seeing replicas at all
                              requires -pgconn's role to be a superuser or hold pg_monitor; otherwise
                              Postgres_ReplicaCount reads 0 rather than failing the sample.
                              """);
        }
    }
}
