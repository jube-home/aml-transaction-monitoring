﻿/* Copyright (C) 2022-present Jube Holdings Limited.
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

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using FluentMigrator.Runner;
using FluentValidation;
using FluentValidation.Results;
using Jube.App.Code;
using Jube.App.Code.QueryBuilder;
using Jube.App.Dto;
using Jube.App.Validators;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Reporting;
using Jube.Data.Repository;
using Jube.Engine.Helpers;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Jube.App.Controllers.Session
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class SessionCaseSearchCompiledSqlController : Controller
    {
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly SessionCaseSearchCompiledSqlRepository repository;
        private readonly IValidator<SessionCaseSearchCompiledSqlDto> validator;
        private readonly string userName;

        public SessionCaseSearchCompiledSqlController(ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            this.log = log;

            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            permissionValidation = new PermissionValidation(dbContext, userName);


            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<SessionCaseSearchCompiledSql, SessionCaseSearchCompiledSqlDto>();
                cfg.CreateMap<SessionCaseSearchCompiledSqlDto, SessionCaseSearchCompiledSql>();
                cfg.CreateMap<List<SessionCaseSearchCompiledSql>, List<SessionCaseSearchCompiledSqlDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            mapper = new Mapper(config);
            repository = new SessionCaseSearchCompiledSqlRepository(dbContext, userName);
            validator = new SessionCaseUserDtoValidator();
            this.dynamicEnvironment = dynamicEnvironment;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dbContext.Close();
                dbContext.Dispose();
            }

            base.Dispose(disposing);
        }

        [HttpGet("ByGuid/{guid:Guid}")]
        public async Task<ActionResult<List<dynamic>>> ExecuteByGuidAsync(Guid guid)
        {
            try
            {
                if (!permissionValidation.Validate(new[] {1})) return Forbid();

                var modelCompiled = repository.GetByGuid(guid);

                var postgres = new Postgres(dynamicEnvironment.AppSettings("ConnectionString"));
                var tokens = JsonConvert.DeserializeObject<List<object>>(modelCompiled.FilterTokens);

                var sw = new StopWatch();
                sw.Start();

                var value = await postgres.ExecuteByOrderedParametersAsync(modelCompiled.SelectSqlSearch + " "
                    + modelCompiled.WhereSql
                    + " " + modelCompiled.OrderSql, tokens);

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

                return Ok(value);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByLast")]
        public ActionResult<SessionCaseSearchCompiledSqlDto> ExecuteByLast()
        {
            try
            {
                if (!permissionValidation.Validate(new[] {1})) return Forbid();

                return Ok(mapper.Map<SessionCaseSearchCompiledSqlDto>(repository.GetByLast()));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(SessionCaseSearchCompiledSqlDto), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int) HttpStatusCode.BadRequest)]
        public async Task<ActionResult<SessionCaseSearchCompiledSqlDto>> Create(
            [FromBody] SessionCaseSearchCompiledSqlDto model)
        {
            try
            {
                if (!permissionValidation.Validate(new[] {1}, true)) return Forbid();

                var results = await validator.ValidateAsync(model);
                if (!results.IsValid) return BadRequest(results);

                var insert = mapper.Map<SessionCaseSearchCompiledSql>(model);

                var filterJsonRule = JsonConvert.DeserializeObject<Rule>(model.FilterJson);
                var filterRule = new Code.QueryBuilder.Parser(filterJsonRule,dbContext,model.CaseWorkflowId,userName);
                var selectTokensRule = JsonConvert.DeserializeObject<Rule>(model.SelectJson);
                var selectRule = new Code.QueryBuilder.Parser(selectTokensRule,dbContext,model.CaseWorkflowId,userName);
                
                insert.SelectSqlDisplay = "select \"Case\".\"Id\" as \"Id\"," +
                                          "\"Case\".\"EntityAnalysisModelInstanceEntryGuid\" as \"EntityAnalysisModelInstanceEntryGuid\"," +
                                          "\"Case\".\"DiaryDate\" as \"DiaryDate\"," +
                                          "\"Case\".\"CaseWorkflowId\" as \"CaseWorkflowId\"," +
                                          "\"Case\".\"CaseWorkflowStatusId\" as \"CaseWorkflowStatusId\"," +
                                          "\"Case\".\"CreatedDate\" as \"CreatedDate\"," +
                                          "\"Case\".\"Locked\" as \"Locked\"," +
                                          "\"Case\".\"LockedUser\" as \"LockedUser\"," +
                                          "\"Case\".\"LockedDate\" as \"LockedDate\"," +
                                          "\"Case\".\"ClosedStatusId\" as \"ClosedStatusId\"," +
                                          "\"Case\".\"ClosedDate\" as \"ClosedDate\"," +
                                          "\"Case\".\"ClosedUser\" as \"ClosedUser\"," +
                                          "\"Case\".\"CaseKey\" as \"CaseKey\"," +
                                          "\"Case\".\"Diary\" as \"Diary\"," +
                                          "\"Case\".\"DiaryUser\" as \"DiaryUser\"," +
                                          "\"Case\".\"Rating\" as \"Rating\"," +
                                          "\"Case\".\"CaseKeyValue\" as \"CaseKeyValue\"," +
                                          "\"Case\".\"ClosedStatusMigrationDate\" as \"ClosedStatusMigrationDate\"," +
                                          "\"Case\".\"Json\" as \"Json\"," +
                                          "\"CaseWorkflow\".\"EnableVisualisation\" as \"EnableVisualisation\"," +
                                          "\"CaseWorkflow\".\"VisualisationRegistryId\" as \"VisualisationRegistryId\"," +
                                          "\"CaseWorkflowStatus\".\"ForeColor\" as \"ForeColor\"," +
                                          "\"CaseWorkflowStatus\".\"BackColor\" as \"BackColor\" ";

                var columnsSelect = new List<string>
                {
                    "\"Case\".\"Id\" as \"Id\"",
                    "\"CaseWorkflowStatus\".\"BackColor\" as \"BackColor\"",
                    "\"CaseWorkflowStatus\".\"ForeColor\" as \"ForeColor\""
                };

                var columnsOrder = new List<string>();
                foreach (var rule in selectRule.Rules)
                {
                    if (rule.Field == null || rule.Id == null) continue;

                    var convertedColumnSelectField = rule.Id switch
                    {
                        "Locked" => $"case when {rule.Field} = 1 then 'Yes' else 'No' end",
                        "Diary" => $"case when {rule.Field} = 1 then 'Yes' else 'No' end",
                        "ClosedStatusId" => "case " + $"when {rule.Field} = 0 then 'Open' " +
                                            $"when {rule.Field} = 1 then 'Suspend Open' " +
                                            $"when {rule.Field} = 2 then 'Suspend Closed' " +
                                            $"when {rule.Field} = 3 then 'Closed' " +
                                            $"when {rule.Field} = 4 then 'Suspend Bypass' " + "end",
                        "Priority" => "case " + $"when {rule.Field} = 1 then 'Ultra High' " +
                                      $"when {rule.Field} = 2 then 'High' " +
                                      $"when {rule.Field} = 3 then 'Normal' " +
                                      $"when {rule.Field} = 4 then 'Low' " +
                                      $"when {rule.Field} = 5 then 'Ultra Low' " + "end",
                        _ => rule.Field
                    };

                    columnsOrder.Add(rule.Field + " " + rule.Value);

                    if (rule.Id == "Id") continue;

                    if (rule.Id.Contains('.'))
                        columnsSelect.Add(convertedColumnSelectField
                                          + " as \"" + rule.Id.Replace(".", "") + "\"");
                    else
                        columnsSelect.Add(convertedColumnSelectField
                                          + " as \"" + rule.Id + "\"");
                }

                if (filterRule.Tokens == null)
                    return Ok(mapper.Map<SessionCaseSearchCompiledSqlDto>(repository.Insert(insert)));

                filterRule.Tokens.Add(insert.CaseWorkflowId);
                var positionCaseWorkflowId = filterRule.Tokens.Count;

                filterRule.Tokens.Add(userName);
                var positionUser = filterRule.Tokens.Count;

                insert.SelectSqlSearch = "select " + string.Join(",", columnsSelect);

                insert.WhereSql = "from \"Case\",\"CaseWorkflow\",\"EntityAnalysisModel\",\"TenantRegistry\"," +
                                  "\"CaseWorkflowStatus\",\"UserInTenant\"" +
                                  " where \"EntityAnalysisModel\".\"Id\" = \"CaseWorkflow\".\"EntityAnalysisModelId\"" +
                                  " and \"EntityAnalysisModel\".\"TenantRegistryId\" = \"TenantRegistry\".\"Id\"" +
                                  " and \"UserInTenant\".\"TenantRegistryId\" = \"TenantRegistry\".\"Id\"" +
                                  " and \"Case\".\"CaseWorkflowId\" = \"CaseWorkflow\".\"Id\"" +
                                  " and (\"Case\".\"CaseWorkflowStatusId\" = \"CaseWorkflowStatus\".\"Id\"" +
                                  " and \"Case\".\"CaseWorkflowId\" = \"CaseWorkflowStatus\".\"CaseWorkflowId\"" +
                                  " and (\"CaseWorkflowStatus\".\"Deleted\" = 0" +
                                  " or \"CaseWorkflowStatus\".\"Deleted\" IS null) ) and " + filterRule.Sql +
                                  " and \"CaseWorkflow\".\"Id\" = (@" + positionCaseWorkflowId + ")" +
                                  " and \"UserInTenant\".\"User\" = (@" + positionUser + ")";

                insert.OrderSql = "order by " + string.Join(",", columnsOrder);

                insert.FilterTokens = JsonConvert.SerializeObject(filterRule.Tokens);

                try
                {
                    var postgres = new Postgres(dynamicEnvironment.AppSettings("ConnectionString"));
                    await postgres.PrepareAsync(insert.SelectSqlSearch + " " + insert.WhereSql + " " + insert.OrderSql,
                        filterRule.Tokens);
                    insert.Prepared = 1;
                }
                catch (Exception e)
                {
                    insert.Prepared = 0;
                    insert.Error = e.Message;
                }

                return Ok(mapper.Map<SessionCaseSearchCompiledSqlDto>(repository.Insert(insert)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}