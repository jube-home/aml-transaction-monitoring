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
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using DynamicResultsSchema;
    using log4net;
    using Newtonsoft.Json.Linq;
    using Reporting;

    public class GetCaseJournalQuery(DbContext dbContext, string user, ILog log, bool parserAssertSelectOnly, string reportConnectionString = null)
    {
        public async Task<GetCaseJournalQueryResultDto> ExecuteAsync(string key, string keyValue, Guid caseWorkflowGuid,
            int limit,
            int activationRuleCount, double responseElevation, CancellationToken token = default)
        {
            var caseWorkflowXPathByCaseWorkflowIdQuery =
                new GetCaseWorkflowXPathByCaseWorkflowIdQuery(dbContext, user);

            var xPaths = (await caseWorkflowXPathByCaseWorkflowIdQuery
                .ExecuteAsync(caseWorkflowGuid, token)).ToList();

            const string sql = "select '(' || \"ActivationRuleCount\" || ') ' || r.\"Name\" as \"Activation\", a.* " +
                               "from \"Archive\" a " +
                               "inner join \"EntityAnalysisModel\" m on m.\"Id\" = a.\"EntityAnalysisModelId\" " +
                               "inner join \"TenantRegistry\" t on t.\"Id\" = m.\"TenantRegistryId\" " +
                               "left join (select \"Id\", \"Name\" from \"EntityAnalysisModelActivationRule\") r on r.\"Id\" = a.\"EntityAnalysisModelActivationRuleId\" " +
                               "where exists ( " +
                               "    select 1 from \"UserInTenant\" u " +
                               "    where u.\"TenantRegistryId\" = t.\"Id\" " +
                               "    and u.\"User\" = (@1) " +
                               ") " +
                               "and a.\"Json\" -> 'payload' ->> (@2) = (@3) " +
                               "and a.\"ResponseElevation\" >= (@4) " +
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

            using var postgres = new Postgres(reportConnectionString ?? dbContext.Connection.ConnectionString, log, parserAssertSelectOnly);

            await postgres.PrepareAsync(sql, tokens, token).ConfigureAwait(false);

            var records = (await postgres.ExecuteByOrderedParametersAsync(sql, tokens, token).ConfigureAwait(false)).ToList();

            var schema = new Dictionary<string, string>
            {
                {
                    "Id", "number"
                },
                {
                    "Activation", "string"
                },
                {
                    "EntityAnalysisModelInstanceEntryGuid", "string"
                },
                {
                    "ReferenceDate", "date"
                },
                {
                    "CreatedDate", "date"
                },
                {
                    "ResponseElevation", "number"
                },
                {
                    "EntryKeyValue", "string"
                },
                {
                    "Tag", "array"
                }
            };

            foreach (var xPath in xPaths)
            {
                schema.TryAdd(xPath.Name, "string");
            }

            var rows = new List<Dictionary<string, object>>();

            foreach (var record in records)
            {
                var json = JObject.Parse(record["Json"].ToString() ?? String.Empty);

                var cellFormats = new List<GetCaseJournalQueryCellFormatDto>();

                var value = new Dictionary<string, object>
                {
                    {
                        "Id", DynamicResultSchema.NormaliseRecordValue(record["Id"])
                    },
                    {
                        "Activation", DynamicResultSchema.NormaliseRecordValue(record["Activation"])
                    },
                    {
                        "EntityAnalysisModelInstanceEntryGuid", DynamicResultSchema.NormaliseRecordValue(record["EntityAnalysisModelInstanceEntryGuid"])
                    },
                    {
                        "ReferenceDate", DynamicResultSchema.NormaliseRecordValue(record["ReferenceDate"])
                    },
                    {
                        "CreatedDate", DynamicResultSchema.NormaliseRecordValue(record["CreatedDate"])
                    },
                    {
                        "ResponseElevation", DynamicResultSchema.NormaliseRecordValue(record["ResponseElevation"])
                    },
                    {
                        "EntryKeyValue", DynamicResultSchema.NormaliseRecordValue(record["EntryKeyValue"])
                    }
                };

                foreach (var xPath in xPaths)
                {
                    try
                    {
                        var jToken = json.SelectToken(xPath.XPath);
                        if (jToken != null)
                        {
                            object typedValue = jToken.Type switch
                            {
                                JTokenType.Integer => jToken.Value<long>(),
                                JTokenType.Float => jToken.Value<double>(),
                                JTokenType.Boolean => jToken.Value<bool>(),
                                JTokenType.Null => null,
                                JTokenType.Date => new DateTimeOffset(jToken.Value<DateTime>().ToUniversalTime(), TimeSpan.Zero),
                                _ => jToken.Value<string>()
                            };

                            if (typedValue != null)
                            {
                                schema[xPath.Name] = DynamicResultSchema.ClrTypeToSchemaType(typedValue);
                            }

                            if (value.TryAdd(xPath.Name, typedValue))
                            {
                                if (xPath.ConditionalRegularExpressionFormatting)
                                {
                                    try
                                    {
                                        var valueToken = jToken.Value<string>();
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
                                        // ignored
                                    }
                                }
                            }
                        }
                        else
                        {
                            value.Add(xPath.Name, null);
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

                value.Add("Tag", json["tag"] is JArray ? json["tag"] : new JArray());
                rows.Add(value);
            }

            return new GetCaseJournalQueryResultDto
            {
                Schema = schema,
                Rows = rows
            };
        }

        public class GetCaseJournalQueryResultDto
        {
            public Dictionary<string, string> Schema { get; set; }
            public List<Dictionary<string, object>> Rows { get; set; }
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
