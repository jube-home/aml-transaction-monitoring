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
    using Npgsql;
    using Polly;

    public class ResilientNpgsqlCommand : DbCommand
    {
        private readonly NpgsqlCommand inner;
        private readonly IAsyncPolicy policy;

        internal ResilientNpgsqlCommand(NpgsqlCommand inner, IAsyncPolicy policy)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public ResilientNpgsqlCommand(ResilientNpgsqlConnection connection)
            : this(connection.UnderlyingConnection.CreateCommand(), connection.FailoverPolicy)
        {
        }

        public ResilientNpgsqlCommand(ResilientNpgsqlConnection connection, string commandText)
            : this(connection)
        {
            CommandText = commandText;
        }

        [AllowNull]
        public override sealed string CommandText
        {
            get
            {
                return inner.CommandText;
            }
            set
            {
                inner.CommandText = value;
            }
        }

        public override int CommandTimeout
        {
            get
            {
                return inner.CommandTimeout;
            }
            set
            {
                inner.CommandTimeout = value;
            }
        }

        public override CommandType CommandType
        {
            get
            {
                return inner.CommandType;
            }
            set
            {
                inner.CommandType = value;
            }
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get
            {
                return inner.UpdatedRowSource;
            }
            set
            {
                inner.UpdatedRowSource = value;
            }
        }

        public override bool DesignTimeVisible
        {
            get
            {
                return inner.DesignTimeVisible;
            }
            set
            {
                inner.DesignTimeVisible = value;
            }
        }

        [AllowNull]
        protected override DbConnection DbConnection
        {
            get
            {
                return inner.Connection!;
            }
            set
            {
                inner.Connection = value switch
                {
                    ResilientNpgsqlConnection resilient => resilient.UnderlyingConnection,
                    NpgsqlConnection raw => raw,
                    null => null,
                    _ => throw new ArgumentException(
                        "Connection must be a NpgsqlConnection or ResilientNpgsqlConnection.", nameof(value))
                };
            }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get
            {
                return inner.Parameters;
            }
        }

        [AllowNull]
        protected override DbTransaction DbTransaction
        {
            get
            {
                return inner.Transaction!;
            }
            set
            {
                inner.Transaction = (NpgsqlTransaction)value!;
            }
        }

        public override void Prepare()
        {
            inner.Prepare();
        }

        public override async Task PrepareAsync(CancellationToken ct = default)
        {
            await inner.PrepareAsync(ct).ConfigureAwait(false);
        }

        private async Task EnsurePreparedAsync(CancellationToken ct)
        {
            if (inner.IsPrepared)
            {
                return;
            }
            await inner.PrepareAsync(ct).ConfigureAwait(false);
        }

        public override async Task<int> ExecuteNonQueryAsync(CancellationToken ct)
        {
            if (inner.Transaction != null)
            {
                await EnsurePreparedAsync(ct).ConfigureAwait(false);
                return await inner.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            return await policy.ExecuteAsync(async _ =>
            {
                await EnsurePreparedAsync(ct).ConfigureAwait(false);
                return await inner.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }, new Context()).ConfigureAwait(false);
        }

        public override async Task<object?> ExecuteScalarAsync(CancellationToken ct)
        {
            if (inner.Transaction != null)
            {
                await EnsurePreparedAsync(ct).ConfigureAwait(false);
                return await inner.ExecuteScalarAsync(ct).ConfigureAwait(false);
            }

            return await policy.ExecuteAsync(async _ =>
            {
                await EnsurePreparedAsync(ct).ConfigureAwait(false);
                return await inner.ExecuteScalarAsync(ct).ConfigureAwait(false);
            }, new Context()).ConfigureAwait(false);
        }

        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken ct)
        {
            if (inner.Transaction != null)
            {
                await EnsurePreparedAsync(ct).ConfigureAwait(false);
                return await inner.ExecuteReaderAsync(behavior, ct).ConfigureAwait(false);
            }

            return await policy.ExecuteAsync(async _ =>
            {
                await EnsurePreparedAsync(ct).ConfigureAwait(false);
                return (DbDataReader)await inner.ExecuteReaderAsync(behavior, ct).ConfigureAwait(false);
            }, new Context()).ConfigureAwait(false);
        }

        public override int ExecuteNonQuery()
        {
            return ExecuteNonQueryAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        public override object? ExecuteScalar()
        {
            return ExecuteScalarAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return ExecuteDbDataReaderAsync(behavior, CancellationToken.None).GetAwaiter().GetResult();
        }

        public override void Cancel()
        {
            inner.Cancel();
        }

        protected override DbParameter CreateDbParameter()
        {
            return inner.CreateParameter();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }
            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            return inner.DisposeAsync();
        }
    }
}
