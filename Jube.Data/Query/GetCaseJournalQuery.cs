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
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Context;
    using Newtonsoft.Json.Linq;
    using Reporting;

    public class GetCaseJournalQuery(DbContext dbContext, string user)
    {
        private readonly string connectionString = dbContext.ConnectionString;

        public async Task<List<Dictionary<string, object>>> ExecuteAsync(string key, string keyValue, Guid caseWorkflowGuid,
            int limit,
            int activationRuleCount, double responseElevation)
        {
            var values = new List<Dictionary<string, object>>();

            var caseWorkflowXPathByCaseWorkflowIdQuery =
                new GetCaseWorkflowXPathByCaseWorkflowIdQuery(dbContext, user);

            var xPaths = caseWorkflowXPathByCaseWorkflowIdQuery
                .Execute(caseWorkflowGuid).ToList();

            const string sql = "select '(' || \"ActivationRuleCount\" || ') ' || r.\"Name\" as \"Activation\", a.* " +
                               "from \"Archive\" a " +
                               "inner join \"EntityAnalysisModel\" m on m.\"Id\" = a.\"EntityAnalysisModelId\" " +
                               "inner join \"TenantRegistry\" t on t.\"Id\" = m.\"TenantRegistryId\" " +
                               "inner join \"UserInTenant\" u on u.\"TenantRegistryId\" = t.\"Id\" " +
                               "left join (select \"Id\", \"Name\" from \"EntityAnalysisModelActivationRule\") r on r.\"Id\" = a.\"EntityAnalysisModelActivationRuleId\" " +
                               "where u.\"User\" = (@1) " +
                               "and a.\"Json\" -> 'payload' ->> (@2) = (@3) " +
                               "and a.\"ResponseElevation\" >= (@4)" +
                               "and a.\"ActivationRuleCount\" >= (@5) " +
                               "order by a.\"Id\" desc " +
                               "limit (@6)";

            var tokens = new List<object>
            {
                user,
                key,
                keyValue,
                responseElevation,
                activationRuleCount,
                limit
            };

            var postgres = new Postgres(connectionString);

            await postgres.PrepareAsync(sql, tokens).ConfigureAwait(false);

            foreach (var record in await postgres.ExecuteByOrderedParametersAsync(sql, tokens).ConfigureAwait(false))
            {
                var json = JObject.Parse(record["Json"].ToString() ?? String.Empty);

                var cellFormats = new List<GetCaseJournalQueryCellFormatDto>();

                var value = new Dictionary<string, object>
                {
                    {
                        "Id", record["Id"]
                    },
                    {
                        "Activation", record["Activation"]
                    },
                    {
                        "EntityAnalysisModelInstanceEntryGuid", record["EntityAnalysisModelInstanceEntryGuid"]
                    },
                    {
                        "ReferenceDate", record["ReferenceDate"]
                    },
                    {
                        "CreatedDate", record["CreatedDate"]
                    },
                    {
                        "ResponseElevation", record["ResponseElevation"]
                    },
                    {
                        "EntryKeyValue", record["EntryKeyValue"]
                    }
                };

                foreach (var xPath in xPaths)
                {
                    try
                    {
                        var jToken = json.SelectToken(xPath.XPath);
                        if (jToken != null)
                        {
                            var valueToken = jToken.Value<string>();

                            if (value.TryAdd(xPath.Name, valueToken))
                            {
                                if (xPath.ConditionalRegularExpressionFormatting)
                                {
                                    try
                                    {
                                        var regex = new Regex(xPath.RegularExpression);

                                        var match = regex.Match(valueToken);

                                        if (match.Success)
                                        {
                                            cellFormats.Add(new GetCaseJournalQueryCellFormatDto
                                            {
                                                CellFormatKey = xPath.Name,
                                                CellFormatBackColor = xPath.ConditionalFormatBackColor,
                                                CellFormatForeColor = xPath.ConditionalFormatForeColor,
                                                CellFormatForeRow = xPath.ForeRowColorScope,
                                                CellFormatBackRow = xPath.BackRowColorScope
                                            });
                                        }
                                    }
                                    catch
                                    {
                                        //ignored
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                    if (xPath.BoldLineMatched)
                    {
                        value.TryAdd("BoldLine", new GetCaseJournalQueryBoldLineDto
                        {
                            BoldLineKey = xPath.Name,
                            BoldLineFormatBackColor = xPath.BoldLineFormatBackColor,
                            BoldLineFormatForeColor = xPath.BoldLineFormatForeColor
                        });
                    }
                }

                if (cellFormats.Count > 0)
                {
                    value.Add("CellFormat", cellFormats);
                }

                values.Add(value);
            }

            return values;
        }

        private class GetCaseJournalQueryBoldLineDto
        {
            public string BoldLineKey { get; set; }
            public string BoldLineFormatForeColor { get; set; }
            public string BoldLineFormatBackColor { get; set; }
        }

        private class GetCaseJournalQueryCellFormatDto
        {
            public string CellFormatKey { get; set; }
            public string CellFormatBackColor { get; set; }
            public string CellFormatForeColor { get; set; }
            public bool CellFormatForeRow { get; set; }
            public bool CellFormatBackRow { get; set; }
        }
    }
}
