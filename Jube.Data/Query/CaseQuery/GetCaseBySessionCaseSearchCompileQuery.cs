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
    using System.Threading.Tasks;
    using Context;
    using Dto;
    using Extension;
    using FluentMigrator.Runner;
    using Newtonsoft.Json;
    using Poco;
    using Reporting;
    using Repository;

    public class GetCaseBySessionCaseSearchCompileQuery
    {
        private readonly DbContext dbContext;
        private readonly ProcessCaseQuery processCaseQuery;
        private readonly string userName;

        public GetCaseBySessionCaseSearchCompileQuery(DbContext dbContext, string user)
        {
            this.dbContext = dbContext;
            userName = user;
            processCaseQuery = new ProcessCaseQuery(this.dbContext, userName);
        }

        public async Task<CaseQueryDto> ExecuteAsync(Guid guid)
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

            var postgres = new Postgres(dbContext.ConnectionString);

            var value = await postgres.ExecuteByOrderedParametersAsync(modelCompiled.SelectSqlDisplay + " "
                                                                                                      + modelCompiled.WhereSql
                                                                                                      + " " + modelCompiled.OrderSql + " limit 1", tokens).ConfigureAwait(false);
            sw.Stop();

            var modelInsert = new SessionCaseSearchCompiledSqlExecution
            {
                SessionCaseSearchCompiledSqlId = modelCompiled.Id,
                Records = value.Count,
                ResponseTime = sw.ElapsedTime().Milliseconds
            };

            var sessionCaseSearchCompiledSqlExecutionRepository =
                new SessionCaseSearchCompiledSqlExecutionRepository(dbContext, userName);

            sessionCaseSearchCompiledSqlExecutionRepository.Insert(modelInsert);

            var caseQueryDto = new CaseQueryDto();

            if (value.Count <= 0)
            {
                throw new KeyNotFoundException();
            }

            if (value[0].ContainsKey("Id"))
            {
                caseQueryDto.Id = value[0]["Id"]?.AsInt() ?? 0;
            }
            else
            {
                caseQueryDto.Id = 0;
            }

            if (value[0].ContainsKey("EntityAnalysisModelInstanceEntryGuid"))
            {
                caseQueryDto.EntityAnalysisModelInstanceEntryGuid
                    = value[0]["EntityAnalysisModelInstanceEntryGuid"]?.AsGuid() ?? Guid.Empty;
            }
            else
            {
                caseQueryDto.EntityAnalysisModelInstanceEntryGuid = Guid.Empty;
            }

            if (value[0].ContainsKey("DiaryDate"))
            {
                caseQueryDto.DiaryDate
                    = value[0]["DiaryDate"]?.AsDateTime() ?? default(DateTime);
            }
            else
            {
                caseQueryDto.DiaryDate = default(DateTime);
            }

            if (value[0].ContainsKey("CaseWorkflowGuid"))
            {
                caseQueryDto.CaseWorkflowGuid
                    = value[0]["CaseWorkflowGuid"]?.AsGuid() ?? Guid.Empty;
            }
            else
            {
                caseQueryDto.CaseWorkflowGuid = Guid.Empty;
            }

            caseQueryDto.CaseWorkflowStatusGuid = value[0].ContainsKey("CaseWorkflowStatusGuid")
                ? value[0]["CaseWorkflowStatusGuid"].AsGuid()
                : Guid.Empty;

            caseQueryDto.CreatedDate = value[0].ContainsKey("CreatedDate")
                ? value[0]["CreatedDate"].AsDateTime()
                : default(DateTime);

            if (value[0].ContainsKey("Locked"))
            {
                caseQueryDto.Locked
                    = value[0]["Locked"]?.AsShort() == 1;
            }
            else
            {
                caseQueryDto.Locked = false;
            }

            caseQueryDto.LockedUser =
                value[0].ContainsKey("LockedUser") ? value[0]["LockedUser"]?.AsString() : null;

            caseQueryDto.LockedDate = value[0].ContainsKey("LockedDate")
                ? value[0]["LockedDate"].AsDateTime()
                : default(DateTime);

            if (value[0].ContainsKey("ClosedStatusId"))
            {
                caseQueryDto.ClosedStatusId
                    = value[0]["ClosedStatusId"]?.AsShort() ?? 0;
            }
            else
            {
                caseQueryDto.ClosedStatusId = 0;
            }

            caseQueryDto.ClosedUser =
                value[0].ContainsKey("ClosedUser") ? value[0]["ClosedUser"]?.AsString() : null;

            caseQueryDto.CaseKey = value[0].ContainsKey("CaseKey") ? value[0]["CaseKey"]?.AsString() : null;

            caseQueryDto.CaseKey = !value[0].ContainsKey("CaseKey") ? null : value[0]["CaseKey"]?.AsString();

            if (value[0].ContainsKey("Diary"))
            {
                caseQueryDto.Diary
                    = value[0]["Diary"]?.AsShort() == 1;
            }
            else
            {
                caseQueryDto.Diary = false;
            }

            caseQueryDto.DiaryUser =
                value[0].ContainsKey("DiaryUser") ? value[0]["DiaryUser"]?.AsString() : null;

            if (value[0].ContainsKey("Rating"))
            {
                caseQueryDto.Rating
                    = value[0]["Rating"]?.AsShort() ?? 0;
            }
            else
            {
                caseQueryDto.Rating = 0;
            }

            caseQueryDto.CaseKeyValue = value[0].ContainsKey("CaseKeyValue")
                ? value[0]["CaseKeyValue"]?.AsString()
                : null;

            if (value[0].ContainsKey("LastClosedStatus"))
            {
                caseQueryDto.LastClosedStatus
                    = value[0]["LastClosedStatus"]?.AsShort() ?? 0;
            }
            else
            {
                caseQueryDto.LastClosedStatus = 0;
            }

            if (value[0].ContainsKey("EnableVisualisation"))
            {
                caseQueryDto.EnableVisualisation
                    = value[0]["EnableVisualisation"]?.AsShort() == 1;
            }
            else
            {
                caseQueryDto.EnableVisualisation = false;
            }

            if (value[0].ContainsKey("VisualisationRegistryGuid"))
            {
                caseQueryDto.VisualisationRegistryGuid
                    = value[0]["VisualisationRegistryGuid"]?.AsGuid() ?? Guid.Empty;
            }
            else
            {
                caseQueryDto.VisualisationRegistryGuid = Guid.Empty;
            }

            if (value[0].ContainsKey("ClosedStatusMigrationDate"))
            {
                caseQueryDto.ClosedStatusMigrationDate
                    = value[0]["ClosedStatusMigrationDate"]?.AsDateTime() ?? default(DateTime);
            }
            else
            {
                caseQueryDto.ClosedStatusMigrationDate = default(DateTime);
            }

            caseQueryDto.ForeColor =
                value[0].ContainsKey("ForeColor") ? value[0]["ForeColor"]?.AsString() : null;

            caseQueryDto.BackColor =
                value[0].ContainsKey("BackColor") ? value[0]["BackColor"]?.AsString() : null;

            caseQueryDto.Json = value[0].ContainsKey("Json") ? value[0]["Json"]?.AsString() : null;

            return processCaseQuery.Process(caseQueryDto);
        }
    }
}
