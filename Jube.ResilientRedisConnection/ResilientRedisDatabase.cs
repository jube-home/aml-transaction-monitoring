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

namespace Jube.ResilientRedisConnection
{
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using log4net;
    using Npgsql;
    using NpgsqlTypes;
    using Polly;
    using ResilientNpgsqlConnection;
    using ResilientNpgsqlConnection.Extensions.Jube.ResilientNpgsqlConnection;
    using StackExchange.Redis;

    public class ResilientRedisDatabase(IDatabase inner, IAsyncPolicy idempotentPolicy, IAsyncPolicy nonIdempotentPolicy, string connectionString, ILog log, bool hsetOffload)
        : IHybridResilientRedisDatabase
    {
        private readonly IAsyncPolicy idempotentPolicy = idempotentPolicy ?? throw new ArgumentNullException(nameof(idempotentPolicy));
        private readonly IAsyncPolicy nonIdempotentPolicy = nonIdempotentPolicy ?? throw new ArgumentNullException(nameof(nonIdempotentPolicy));

        private IDatabase UnderlyingDatabase
        {
            get;
        } = inner ?? throw new ArgumentNullException(nameof(inner));

        public Task<bool> KeyExistsAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.KeyExistsAsync(key, flags),
                new Context());
        }

        public bool KeyExists(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return KeyExistsAsync(key, flags).GetAwaiter().GetResult();
        }

        public bool KeyRename(RedisKey key, RedisKey newKey, When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            return KeyRenameAsync(key, newKey, when, flags).GetAwaiter().GetResult();
        }

        public Task<bool> HashSetAsync(RedisKey key, RedisValue field, RedisValue value,
            When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            if (hsetOffload)
            {
                return PgHashSetAsync(key, field, value, when);
            }

            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashSetAsync(key, field, value, when, flags),
                new Context());
        }

        public void HashSet(RedisKey key, RedisValue field, RedisValue value,
            When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            HashSetAsync(key, field, value, when, flags).GetAwaiter().GetResult();
        }

        public void HashSet(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None)
        {
            HashSetAsync(key, hashFields, flags).GetAwaiter().GetResult();
        }

        public Task<RedisValue> HashGetAsync(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None)
        {
            if (hsetOffload)
            {
                return PgHashGetAsync(key, field);
            }

            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashGetAsync(key, field, flags),
                new Context());
        }

        public Task<RedisValue[]> HashGetAsync(RedisKey key, RedisValue[] fields, CommandFlags flags = CommandFlags.None)
        {
            if (hsetOffload)
            {
                return PgHashGetManyAsync(key, fields);
            }

            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashGetAsync(key, fields, flags),
                new Context());
        }

        public Task<bool> HashDeleteAsync(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None)
        {
            if (hsetOffload)
            {
                return PgHashDeleteAsync(key, field);
            }

            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashDeleteAsync(key, field, flags),
                new Context());
        }

        public Task<long> HashDeleteAsync(RedisKey key, RedisValue[] fields, CommandFlags flags = CommandFlags.None)
        {
            if (hsetOffload)
            {
                return PgHashDeleteManyAsync(key, fields);
            }

            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashDeleteAsync(key, fields, flags),
                new Context());
        }

        public bool HashDelete(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None)
        {
            return HashDeleteAsync(key, field, flags).GetAwaiter().GetResult();
        }

        public Task<long> HashIncrementAsync(RedisKey key, RedisValue field, long value,
            CommandFlags flags = CommandFlags.None)
        {
            if (hsetOffload)
            {
                return PgHashIncrementAsync(key, field, value);
            }

            return nonIdempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashIncrementAsync(key, field, value, flags),
                new Context());
        }

        public Task<double> HashIncrementAsync(RedisKey key, RedisValue field, double value,
            CommandFlags flags = CommandFlags.None)
        {
            if (hsetOffload)
            {
                return PgHashIncrementAsync(key, field, value);
            }

            return nonIdempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashIncrementAsync(key, field, value, flags),
                new Context());
        }

        public Task<long> HashDecrementAsync(RedisKey key, RedisValue field, long value,
            CommandFlags flags = CommandFlags.None)
        {
            if (hsetOffload)
            {
                return PgHashDecrementAsync(key, field, value);
            }

            return nonIdempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashDecrementAsync(key, field, value, flags),
                new Context());
        }

        public Task<double> HashDecrementAsync(RedisKey key, RedisValue field, double value,
            CommandFlags flags = CommandFlags.None)
        {
            if (hsetOffload)
            {
                return PgHashDecrementAsync(key, field, value);
            }

            return nonIdempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashDecrementAsync(key, field, value, flags),
                new Context());
        }

        public Task<long> HashStringLengthAsync(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None)
        {
            if (hsetOffload)
            {
                return PgHashStringLengthAsync(key, field);
            }

            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashStringLengthAsync(key, field, flags),
                new Context());
        }

        public IAsyncEnumerable<HashEntry> HashScanAsync(RedisKey key, RedisValue pattern = default,
            int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None)
        {
            if (hsetOffload)
            {
                return PgHashScanAsync(key, pattern);
            }

            return UnderlyingDatabase.HashScanAsync(key, pattern, pageSize, cursor, pageOffset, flags);
        }

        public IEnumerable<HashEntry> HashScan(RedisKey key, RedisValue pattern = default,
            int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None)
        {
            if (hsetOffload)
            {
                async Task<List<HashEntry>> Collect()
                {
                    var results = new List<HashEntry>();
                    await foreach (var entry in PgHashScanAsync(key, pattern))
                    {
                        results.Add(entry);
                    }
                    return results;
                }

                return Collect().GetAwaiter().GetResult();
            }

            return UnderlyingDatabase.HashScan(key, pattern, pageSize, cursor, pageOffset, flags);
        }

        public Task<bool> SetAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SetAddAsync(key, value, flags),
                new Context());
        }

        public Task<bool> SetContainsAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SetContainsAsync(key, value, flags),
                new Context());
        }

        public Task<bool> SetRemoveAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SetRemoveAsync(key, value, flags),
                new Context());
        }

        public Task<long> SetRemoveAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SetRemoveAsync(key, values, flags),
                new Context());
        }

        public Task<RedisValue[]> SetMembersAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SetMembersAsync(key, flags),
                new Context());
        }

        public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score,
            CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetAddAsync(key, member, score, flags),
                new Context());
        }

        public Task<long> SortedSetLengthAsync(RedisKey key, double min = Double.NegativeInfinity,
            double max = Double.PositiveInfinity, Exclude exclude = Exclude.None,
            CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetLengthAsync(key, min, max, exclude, flags),
                new Context());
        }

        public Task<long> SortedSetRemoveRangeByScoreAsync(RedisKey key,
            double start = Double.NegativeInfinity, double stop = Double.PositiveInfinity,
            Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetRemoveRangeByScoreAsync(key, start, stop, exclude, flags),
                new Context());
        }

        public Task<bool> SortedSetUpdateAsync(RedisKey key, RedisValue member, double score,
            SortedSetWhen when = SortedSetWhen.Always, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetUpdateAsync(key, member, score, when, flags),
                new Context());
        }

        public Task<bool> SortedSetRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetRemoveAsync(key, member, flags),
                new Context());
        }

        public Task<long> SortedSetRemoveAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetRemoveAsync(key, members, flags),
                new Context());
        }

        public Task<SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(RedisKey key,
            long start = 0, long stop = -1, Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetRangeByRankWithScoresAsync(key, start, stop, order, flags),
                new Context());
        }

        public Task<SortedSetEntry[]> SortedSetRangeByScoreWithScoresAsync(RedisKey key,
            double start = Double.NegativeInfinity, double stop = Double.PositiveInfinity,
            Exclude exclude = Exclude.None, Order order = Order.Ascending,
            long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetRangeByScoreWithScoresAsync(key, start, stop, exclude, order, skip, take, flags),
                new Context());
        }

        public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null,
            CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.StringSetAsync(key, value, expiry, When.Always, flags),
                new Context());
        }

        public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry = null,
            CommandFlags flags = CommandFlags.None)
        {
            return StringSetAsync(key, value, expiry, flags).GetAwaiter().GetResult();
        }

        public Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.StringGetAsync(key, flags),
                new Context());
        }

        public Task<long> PublishAsync(RedisChannel channel, RedisValue message,
            CommandFlags flags = CommandFlags.None)
        {
            return nonIdempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.PublishAsync(channel, message, flags),
                new Context());
        }

        private async Task<ResilientNpgsqlConnection> OpenAsync(CancellationToken cancellationToken = default)
        {
            var conn = new ResilientNpgsqlConnection(connectionString, log);
            await conn.OpenAsync(cancellationToken);
            return conn;
        }

        private Task<bool> KeyRenameAsync(RedisKey key, RedisKey newKey, When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.KeyRenameAsync(key, newKey, when, flags),
                new Context());
        }

        private Task HashSetAsync(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None)
        {
            if (hsetOffload)
            {
                return PgHashSetBatchAsync(key, hashFields);
            }

            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashSetAsync(key, hashFields, flags),
                new Context());
        }

        private async Task<bool> PgHashSetAsync(RedisKey key, RedisValue field, RedisValue value, When when)
        {
            var sql = when == When.NotExists
                ? """
                  INSERT INTO "CacheSetHash"("Key","Field","ByteValue")
                  VALUES(@k,@f,@v)
                  ON CONFLICT("Key","Field") DO NOTHING
                  RETURNING true;
                  """
                : """
                  INSERT INTO "CacheSetHash"("Key","Field","ByteValue")
                  VALUES(@k,@f,@v)
                  ON CONFLICT("Key","Field")
                  DO UPDATE SET "ByteValue"=EXCLUDED."ByteValue"
                  RETURNING true;
                  """;
            await using var conn = await OpenAsync();
            await using var cmd = new ResilientNpgsqlCommand(conn, sql);
            cmd.Parameters.AddWithValue("k", key.ToString());
            cmd.Parameters.AddWithValue("f", field.ToString());
            cmd.Parameters.AddWithValue("v", (byte[]?)value ?? Array.Empty<byte>());
            return await cmd.ExecuteScalarAsync() is not null and not DBNull;
        }

        private async Task PgHashSetBatchAsync(RedisKey key, HashEntry[] hashFields)
        {
            if (hashFields.Length == 0)
            {
                return;
            }

            await using var conn = await OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                await using var cmd = new ResilientNpgsqlCommand(conn, """
                                                                       INSERT INTO "CacheSetHash"("Key","Field","ByteValue")
                                                                       VALUES(@k,@f,@v)
                                                                       ON CONFLICT("Key","Field")
                                                                       DO UPDATE SET "ByteValue"=EXCLUDED."ByteValue";
                                                                       """);
                cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("k", key.ToString());
                cmd.Parameters.AddWithValue("f", "");
                cmd.Parameters.AddWithValue("v", Array.Empty<byte>());
                foreach (var e in hashFields)
                {
                    cmd.Parameters["k"].Value = key.ToString();
                    cmd.Parameters["f"].Value = e.Name.ToString();
                    cmd.Parameters["v"].Value = (byte[]?)e.Value ?? Array.Empty<byte>();
                    await cmd.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task<RedisValue> PgHashGetAsync(RedisKey key, RedisValue field)
        {
            await using var conn = await OpenAsync();
            await using var cmd = new ResilientNpgsqlCommand(conn,
                """SELECT "ByteValue","IntValue","FloatValue" FROM "CacheSetHash" WHERE "Key"=@k AND "Field"=@f;""");
            cmd.Parameters.AddWithValue("k", key.ToString());
            cmd.Parameters.AddWithValue("f", field.ToString());
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return RedisValue.Null;
            }
            if (!reader.IsDBNull(0))
            {
                return (RedisValue)reader.GetFieldValue<byte[]>(0);
            }
            if (!reader.IsDBNull(1))
            {
                return (RedisValue)reader.GetInt64(1).ToString();
            }
            if (!reader.IsDBNull(2))
            {
                return (RedisValue)reader.GetDouble(2).ToString(CultureInfo.InvariantCulture);
            }
            return RedisValue.Null;
        }


        private async Task<RedisValue[]> PgHashGetManyAsync(RedisKey key, RedisValue[] fields)
        {
            var fs = fields.Select(f => f.ToString()).ToArray();
            await using var conn = await OpenAsync();
            await using var cmd = new ResilientNpgsqlCommand(conn,
                """SELECT "Field","ByteValue","IntValue","FloatValue" FROM "CacheSetHash" WHERE "Key"=@k AND "Field"=ANY(@fields);""");
            cmd.Parameters.AddWithValue("k", key.ToString());
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
            cmd.Parameters.Add(new NpgsqlParameter("fields", NpgsqlDbType.Array | NpgsqlDbType.Text)
            {
                Value = fs
            });
            var lookup = new Dictionary<string, RedisValue>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var f = reader.GetString(0);
                RedisValue value;
                if (!reader.IsDBNull(1))
                {
                    value = (RedisValue)reader.GetFieldValue<byte[]>(1);
                }
                else if (!reader.IsDBNull(2))
                {
                    value = (RedisValue)reader.GetInt64(2).ToString();
                }
                else if (!reader.IsDBNull(3))
                {
                    value = (RedisValue)reader.GetDouble(3).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    value = RedisValue.Null;
                }
                lookup[f] = value;
            }
            return fs.Select(f => lookup.TryGetValue(f, out var v) ? v : RedisValue.Null).ToArray();
        }

        private async Task<bool> PgHashDeleteAsync(RedisKey key, RedisValue field)
        {
            await using var conn = await OpenAsync();
            await using var cmd = new ResilientNpgsqlCommand(conn,
                """DELETE FROM "CacheSetHash" WHERE "Key"=@k AND "Field"=@f;""");
            cmd.Parameters.AddWithValue("k", key.ToString());
            cmd.Parameters.AddWithValue("f", field.ToString());
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        private async Task<long> PgHashDeleteManyAsync(RedisKey key, RedisValue[] fields)
        {
            await using var conn = await OpenAsync();
            await using var cmd = new ResilientNpgsqlCommand(conn,
                """DELETE FROM "CacheSetHash" WHERE "Key"=@k AND "Field"=ANY(@fields);""");
            cmd.Parameters.AddWithValue("k", key.ToString());
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
            cmd.Parameters.Add(new NpgsqlParameter("fields", NpgsqlDbType.Array | NpgsqlDbType.Text)
            {
                Value = fields.Select(f => f.ToString()).ToArray()
            });
            return await cmd.ExecuteNonQueryAsync();
        }

        private async Task<long> PgHashIncrementAsync(RedisKey key, RedisValue field, long value)
        {
            await using var conn = await OpenAsync();
            await using var cmd = new ResilientNpgsqlCommand(conn, """
                                                                   INSERT INTO "CacheSetHash"("Key","Field","IntValue")
                                                                   VALUES(@k,@f,@v)
                                                                   ON CONFLICT("Key","Field")
                                                                   DO UPDATE SET "IntValue"=COALESCE("CacheSetHash"."IntValue",0)+@v
                                                                   RETURNING "IntValue";
                                                                   """);
            cmd.Parameters.AddWithValue("k", key.ToString());
            cmd.Parameters.AddWithValue("f", field.ToString());
            cmd.Parameters.AddWithValue("v", value);
            return (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        }

        private async Task<double> PgHashIncrementAsync(RedisKey key, RedisValue field, double value)
        {
            await using var conn = await OpenAsync();
            await using var cmd = new ResilientNpgsqlCommand(conn, """
                                                                   INSERT INTO "CacheSetHash"("Key","Field","FloatValue")
                                                                   VALUES(@k,@f,@v)
                                                                   ON CONFLICT("Key","Field")
                                                                   DO UPDATE SET "FloatValue"=COALESCE("CacheSetHash"."FloatValue",0)+@v
                                                                   RETURNING "FloatValue";
                                                                   """);
            cmd.Parameters.AddWithValue("k", key.ToString());
            cmd.Parameters.AddWithValue("f", field.ToString());
            cmd.Parameters.AddWithValue("v", value);
            return (double)(await cmd.ExecuteScalarAsync() ?? 0d);
        }

        private Task<long> PgHashDecrementAsync(RedisKey key, RedisValue field, long value)
        {
            return PgHashIncrementAsync(key, field, -value);
        }

        private Task<double> PgHashDecrementAsync(RedisKey key, RedisValue field, double value)
        {
            return PgHashIncrementAsync(key, field, -value);
        }

        private async Task<long> PgHashStringLengthAsync(RedisKey key, RedisValue field)
        {
            await using var conn = await OpenAsync();
            await using var cmd = new ResilientNpgsqlCommand(conn,
                """SELECT length("ByteValue"), length("IntValue"::text), length("FloatValue"::text) FROM "CacheSetHash" WHERE "Key"=@k AND "Field"=@f;""");
            cmd.Parameters.AddWithValue("k", key.ToString());
            cmd.Parameters.AddWithValue("f", field.ToString());
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return 0L;
            }
            if (!reader.IsDBNull(0))
            {
                return Convert.ToInt64(reader.GetValue(0));
            }
            if (!reader.IsDBNull(1))
            {
                return Convert.ToInt64(reader.GetValue(1));
            }
            if (!reader.IsDBNull(2))
            {
                return Convert.ToInt64(reader.GetValue(2));
            }
            return 0L;
        }

        private async IAsyncEnumerable<HashEntry> PgHashScanAsync(RedisKey key, RedisValue pattern,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            var like = GlobToLike(pattern);
            var sql = like is null
                ? """SELECT "Field","ByteValue","IntValue","FloatValue" FROM "CacheSetHash" WHERE "Key"=@k ORDER BY "Field";"""
                : """SELECT "Field","ByteValue","IntValue","FloatValue" FROM "CacheSetHash" WHERE "Key"=@k AND "Field" LIKE @p ORDER BY "Field";""";
            await using var conn = await OpenAsync(token);
            await using var cmd = new ResilientNpgsqlCommand(conn, sql);
            cmd.Parameters.AddWithValue("k", key.ToString());
            if (like is not null)
            {
                cmd.Parameters.AddWithValue("p", like);
            }
            await using var reader = await cmd.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                var field = reader.GetString(0);
                RedisValue value;
                if (!reader.IsDBNull(1))
                {
                    value = (RedisValue)reader.GetFieldValue<byte[]>(1);
                }
                else if (!reader.IsDBNull(2))
                {
                    value = (RedisValue)reader.GetInt64(2).ToString();
                }
                else if (!reader.IsDBNull(3))
                {
                    value = (RedisValue)reader.GetDouble(3).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    value = RedisValue.Null;
                }
                yield return new HashEntry(field, value);
            }
        }

        private static string? GlobToLike(RedisValue pattern)
        {
            var p = pattern.ToString();
            if (String.IsNullOrEmpty(p) || p == "*")
            {
                return null;
            }

            return p
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_")
                .Replace("*", "%")
                .Replace("?", "_");
        }
    }
}
