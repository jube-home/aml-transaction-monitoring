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

namespace Jube.Data.Reporting
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Dynamic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dictionary;
    using Extension;
    using log4net;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using ResilientNpgsqlConnection;
    using ResilientNpgsqlConnection.Extensions.Jube.ResilientNpgsqlConnection;

    public class Postgres : IDisposable
    {
        private readonly ResilientNpgsqlConnection connection;
        private readonly bool parserAssertSelectOnly;
        private bool disposed;

        public Postgres(string connectionString, ILog log, bool parserAssertSelectOnly)
        {
            this.parserAssertSelectOnly = parserAssertSelectOnly;
            connection = new ResilientNpgsqlConnection(connectionString, log);
            try
            {
                connection.Open();
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            connection.Dispose();
        }

        public async Task<Dictionary<string, string>> IntrospectAsync(string sql, Dictionary<string, object> parameters, CancellationToken token = default)
        {
            var values = new Dictionary<string, string>();
            var wrapSql = $"SELECT * FROM ({sql}) b LIMIT 0";

            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(wrapSql);
            }

            await using var command = new ResilientNpgsqlCommand(connection, wrapSql);

            foreach (var (key, value) in parameters.Where(parameter => sql.Contains("@" + parameter.Key)))
            {
                command.Parameters.AddWithValue(key, value);
            }

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo, token).ConfigureAwait(false);

            var schema = reader.GetColumnSchema();
            foreach (var col in schema)
            {
                values.Add(col.ColumnName, col.DataTypeName ?? "unknown");
            }

            return values;
        }

        public async Task<Dictionary<string, string>> IntrospectAsync(string sql, List<object> parameters, CancellationToken token = default)
        {
            var values = new Dictionary<string, string>();
            var wrapSql = $"SELECT * FROM ({sql}) b LIMIT 0";

            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(wrapSql);
            }

            await using var command = new ResilientNpgsqlCommand(connection, wrapSql);

            for (var i = 0; i < parameters.Count; i++)
            {
                command.Parameters.AddWithValue("@" + (i + 1), parameters[i]);
            }

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo, token).ConfigureAwait(false);

            var schema = reader.GetColumnSchema();
            foreach (var col in schema)
            {
                values.Add(col.ColumnName, col.DataTypeName ?? "unknown");
            }

            return values;
        }

        public async Task PrepareAsync(string sql, List<object> parameters, CancellationToken token = default)
        {
            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(sql);
            }

            await using var command = new ResilientNpgsqlCommand(connection, sql);

            for (var i = 0; i < parameters.Count; i++)
            {
                command.Parameters.AddWithValue("@" + (i + 1), parameters[i]);
            }

            await command.PrepareAsync(token);
        }

        public async Task<List<string>> ExecuteReturnOnlyJsonFromArchiveSampleAsync(int entityAnalysisModelId,
            string sql,
            string filterTokens,
            int limit, bool mockData, CancellationToken token = default)
        {
            var value = new List<string>();

            var tokens = JsonConvert.DeserializeObject<List<object>>(filterTokens);
            tokens.Add(entityAnalysisModelId);
            tokens.Add(limit);

            var tableName = mockData ? "MockArchive" : "Archive";

            var dynamicGatedSql =
                $"select \"Json\" from \"{tableName}\" where \"EntityAnalysisModelId\" = (@{tokens.Count - 1})"
                + " and " + sql
                + $" order by \"EntityAnalysisModelInstanceEntryGuid\" limit (@{limit})";

            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(dynamicGatedSql);
            }

            await using var command = new ResilientNpgsqlCommand(connection, dynamicGatedSql);

            for (var i = 0; i < tokens.Count; i++)
            {
                command.Parameters.AddWithValue("@" + (i + 1), tokens[i]);
            }

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                value.Add(reader.GetValue(0).AsString());
            }

            return value;
        }

        public async Task<List<JObject>> ExecuteReturnOnlyJsonFromArchiveSampleAsync(int entityAnalysisModelId,
            double sample, DateTime? dateFrom, DateTime? dateTo, CancellationToken token = default)
        {
            var value = new List<JObject>();

            var dynamicGatedSql = """
                                  SELECT a."Json"
                                  FROM "Archive" a
                                  INNER JOIN "EntityAnalysisModel" e ON a."EntityAnalysisModelId" = e."Id"
                                  WHERE e."Id" = @entityAnalysisModelId
                                  AND a."ReferenceDate" BETWEEN @dateFrom AND @dateTo
                                  AND random() < @sample
                                  LIMIT 100000
                                  """;

            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(dynamicGatedSql);
            }

            await using var command = new ResilientNpgsqlCommand(connection, dynamicGatedSql);
            command.Parameters.AddWithValue("@entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("@sample", sample);
            command.Parameters.AddWithValue("@dateFrom", dateFrom ?? DateTime.UtcNow);
            command.Parameters.AddWithValue("@dateTo", dateTo ?? DateTime.UtcNow.AddDays(-1));

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                value.Add(JObject.Parse(reader.GetValue(0).AsString()));
            }

            return value;
        }

        public async Task<List<DictionaryNoBoxing<string>>> ExecuteReturnPayloadFromArchiveWithSkipLimitAsync(
            string sql,
            DateTime adjustedStartDate,
            int skip,
            int limit,
            CancellationToken token = default)
        {
            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(sql);
            }

            await using var command = new ResilientNpgsqlCommand(connection, sql);

            command.Parameters.AddWithValue("adjustedStartDate", adjustedStartDate);
            command.Parameters.AddWithValue("limit", limit);
            command.Parameters.AddWithValue("skip", skip);

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            var value = new List<DictionaryNoBoxing<string>>();
            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                var dictionaryNoBoxing = new DictionaryNoBoxing<string>();
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    if (!dictionaryNoBoxing.ContainsKey(reader.GetName(index)))
                    {
                        var clrType = reader.GetFieldType(index);

                        if (await reader.IsDBNullAsync(index, token))
                        {
                            continue;
                        }

                        if (clrType == typeof(int))
                        {
                            dictionaryNoBoxing.TryAdd(reader.GetName(index), reader.GetValue(index).AsInt());
                        }
                        else if (clrType == typeof(decimal) || clrType == typeof(float) || clrType == typeof(double))
                        {
                            dictionaryNoBoxing.TryAdd(reader.GetName(index), reader.GetValue(index).AsDouble());
                        }
                        else if (clrType == typeof(bool))
                        {
                            dictionaryNoBoxing.TryAdd(reader.GetName(index), reader.GetBoolean(index));
                        }
                        else if (clrType == typeof(string))
                        {
                            dictionaryNoBoxing.TryAdd(reader.GetName(index), reader.GetValue(index).AsString());
                        }
                        else if (clrType == typeof(DateTime))
                        {
                            dictionaryNoBoxing.TryAdd(reader.GetName(index), reader.GetValue(index).AsDateTime());
                        }
                        else if (clrType == typeof(Guid))
                        {
                            dictionaryNoBoxing.TryAdd(reader.GetName(index), reader.GetValue(index).AsGuid());
                        }
                    }
                }

                value.Add(dictionaryNoBoxing);
            }

            return value;
        }

        public async Task<List<IDictionary<string, object>>> ExecuteByNamedParametersAsync(string sql,
            Dictionary<string, object> parameters, CancellationToken token = default)
        {
            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(sql);
            }

            await using var command = new ResilientNpgsqlCommand(connection, sql);

            foreach (var (key, o) in parameters)
            {
                command.Parameters.AddWithValue("@" + key, o);
            }

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            var value = new List<IDictionary<string, object>>();
            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                IDictionary<string, object> eo = new ExpandoObject();
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    if (!eo.ContainsKey(reader.GetName(index)))
                    {
                        eo.Add(reader.GetName(index), await reader.IsDBNullAsync(index, token) ? null : reader.GetValue(index));
                    }
                }

                value.Add(eo);
            }

            return value;
        }

        public async Task<List<IDictionary<string, object>>> ExecuteByOrderedParametersAsync(string sql,
            List<object> parameters, CancellationToken token = default)
        {
            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(sql);
            }

            await using var command = new ResilientNpgsqlCommand(connection, sql);

            for (var i = 0; i < parameters.Count; i++)
            {
                command.Parameters.AddWithValue("@" + (i + 1), parameters[i]);
            }

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            var value = new List<IDictionary<string, object>>();
            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                IDictionary<string, object> eo = new ExpandoObject();
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    if (!eo.ContainsKey(reader.GetName(index)))
                    {
                        eo.Add(reader.GetName(index), await reader.IsDBNullAsync(index, token) ? null : reader.GetValue(index));
                    }
                }

                value.Add(eo);
            }

            return value;
        }

        public async Task<int?> ExecuteScalarIdAsync(string sql,
            List<object> parameters, CancellationToken token = default)
        {
            if (parserAssertSelectOnly)
            {
                PostgresSqlValidator.AssertSelectOnly(sql);
            }

            await using var command = new ResilientNpgsqlCommand(connection, sql);

            for (var i = 0; i < parameters.Count; i++)
            {
                command.Parameters.AddWithValue("@" + (i + 1), parameters[i]);
            }

            var id = await command.ExecuteScalarAsync(token).ConfigureAwait(false);
            if (id == null)
            {
                return null;
            }

            return (int)id;
        }
    }
}
