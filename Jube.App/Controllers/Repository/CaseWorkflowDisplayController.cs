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

namespace Jube.App.Controllers.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using Code;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Dto;
    using DynamicEnvironment;
    using FluentValidation;
    using FluentValidation.Results;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging.Abstractions;
    using Validators;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CaseWorkflowDisplayController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseWorkflowDisplayRepository repository;
        private readonly string userName;
        private readonly IValidator<CaseWorkflowDisplayDto> validator;

        public CaseWorkflowDisplayController(ILog log,
            IHttpContextAccessor httpContextAccessor, DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            this.log = log;
            dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), log);
            permissionValidation = new PermissionValidation(dbContext, userName, log);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CaseWorkflowDisplay, CaseWorkflowDisplayDto>();
                cfg.CreateMap<CaseWorkflowDisplayDto, CaseWorkflowDisplay>();
            }, NullLoggerFactory.Instance);

            mapper = new Mapper(config);
            repository = new CaseWorkflowDisplayRepository(dbContext, userName);
            validator = new CaseWorkflowDisplayDtoValidator(repository);
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

        [HttpGet("ByCasesWorkflowIdActiveOnly/{id:int}")]
        public async Task<ActionResult<List<CaseWorkflowDisplayDto>>> ByCasesWorkflowIdActiveOnlyAsync(int id, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        23, 1
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<CaseWorkflowDisplayDto>>(await repository.GetByCasesWorkflowIdActiveOnlyAsync(id, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCasesWorkflowGuidActiveOnly/{guid:guid}")]
        public async Task<ActionResult<List<CaseWorkflowDisplayDto>>> ByCasesWorkflowGuidActiveOnlyAsync(Guid guid, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        23, 1
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<CaseWorkflowDisplayDto>>(
                    await repository.GetByCasesWorkflowGuidActiveOnlyAsync(guid, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<CaseWorkflowDisplayDto>>> GetAsync(CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        23
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<CaseWorkflowDisplayDto>>(await repository.GetAsync(token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCasesWorkflowId/{casesWorkflowId:int}")]
        public async Task<ActionResult<List<CaseWorkflowDisplayDto>>> GetByEntityAnalysisModelIdAsync(int casesWorkflowId, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        23
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<CaseWorkflowDisplayDto>>(
                    await repository.GetByCasesWorkflowIdOrderByIdAsync(casesWorkflowId, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CaseWorkflowDisplayDto>> GetByIdAsync(int id, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        23
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<CaseWorkflowDisplayDto>(await repository.GetByIdAsync(id, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(CaseWorkflowDisplayDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<CaseWorkflowDisplayDto>> CreateAsync([FromBody] CaseWorkflowDisplayDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        23
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model, token);
                if (results.IsValid)
                {
                    return Ok(await repository.InsertAsync(mapper.Map<CaseWorkflowDisplay>(model), token));
                }

                return BadRequest(results);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPut]
        [ProducesResponseType(typeof(CaseWorkflowDisplayDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<CaseWorkflowDisplayDto>> UpdateAsync([FromBody] CaseWorkflowDisplayDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        23
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model, token);
                if (results.IsValid)
                {
                    return Ok(await repository.UpdateAsync(mapper.Map<CaseWorkflowDisplay>(model), token));
                }

                return BadRequest(results);
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(204);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpDelete]
        [Route("{id:int}")]
        public async Task<ActionResult<List<CaseWorkflowDisplayDto>>> GetAsync(int id, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        23
                    }))
                {
                    return Forbid();
                }

                await repository.DeleteAsync(id, token);
                return Ok();
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(204);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
