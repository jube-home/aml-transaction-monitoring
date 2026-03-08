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

namespace Jube.Data.Query
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Dictionary;
    using Extension;
    using log4net;
    using ResilientNpgsqlConnection;
    using ResilientNpgsqlConnection.Extensions.Jube.ResilientNpgsqlConnection;

    public class GetArchiveSqlByKeyValueLimitQuery(DbContext dbContext, ILog log)
    {
        public async Task<List<DictionaryNoBoxing<string>>> ExecuteAsync(string sql,
            string key, string value, string order, int limit, CancellationToken token = default)
        {
            var values = new List<DictionaryNoBoxing<string>>();
            try
            {
                await using var command = new ResilientNpgsqlCommand((ResilientNpgsqlConnection)dbContext.Connection, sql);
                command.Parameters.AddWithValue("key", key);
                command.Parameters.AddWithValue("value", value);
                command.Parameters.AddWithValue("order", order);
                command.Parameters.AddWithValue("limit", limit);
                await command.PrepareAsync(token).ConfigureAwait(false);

                await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
                while (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    var document = new DictionaryNoBoxing<string>();
                    for (var index = 0; index < reader.FieldCount; index++)
                    {
                        if (await reader.IsDBNullAsync(index, token))
                        {
                            continue;
                        }

                        if (document.ContainsKey(reader.GetName(index)))
                        {
                            continue;
                        }

                        var clrType = reader.GetFieldType(index);

                        if (clrType == typeof(int))
                        {
                            document.TryAdd(reader.GetName(index), reader.GetValue(index).AsInt());
                        }
                        else if (clrType == typeof(decimal) || clrType == typeof(float) || clrType == typeof(double))
                        {
                            document.TryAdd(reader.GetName(index), reader.GetValue(index).AsDouble());
                        }
                        else if (clrType == typeof(bool))
                        {
                            document.TryAdd(reader.GetName(index), reader.GetBoolean(index));
                        }
                        else if (clrType == typeof(string))
                        {
                            document.TryAdd(reader.GetName(index), reader.GetValue(index).AsString());
                        }
                        else if (clrType == typeof(DateTime))
                        {
                            document.TryAdd(reader.GetName(index), reader.GetValue(index).AsDateTime());
                        }
                        else if (clrType == typeof(Guid))
                        {
                            document.TryAdd(reader.GetName(index), reader.GetValue(index).AsString());
                        }
                    }

                    values.Add(document);
                }

                await reader.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Archive SQL: Has created an exception as {ex}.");
            }

            return values;
        }
    }
}
