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
    using System.Linq;
    using System.Text.RegularExpressions;
    using PgSqlParser;

    public static class PostgresSqlValidator
    {
        public static void AssertSelectOnly(string sql)
        {
            var normalizedSql = Regex.Replace(sql, @"@\w+", "1"); //Does not support names, but not an issue for just a parse.
            var result = Parser.Parse(normalizedSql);

            if (result.Error is not null)
            {
                throw new InvalidOperationException(
                    $"SQL parse error: {result.Error.Message}");
            }

            var parsed = result.Value;

            if (parsed.Stmts.Count == 0)
            {
                throw new InvalidOperationException("No SQL statement provided.");
            }

            if (parsed.Stmts.Any(rawStmt => rawStmt.Stmt.SelectStmt == null))
            {
                throw new InvalidOperationException(
                    "Only SELECT statements are permitted. Blocked statement detected in query.");
            }
        }
    }
}
