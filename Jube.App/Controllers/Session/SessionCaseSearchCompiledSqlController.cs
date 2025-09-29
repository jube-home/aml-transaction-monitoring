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

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using FluentMigrator.Runner;
using FluentValidation;
using FluentValidation.Results;
using Jube.App.Code;
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
        private readonly DbContext _dbContext;
        private readonly DynamicEnvironment.DynamicEnvironment _dynamicEnvironment;
        private readonly ILog _log;
        private readonly IMapper _mapper;
        private readonly PermissionValidation _permissionValidation;
        private readonly string _userName;
        private readonly IValidator<SessionCaseSearchCompiledSqlDto> _validator;

        public SessionCaseSearchCompiledSqlController(ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
                _userName = httpContextAccessor.HttpContext.User.Identity.Name;
            _log = log;

            _dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            _permissionValidation = new PermissionValidation(_dbContext, _userName);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<SessionCaseSearchCompiledSql, SessionCaseSearchCompiledSqlDto>();
                cfg.CreateMap<SessionCaseSearchCompiledSqlDto, SessionCaseSearchCompiledSql>();
                cfg.CreateMap<List<SessionCaseSearchCompiledSql>, List<SessionCaseSearchCompiledSqlDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            _mapper = new Mapper(config);

            _validator = new SessionCaseSearchCompiledSqlDtoValidator();
            _dynamicEnvironment = dynamicEnvironment;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext.Close();
                _dbContext.Dispose();
            }

            base.Dispose(disposing);
        }

        [HttpGet("ByGuid/{guid:Guid}")]
        public async Task<ActionResult<List<dynamic>>> ExecuteByGuidAsync(Guid guid)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 1 })) return Forbid();

                var repository = new SessionCaseSearchCompiledSqlRepository(_dbContext, _userName);

                var modelCompiled = repository.GetByGuid(guid);
                if (modelCompiled == null) return NotFound();
                await CheckRebuild(modelCompiled).ConfigureAwait(false);

                var postgres = new Postgres(_dynamicEnvironment.AppSettings("ConnectionString"));
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
                    new SessionCaseSearchCompiledSqlExecutionRepository(_dbContext, _userName);

                sessionCaseSearchCompiledSqlExecutionRepository.Insert(modelInsert);

                return Ok(value);
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }

        private async Task<SessionCaseSearchCompiledSql> CheckRebuild(SessionCaseSearchCompiledSql modelCompiled)
        {
            if (modelCompiled.Rebuild == 1 && modelCompiled.RebuildDate != null)
                return await CompileSql.Compile(_dbContext, modelCompiled, _userName).ConfigureAwait(false);
            return modelCompiled;
        }

        [HttpGet("ByLast")]
        public async Task<ActionResult<SessionCaseSearchCompiledSqlDto>> ExecuteByLast()
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 1 })) return Forbid();

                var repository = new SessionCaseSearchCompiledSqlRepository(_dbContext, _userName);

                var modelCompiled = repository.GetByLast();
                if (modelCompiled == null) return new SessionCaseSearchCompiledSqlDto { NotFound = true };
                modelCompiled = await CheckRebuild(modelCompiled).ConfigureAwait(false);

                return Ok(_mapper.Map<SessionCaseSearchCompiledSqlDto>(modelCompiled));
            }
            catch (Exception e)
            {
                _log.Error(e);
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
                if (!_permissionValidation.Validate(new[] { 1 }, true)) return Forbid();

                var results = await _validator.ValidateAsync(model).ConfigureAwait(false);
                if (!results.IsValid) return BadRequest(results);

                return Ok(_mapper.Map<SessionCaseSearchCompiledSqlDto>(await CompileSql.Compile(_dbContext,
                    _mapper.Map<SessionCaseSearchCompiledSql>(model),
                    _userName).ConfigureAwait(false)));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }
    }
}