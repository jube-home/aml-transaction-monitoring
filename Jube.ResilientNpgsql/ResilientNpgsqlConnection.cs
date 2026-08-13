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

namespace Jube.ResilientNpgsqlConnection
{
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Sockets;
    using log4net;
    using Npgsql;
    using Polly;

    public class ResilientNpgsqlConnection : DbConnection
    {
        public ResilientNpgsqlConnection(string connectionString, ILog log, int maxRetries = 10)
        {
            UnderlyingConnection = new NpgsqlConnection(connectionString);

            FailoverPolicy = Policy
                .Handle<NpgsqlException>(ex => ex.IsTransient)
                .Or<NpgsqlException>(ex => ex.InnerException is EndOfStreamException)
                .Or<InvalidOperationException>(ex =>
                    ex.Message.Contains("Connection is not open") ||
                    UnderlyingConnection.State != ConnectionState.Open)
                .Or<PostgresException>(ex =>
                    ex.SqlState.StartsWith("08") ||
                    ex.SqlState.StartsWith("53") ||
                    ex.SqlState is "57P01" or "57P02" or "57P03" or "40001" or "40P01" or "55P03"
                )
                .Or<SocketException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    maxRetries,
                    (attempt, ex, _) =>
                        ex is PostgresException { SqlState: "55P03" or "40001" or "40P01" }
                            ? TimeSpan.FromMilliseconds(Random.Shared.Next(50, 500) * attempt)
                            : TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 30))
                              + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1_000)),
                    (innerEx, _, count, _) =>
                    {
                        var shouldClearPool = innerEx is not PostgresException pg
                                              || !pg.SqlState.StartsWith("53")
                                              && pg.SqlState is not "55P03" and not "40001" and not "40P01";

                        if (shouldClearPool)
                        {
                            NpgsqlConnection.ClearPool(UnderlyingConnection);
                        }

                        try
                        {
                            if (UnderlyingConnection.State != ConnectionState.Open)
                            {
                                UnderlyingConnection.Open();
                            }
                        }
                        catch (Exception outerEx)
                        {
                            log.Warn($"Pg Retry: {count}/{maxRetries}. Reconnect failed. {outerEx.Message}");
                        }

                        if (count == maxRetries)
                        {
                            log.Error($"Pg Retry exhausted: {count}/{maxRetries}. {innerEx.Message}");
                        }
                        else
                        {
                            log.Warn($"Pg Retry: {count}/{maxRetries}. {innerEx.Message}");
                        }

                        return Task.CompletedTask;
                    });
        }

        internal IAsyncPolicy FailoverPolicy { get; }
        public NpgsqlConnection UnderlyingConnection
        {
            get;
        }

        [AllowNull]
        public override string ConnectionString
        {
            get
            {
                return UnderlyingConnection.ConnectionString;
            }
            set
            {
                UnderlyingConnection.ConnectionString = value;
            }
        }

        public override string Database
        {
            get
            {
                return UnderlyingConnection.Database;
            }
        }

        public override string DataSource
        {
            get
            {
                return UnderlyingConnection.DataSource;
            }
        }

        public override string ServerVersion
        {
            get
            {
                return UnderlyingConnection.ServerVersion;
            }
        }

        public override ConnectionState State
        {
            get
            {
                return UnderlyingConnection.State;
            }
        }

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            await FailoverPolicy.ExecuteAsync(async () =>
            {
                if (UnderlyingConnection.State == ConnectionState.Broken)
                {
                    await UnderlyingConnection.CloseAsync().ConfigureAwait(false);
                }

                if (UnderlyingConnection.State != ConnectionState.Open)
                {
                    await UnderlyingConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                }

            }).ConfigureAwait(false);
        }

        public override void Open()
        {
            OpenAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public override void Close()
        {
            UnderlyingConnection.Close();
        }

        public override Task CloseAsync()
        {
            return UnderlyingConnection.CloseAsync();
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return UnderlyingConnection.BeginTransaction(isolationLevel);
        }

        protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(
            IsolationLevel isolationLevel, CancellationToken ct)
        {
            return await UnderlyingConnection.BeginTransactionAsync(isolationLevel, ct).ConfigureAwait(false);
        }

        protected override DbCommand CreateDbCommand()
        {
            return new ResilientNpgsqlCommand(UnderlyingConnection.CreateCommand(), FailoverPolicy);
        }

        public override void ChangeDatabase(string databaseName)
        {
            UnderlyingConnection.ChangeDatabase(databaseName);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnderlyingConnection.Dispose();
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await UnderlyingConnection.DisposeAsync();
            base.Dispose(false);
        }
    }
}
