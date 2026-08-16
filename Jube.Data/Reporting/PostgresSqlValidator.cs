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
        private static readonly (Regex Pattern, string Replacement)[] NormalisationRules =
        [
            (new Regex(@"@\w+", RegexOptions.Compiled), "1")
        ];

        public static void AssertSelectOnly(string sql)
        {
            var normalizedSql = NormalisationRules.Aggregate(sql, (current, rule) => rule.Pattern.Replace(current, rule.Replacement));
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

            foreach (var rawStmt in parsed.Stmts)
            {
                if (rawStmt.Stmt.SelectStmt == null)
                {
                    throw new InvalidOperationException(
                        "Only SELECT statements are permitted. Blocked statement detected in query.");
                }

                AssertNoDataModifyingSubqueries(rawStmt.Stmt.SelectStmt);
            }
        }

        private static void AssertNoDataModifyingSubqueries(SelectStmt selectStmt)
        {
            if (selectStmt.Larg != null)
            {
                AssertNoDataModifyingSubqueries(selectStmt.Larg);
            }

            if (selectStmt.Rarg != null)
            {
                AssertNoDataModifyingSubqueries(selectStmt.Rarg);
            }

            if (selectStmt.WithClause != null)
            {
                foreach (var cte in selectStmt.WithClause.Ctes)
                {
                    var cteQuery = cte.CommonTableExpr?.Ctequery;
                    if (cteQuery?.SelectStmt == null)
                    {
                        throw new InvalidOperationException(
                            "Only SELECT statements are permitted. Blocked statement detected in a WITH clause.");
                    }

                    AssertNoDataModifyingSubqueries(cteQuery.SelectStmt);
                }
            }

            if (selectStmt.FromClause == null)
            {
                return;
            }

            foreach (var fromItem in selectStmt.FromClause)
            {
                AssertNodeSelectOnly(fromItem);
            }
        }

        private static void AssertNodeSelectOnly(Node node)
        {
            if (node == null)
            {
                return;
            }

            if (node.RangeSubselect != null)
            {
                var subquery = node.RangeSubselect.Subquery;
                if (subquery?.SelectStmt == null)
                {
                    throw new InvalidOperationException(
                        "Only SELECT statements are permitted. Blocked statement detected in a subquery.");
                }

                AssertNoDataModifyingSubqueries(subquery.SelectStmt);
                return;
            }

            if (node.JoinExpr == null)
            {
                return;
            }

            AssertNodeSelectOnly(node.JoinExpr.Larg);
            AssertNodeSelectOnly(node.JoinExpr.Rarg);
        }
    }
}
