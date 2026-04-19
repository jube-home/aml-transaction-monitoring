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

namespace Jube.Data.Query.CaseQuery
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Dto;
    using Extension;
    using FluentMigrator.Runner;
    using log4net;
    using Newtonsoft.Json;
    using Poco;
    using Reporting;
    using Repository;

    public class GetCaseBySessionCaseSearchCompileQuery
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly ProcessCaseQuery processCaseQuery;
        private readonly string reportConnectionString;
        private readonly string userName;

        public GetCaseBySessionCaseSearchCompileQuery(DbContext dbContext, string user, ILog log, string reportConnectionString = null)
        {
            this.dbContext = dbContext;
            userName = user;
            this.reportConnectionString = reportConnectionString ?? dbContext.Connection.ConnectionString;
            this.log = log;
            processCaseQuery = new ProcessCaseQuery(this.dbContext, userName);
        }

        public async Task<CaseQueryDto> ExecuteAsync(Guid guid, CancellationToken token = default)
        {
            var sessionCaseSearchCompiledSqlRepository =
                new SessionCaseSearchCompiledSqlRepository(dbContext, userName);

            var modelCompiled = sessionCaseSearchCompiledSqlRepository.GetByGuid(guid);

            if (modelCompiled.Guid == Guid.Empty)
            {
                throw new KeyNotFoundException();
            }

            var tokens = JsonConvert.DeserializeObject<List<object>>(modelCompiled.FilterTokens);

            var sw = new StopWatch();
            sw.Start();

            using var postgres = new Postgres(reportConnectionString, log);

            var caseId = await
                postgres.ExecuteScalarIdAsync("select \"Case\".\"Id\""
                                              + " "
                                              + modelCompiled.WhereSql 
                                              + " and ((\"Case\".\"Locked\" = 0 or \"Case\".\"Locked\" is null)" +
                                              " or (\"Case\".\"Locked\" = 1 and \"Case\".\"LockedUser\" = (@3)))"
                                              + " " + modelCompiled.OrderSql + " limit 1", tokens, token).ConfigureAwait(false);

            var sessionCaseSearchCompiledSqlExecutionRepository = new SessionCaseSearchCompiledSqlExecutionRepository(dbContext, userName);

            if (caseId == null)
            {
                var modelInsertNotFound = new SessionCaseSearchCompiledSqlExecution
                {
                    SessionCaseSearchCompiledSqlId = modelCompiled.Id,
                    Records = 1,
                    ResponseTime = sw.ElapsedTime().Milliseconds
                };

                await sessionCaseSearchCompiledSqlExecutionRepository.InsertAsync(modelInsertNotFound, token);

                throw new KeyNotFoundException();
            }

            var getCaseByIdQuery = new GetCaseByIdQuery(dbContext, userName);
            var caseQueryDto = await getCaseByIdQuery.ExecuteAsync(caseId.Value, token);

            sw.Stop();

            var modelInsertFound = new SessionCaseSearchCompiledSqlExecution
            {
                SessionCaseSearchCompiledSqlId = modelCompiled.Id,
                Records = 1,
                ResponseTime = sw.ElapsedTime().Milliseconds
            };

            await sessionCaseSearchCompiledSqlExecutionRepository.InsertAsync(modelInsertFound, token);

            return await processCaseQuery.ProcessAsync(caseQueryDto, token);
        }
    }
}
