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
    using System.Dynamic;
    using System.Linq;
    using System.Threading.Tasks;
    using Dictionary;
    using Extension;
    using Newtonsoft.Json;
    using Npgsql;

    public class Postgres(string connectionString)
    {
        public async Task<Dictionary<string, string>> IntrospectAsync(string sql, Dictionary<string, object> parameters)
        {
            var connection = new NpgsqlConnection(connectionString);
            var values = new Dictionary<string, string>();
            try
            {
                await connection.OpenAsync().ConfigureAwait(false);

                var tableName = "Temp_" + Guid.NewGuid().ToString("N");

                var wrapSql = $"select * into TEMPORARY TABLE {tableName} from (select * from ({sql}) b LIMIT 0) c";
                var commandTempTable = new NpgsqlCommand(wrapSql);
                commandTempTable.Connection = connection;

                foreach (var (key, value) in parameters.Where(parameter => sql.Contains("@" + parameter.Key)))
                {
                    commandTempTable.Parameters.AddWithValue(key, value);
                }

                await commandTempTable.ExecuteNonQueryAsync().ConfigureAwait(false);

                var introspectionSql = "SELECT attname, format_type(atttypid, atttypmod) AS type" +
                                       " FROM pg_attribute" +
                                       $" WHERE attrelid = '{tableName}'::regclass" +
                                       " AND attnum > 0 " +
                                       " AND NOT attisdropped " +
                                       " ORDER BY attnum";


                var command = new NpgsqlCommand(introspectionSql);
                command.Connection = connection;

                var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    values.Add(reader.GetValue(0).AsString(), reader.GetValue(1).AsString());
                }

                await reader.CloseAsync().ConfigureAwait(false);
                await reader.DisposeAsync().ConfigureAwait(false);
                await command.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
            }

            return values;
        }

        public async Task PrepareAsync(string sql, List<object> parameters)
        {
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync().ConfigureAwait(false);

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;

                for (var i = 0; i < parameters.Count; i++)
                {
                    command.Parameters.AddWithValue("@" + (i + 1), parameters[i]);
                }

                await command.PrepareAsync().ConfigureAwait(false);
            }
            catch
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }

        public async Task<List<string>> ExecuteReturnOnlyJsonFromArchiveSampleAsync(int entityAnalysisModelId,
            string filterSql,
            string filterTokens,
            int limit, bool mockData)
        {
            var value = new List<string>();
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync().ConfigureAwait(false);

                var tokens = JsonConvert.DeserializeObject<List<object>>(filterTokens);
                tokens.Add(entityAnalysisModelId);
                tokens.Add(limit);

                var tableName = mockData ? "MockArchive" : "Archive";

                var sql =
                    $"select \"Json\" from \"{tableName}\" where \"EntityAnalysisModelId\" = (@{tokens.Count - 1})"
                    + " and " + filterSql
                    + $" order by \"EntityAnalysisModelInstanceEntryGuid\" limit (@{limit})";

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;

                for (var i = 0; i < tokens.Count; i++)
                {
                    command.Parameters.AddWithValue("@" + (i + 1), tokens[i]);
                }

                await command.PrepareAsync().ConfigureAwait(false);

                var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    value.Add(reader.GetValue(0).AsString());
                }

                await reader.CloseAsync().ConfigureAwait(false);
                await reader.DisposeAsync().ConfigureAwait(false);
                await command.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
            }

            return value;
        }

        public async Task<List<DictionaryNoBoxing>> ExecuteReturnPayloadFromArchiveWithSkipLimitAsync(
            string sql,
            DateTime adjustedStartDate,
            int skip,
            int limit)
        {
            var value = new List<DictionaryNoBoxing>();
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync().ConfigureAwait(false);

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;
                command.Parameters.AddWithValue("adjustedStartDate", adjustedStartDate);
                command.Parameters.AddWithValue("limit", limit);
                command.Parameters.AddWithValue("skip", skip);
                await command.PrepareAsync().ConfigureAwait(false);

                var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var dictionaryNoBoxing = new DictionaryNoBoxing();
                    for (var index = 0; index < reader.FieldCount; index++)
                    {
                        if (!dictionaryNoBoxing.ContainsKey(reader.GetName(index)))
                        {
                            var clrType = reader.GetFieldType(index);

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

                await reader.CloseAsync().ConfigureAwait(false);
                await reader.DisposeAsync().ConfigureAwait(false);
                await command.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
            }

            return value;
        }

        public async Task<List<IDictionary<string, object>>> ExecuteByNamedParametersAsync(string sql,
            Dictionary<string, object> parameters)
        {
            var value = new List<IDictionary<string, object>>();
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync().ConfigureAwait(false);

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;

                foreach (var (key, o) in parameters)
                {
                    command.Parameters.AddWithValue("@" + key, o);
                }

                await command.PrepareAsync().ConfigureAwait(false);

                var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    IDictionary<string, object> eo = new ExpandoObject();
                    for (var index = 0; index < reader.FieldCount; index++)
                    {
                        if (!eo.ContainsKey(reader.GetName(index)))
                        {
                            eo.Add(reader.GetName(index), reader.IsDBNull(index) ? null : reader.GetValue(index));
                        }
                    }

                    value.Add(eo);
                }

                await reader.CloseAsync().ConfigureAwait(false);
                await reader.DisposeAsync().ConfigureAwait(false);
                await command.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
            }

            return value;
        }

        public async Task<List<IDictionary<string, object>>> ExecuteByOrderedParametersAsync(string sql,
            List<object> parameters)
        {
            var value = new List<IDictionary<string, object>>();
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync().ConfigureAwait(false);

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;

                for (var i = 0; i < parameters.Count; i++)
                {
                    command.Parameters.AddWithValue("@" + (i + 1), parameters[i]);
                }

                await command.PrepareAsync().ConfigureAwait(false);

                var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    IDictionary<string, object> eo = new ExpandoObject();
                    for (var index = 0; index < reader.FieldCount; index++)
                    {
                        if (!eo.ContainsKey(reader.GetName(index)))
                        {
                            eo.Add(reader.GetName(index), reader.IsDBNull(index) ? null : reader.GetValue(index));
                        }
                    }

                    value.Add(eo);
                }

                await reader.CloseAsync().ConfigureAwait(false);
                await reader.DisposeAsync().ConfigureAwait(false);
                await command.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
            }

            return value;
        }
    }
}
