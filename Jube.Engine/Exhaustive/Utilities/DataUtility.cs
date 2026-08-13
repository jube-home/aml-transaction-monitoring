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

namespace Jube.Engine.Exhaustive.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Algorithms.Models;
    using Data.Context;
    using Data.Query;
    using Data.Reporting;
    using Data.Repository;
    using log4net;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using QueryBuilder;

    public static class Extraction
    {
        public static async Task<Tuple<double[][], double[]>> GetClassDataAsync(DbContext dbContext,
            int entityAnalysisModelId,
            string filterSql,
            string filterTokens,
            Dictionary<int, Variable> variables,
            bool mockData,
            ILog log,
            bool parserAssertSelectOnly,
            string reportConnectionString = null,
            CancellationToken token = default)
        {
            var dataList = new List<double[]>();
            var outputsList = new List<double>();
            using var postgres = new Postgres(reportConnectionString ?? dbContext.Connection.ConnectionString, log, parserAssertSelectOnly);

            foreach (var json in
                     await postgres.ExecuteReturnOnlyJsonFromArchiveSampleAsync(entityAnalysisModelId, filterSql,
                         filterTokens, 10000, mockData, token).ConfigureAwait(false))
            {
                var jObject = JObject.Parse(json);

                var row = new double[variables.Count];
                for (var i = 0; i < row.Length; i++)
                {
                    double value = 0;
                    try
                    {
                        var jToken = jObject.SelectToken(variables[i].ValueJsonPath);
                        if (jToken != null)
                        {
                            value = jToken.Value<double>();
                        }
                    }
                    catch
                    {
                        value = 0;
                    }

                    row[i] = value;
                }

                dataList.Add(row);
                outputsList.Add(1);
            }

            return new Tuple<double[][], double[]>(dataList.ToArray(), outputsList.ToArray());
        }

        public static async Task<Tuple<Dictionary<int, Variable>, double[][]>> GetSampleDataAsync(DbContext dbContext,
            int tenantRegistryId,
            int entityAnalysisModelId,
            bool mockData, CancellationToken token = default)
        {
            if (mockData)
            {
                var mockArchiveRepository = new MockArchiveRepository(dbContext);
                var jsonList =
                    await mockArchiveRepository.GetJsonByEntityAnalysisModelIdRandomLimitAsync(entityAnalysisModelId, 10000, token).ConfigureAwait(false);

                return await ProcessJsonAsync(dbContext, tenantRegistryId, entityAnalysisModelId, true, jsonList, token).ConfigureAwait(false);
            }
            else
            {
                var archiveRepository = new ArchiveRepository(dbContext);
                var jsonList =
                    await archiveRepository.GetJsonByEntityAnalysisModelIdRandomLimitAsync(entityAnalysisModelId, 10000, token).ConfigureAwait(false);

                return await ProcessJsonAsync(dbContext, tenantRegistryId, entityAnalysisModelId, false, jsonList, token).ConfigureAwait(false);
            }
        }

        public static async Task<Tuple<Dictionary<int, Variable>, double[][]>> GetSampleDataAsync(DbContext dbContext,
            int tenantRegistryId,
            int entityAnalysisModelId,
            string filterJson,
            string filterTokens,
            bool mockData,
            ILog log,
            bool parserAssertSelectOnly,
            string reportConnectionString = null,
            CancellationToken token = default)
        {
            var filterJsonRule = JsonConvert.DeserializeObject<Rule>(filterJson);
            var filterRule = await ParserJsonToSqlEntityAnalysisModel.CreateAsync(filterJsonRule, dbContext, tenantRegistryId, entityAnalysisModelId, token).ConfigureAwait(false);

            using var postgres = new Postgres(reportConnectionString ?? dbContext.Connection.ConnectionString, log, parserAssertSelectOnly);
            var jsonList = await postgres.ExecuteReturnOnlyJsonFromArchiveSampleAsync(entityAnalysisModelId,
                "NOT (" + filterRule.Sql + ")",
                filterTokens, 10000, mockData, token).ConfigureAwait(false);

            return await ProcessJsonAsync(dbContext, tenantRegistryId, entityAnalysisModelId, mockData, jsonList, token).ConfigureAwait(false);
        }

        private static async Task<Tuple<Dictionary<int, Variable>, double[][]>> ProcessJsonAsync(DbContext dbContext, int tenantRegistryId, int entityAnalysisModelId,
            bool mockData, IEnumerable<string> jsonList, CancellationToken token = default)
        {
            var variables = new Dictionary<int, Variable>();
            var headerSequence = 0;

            if (!mockData)
            {
                var getModelFieldByEntityAnalysisModelIdParseTypeIdQuery =
                    new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext, tenantRegistryId);

                var fields = await getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                    .ExecuteAsync(entityAnalysisModelId, 5, true, token).ConfigureAwait(false);

                foreach (var variable in from field in fields
                         where field.ProcessingTypeId is 3 or 5 or 7
                         select new Variable
                         {
                             Name = field.Name,
                             ProcessingTypeId = field.ProcessingTypeId,
                             ValueJsonPath = field.ValueJsonPath
                         })
                {
                    variables.Add(headerSequence, variable);
                    headerSequence += 1;
                }
            }
            else
            {
                variables.Add(headerSequence, new Variable
                {
                    Name = "Abstraction.IsChip",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.IsChip"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.IsSwipe",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.IsSwipe"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.IsManual",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.IsManual"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.CountTransactions1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountTransactions1Day"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.Authenticated",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.Authenticated"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.CountTransactionsPINDecline1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountTransactionsPINDecline1Day"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.CountTransactionsDeclined1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountTransactionsDeclined1Day"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.CountUnsafeTerminals1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountUnsafeTerminals1Day"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.CountInPerson1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountInPerson1Day"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.CountInternet1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountInternet1Day"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.ATM",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.ATM"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.CountATM1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountATM1Day"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.CountOver30SEK1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountOver30SEK1Day"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.InPerson",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.InPerson"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.TransactionAmt",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.TransactionAmt"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.SumTransactions1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.SumTransactions1Day"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.SumATMTransactions1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.SumATMTransactions1Day"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.Foreign",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.Foreign"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.DifferentCountryTransactions1Week",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.DifferentCountryTransactions1Week"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.DifferentMerchantTypes1Week",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.DifferentMerchantTypes1Week"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.DifferentDeclineReasons1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.DifferentDeclineReasons1Day"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.DifferentCities1Week",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.DifferentCities1Week"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.DifferentCities1Week",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.DifferentCities1Week"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.CountSameMerchantUsedBefore1Week",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountSameMerchantUsedBefore1Week"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.HasBeenAbroad",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.HasBeenAbroad"
                });

                variables.Add(headerSequence += 1, new Variable
                {
                    Name = "Abstraction.CashTransaction",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CashTransaction"
                });

                variables.Add(headerSequence + 1, new Variable
                {
                    Name = "Abstraction.HighRiskCountry",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.HighRiskCountry"
                });
            }

            var rows = new List<double[]>();
            foreach (var json in jsonList)
            {
                var jObject = JObject.Parse(json);

                var row = new double[variables.Count];
                for (var i = 0; i < row.Length; i++)
                {
                    double value = 0;
                    try
                    {
                        var jToken = jObject.SelectToken(variables[i].ValueJsonPath);
                        if (jToken != null)
                        {
                            value = jToken.Value<double>();
                        }
                    }
                    catch
                    {
                        value = 0;
                    }

                    row[i] = value;
                }

                rows.Add(row);
            }

            return new Tuple<Dictionary<int, Variable>, double[][]>(variables, rows.ToArray());
        }
    }
}
