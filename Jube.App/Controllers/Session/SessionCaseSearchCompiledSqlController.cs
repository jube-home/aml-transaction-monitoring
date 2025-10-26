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

namespace Jube.App.Controllers.Session
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using AutoMapper;
    using Code;
    using Data.Context;
    using Data.Poco;
    using Data.Reporting;
    using Data.Repository;
    using Dto;
    using DynamicEnvironment;
    using FluentMigrator.Runner;
    using FluentValidation;
    using FluentValidation.Results;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using Validators;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class SessionCaseSearchCompiledSqlController : Controller
    {
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly string userName;
        private readonly IValidator<SessionCaseSearchCompiledSqlDto> validator;

        public SessionCaseSearchCompiledSqlController(ILog log,
            DynamicEnvironment dynamicEnvironment,
            IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

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

            validator = new SessionCaseSearchCompiledSqlDtoValidator();
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
                if (!permissionValidation.Validate(new[]
                    {
                        1
                    }))
                {
                    return Forbid();
                }

                var repository = new SessionCaseSearchCompiledSqlRepository(dbContext, userName);

                var modelCompiled = repository.GetByGuid(guid);
                if (modelCompiled == null)
                {
                    return NotFound();
                }

                await CheckRebuild(modelCompiled).ConfigureAwait(false);

                var postgres = new Postgres(dynamicEnvironment.AppSettings("ConnectionString"));
                var tokens = JsonConvert.DeserializeObject<List<object>>(modelCompiled.FilterTokens);

                var sw = new StopWatch();
                sw.Start();

                var value = await postgres.ExecuteByOrderedParametersAsync(modelCompiled.SelectSqlSearch + " "
                                                                                                         + modelCompiled.WhereSql
                                                                                                         + " " + modelCompiled.OrderSql, tokens).ConfigureAwait(false);

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

        private async Task<SessionCaseSearchCompiledSql> CheckRebuild(SessionCaseSearchCompiledSql modelCompiled)
        {
            if (modelCompiled.Rebuild == 1 && modelCompiled.RebuildDate != null)
            {
                return await CompileSql.Compile(dbContext, modelCompiled, userName).ConfigureAwait(false);
            }

            return modelCompiled;
        }

        [HttpGet("ByLast")]
        public async Task<ActionResult<SessionCaseSearchCompiledSqlDto>> ExecuteByLast()
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        1
                    }))
                {
                    return Forbid();
                }

                var repository = new SessionCaseSearchCompiledSqlRepository(dbContext, userName);

                var modelCompiled = repository.GetByLast();
                if (modelCompiled == null)
                {
                    return new SessionCaseSearchCompiledSqlDto
                    {
                        NotFound = true
                    };
                }

                modelCompiled = await CheckRebuild(modelCompiled).ConfigureAwait(false);

                return Ok(mapper.Map<SessionCaseSearchCompiledSqlDto>(modelCompiled));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(SessionCaseSearchCompiledSqlDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<SessionCaseSearchCompiledSqlDto>> Create(
            [FromBody] SessionCaseSearchCompiledSqlDto model)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        1
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    return BadRequest(results);
                }

                return Ok(mapper.Map<SessionCaseSearchCompiledSqlDto>(await CompileSql.Compile(dbContext,
                    mapper.Map<SessionCaseSearchCompiledSql>(model),
                    userName).ConfigureAwait(false)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
