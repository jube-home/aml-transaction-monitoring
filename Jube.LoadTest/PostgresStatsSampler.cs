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
    using System.Data;
    using System.Diagnostics;
    using Npgsql;

    public sealed class PostgresStatsSampler(string connectionString) : IAsyncDisposable
    {
        private const string StatsQuery = """
                                          SELECT
                                              d.numbackends,
                                              d.xact_commit,
                                              d.xact_rollback,
                                              d.blks_hit,
                                              d.blks_read,
                                              d.temp_bytes,
                                              pg_is_in_recovery() AS is_replica,
                                              CASE WHEN pg_is_in_recovery()
                                                  THEN EXTRACT(EPOCH FROM (now() - pg_last_xact_replay_timestamp()))
                                                  ELSE NULL
                                              END AS standby_lag_seconds,
                                              (SELECT MAX(EXTRACT(EPOCH FROM replay_lag)) FROM pg_stat_replication) AS max_replica_lag_seconds,
                                              (SELECT COUNT(*) FROM pg_stat_replication) AS replica_count
                                          FROM pg_stat_database d
                                          WHERE d.datname = current_database()
                                          """;
        private readonly Stopwatch sinceLastSample = new Stopwatch();

        private NpgsqlConnection? connection;
        private (long Commits, long Rollbacks, long BlksHit, long BlksRead, long TempBytes)? lastSample;

        public async ValueTask DisposeAsync()
        {
            if (connection != null)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }

        public async Task<(double Connections, double TxnPerSec, double CacheHitPct, double TempMiBPerSec,
            double ReplicationLagSeconds, double ReplicaCount)?> SampleAsync(CancellationToken token = default)
        {
            try
            {
                if (connection is not { State: ConnectionState.Open })
                {
                    connection?.Dispose();
                    connection = new NpgsqlConnection(connectionString);
                    await connection.OpenAsync(token).ConfigureAwait(false);
                }

                await using var command = new NpgsqlCommand(StatsQuery, connection);
                await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

                if (!await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    return null;
                }

                var connections = reader.GetInt32(0);
                var commits = reader.GetInt64(1);
                var rollbacks = reader.GetInt64(2);
                var blksHit = reader.GetInt64(3);
                var blksRead = reader.GetInt64(4);
                var tempBytes = await reader.IsDBNullAsync(5, token).ConfigureAwait(false) ? 0 : reader.GetInt64(5);
                var isReplica = reader.GetBoolean(6);
                var standbyLagSeconds = await reader.IsDBNullAsync(7, token).ConfigureAwait(false) ? 0.0 : reader.GetDouble(7);
                var maxReplicaLagSeconds = await reader.IsDBNullAsync(8, token).ConfigureAwait(false) ? 0.0 : reader.GetDouble(8);
                var replicaCount = reader.GetInt64(9);

                double txnPerSec = 0;
                double cacheHitPct = 0;
                double tempMiBPerSec = 0;

                if (lastSample is {} last)
                {
                    var elapsedSeconds = sinceLastSample.Elapsed.TotalSeconds;
                    var txnDelta = commits - last.Commits + (rollbacks - last.Rollbacks);
                    txnPerSec = elapsedSeconds > 0 ? txnDelta / elapsedSeconds : 0;

                    var hitDelta = blksHit - last.BlksHit;
                    var readDelta = blksRead - last.BlksRead;
                    var totalBlockDelta = hitDelta + readDelta;
                    cacheHitPct = totalBlockDelta > 0 ? 100.0 * hitDelta / totalBlockDelta : 100.0;

                    var tempMiBDelta = (tempBytes - last.TempBytes) / 1024.0 / 1024.0;
                    tempMiBPerSec = elapsedSeconds > 0 ? tempMiBDelta / elapsedSeconds : 0;
                }

                lastSample = (commits, rollbacks, blksHit, blksRead, tempBytes);
                sinceLastSample.Restart();

                var replicationLagSeconds = isReplica ? standbyLagSeconds : maxReplicaLagSeconds;

                return (connections, txnPerSec, cacheHitPct, tempMiBPerSec, replicationLagSeconds, replicaCount);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"postgres stats sampling failed: {ex.Message}").ConfigureAwait(false);

                if (connection != null)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                    connection = null;
                }

                return null;
            }
        }
    }
}
