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

namespace Jube.Data.Query.DynamicResultsSchema
{
    using System;
    using System.Collections.Generic;

    public static class DynamicResultSchema
    {
        public static object NormaliseRecordValue(object value)
        {
            return value switch
            {
                DateTime dt => new DateTimeOffset(dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime(), TimeSpan.Zero),
                DBNull => null,
                _ => value
            };
        }

        public static string ClrTypeToSchemaType(object value)
        {
            return value switch
            {
                long or int or double or float => "number",
                bool => "boolean",
                DateTimeOffset => "date",
                _ => "string"
            };
        }

        public static DynamicResultSchemaDto Build(IEnumerable<IDictionary<string, object>> records)
        {
            var schema = new Dictionary<string, string>();
            var rows = new List<Dictionary<string, object>>();

            foreach (var record in records)
            {
                var row = new Dictionary<string, object>();

                foreach (var (name, raw) in record)
                {
                    var value = NormaliseRecordValue(raw);
                    row[name] = value;

                    if (value != null)
                    {
                        schema[name] = ClrTypeToSchemaType(value);
                    }
                    else
                    {
                        schema.TryAdd(name, "string");
                    }
                }

                rows.Add(row);
            }

            return new DynamicResultSchemaDto
            {
                Schema = schema,
                Rows = rows
            };
        }
    }

    public class DynamicResultSchemaDto
    {
        public Dictionary<string, string> Schema { get; set; }
        public List<Dictionary<string, object>> Rows { get; set; }
    }
}
