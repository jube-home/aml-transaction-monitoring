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
    using Validators;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class EntityAnalysisModelReprocessingRuleInstanceController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelReprocessingRuleInstanceRepository repository;
        private readonly string userName;
        private readonly IValidator<EntityAnalysisModelReprocessingRuleInstanceDto> validator;

        public EntityAnalysisModelReprocessingRuleInstanceController(ILog log,
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
                cfg.CreateMap<EntityAnalysisModelReprocessingRuleInstanceDto,
                    EntityAnalysisModelReprocessingRuleInstance>();
                cfg.CreateMap<EntityAnalysisModelReprocessingRuleInstance,
                    EntityAnalysisModelReprocessingRuleInstanceDto>();
            });

            mapper = new Mapper(config);
            repository = new EntityAnalysisModelReprocessingRuleInstanceRepository(dbContext, userName);
            validator = new EntityAnalysisModelReprocessingRuleInstanceDtoValidator();
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

        [HttpGet]
        public async Task<ActionResult<List<EntityAnalysisModelReprocessingRuleInstanceDto>>> GetAsync(CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        26
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelReprocessingRuleInstanceDto>>(await repository.GetAsync(token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelReprocessingId")]
        public async Task<ActionResult<List<EntityAnalysisModelReprocessingRuleInstanceDto>>>
            GetByEntityAnalysisModelReprocessingIdAsync(int entityAnalysisModelReprocessingId, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        26
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelReprocessingRuleInstanceDto>>(
                    await repository.GetByEntityAnalysisModelsReprocessingRuleIdAsync(entityAnalysisModelReprocessingId, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<EntityAnalysisModelReprocessingRuleInstanceDto>> GetByIdAsync(int id, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        26
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<EntityAnalysisModelReprocessingRuleInstanceDto>(await repository.GetByIdAsync(id, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [Route("ByExistingUpdateUncompleted")]
        [ProducesResponseType(typeof(EntityAnalysisModelReprocessingRuleInstanceDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<EntityAnalysisModelReprocessingRuleInstanceDto>> CreateByExistingUpdateUncompletedAsync(
            [FromBody] EntityAnalysisModelReprocessingRuleInstanceDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        26
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model, token);
                if (results.IsValid)
                {
                    return Ok(await repository.InsertByExistingUpdateUncompletedAsync(
                        mapper.Map<EntityAnalysisModelReprocessingRuleInstance>(model), token));
                }

                return BadRequest(results);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }


        [HttpPost]
        [ProducesResponseType(typeof(EntityAnalysisModelReprocessingRuleInstanceDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<EntityAnalysisModelReprocessingRuleInstanceDto>> CreateAsync(
            [FromBody] EntityAnalysisModelReprocessingRuleInstanceDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        26
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model, token);
                if (results.IsValid)
                {
                    return Ok(await repository.InsertAsync(mapper.Map<EntityAnalysisModelReprocessingRuleInstance>(model), token));
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
        [ProducesResponseType(typeof(EntityAnalysisModelReprocessingRuleInstanceDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<EntityAnalysisModelReprocessingRuleInstanceDto>> UpdateAsync(
            [FromBody] EntityAnalysisModelReprocessingRuleInstanceDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        26
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (results.IsValid)
                {
                    return Ok(await repository.UpdateAsync(mapper.Map<EntityAnalysisModelReprocessingRuleInstance>(model), token).ConfigureAwait(false));
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
        public async Task<ActionResult<List<EntityAnalysisModelReprocessingRuleInstanceDto>>> DeleteAsync(int id, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        26
                    }))
                {
                    return Forbid();
                }

                await repository.DeleteAsync(id, token).ConfigureAwait(false);
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
