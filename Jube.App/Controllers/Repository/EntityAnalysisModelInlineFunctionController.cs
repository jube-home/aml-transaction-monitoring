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
    public class EntityAnalysisModelInlineFunctionController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelInlineFunctionRepository repository;
        private readonly string userName;
        private readonly IValidator<EntityAnalysisModelInlineFunctionDto> validator;

        public EntityAnalysisModelInlineFunctionController(ILog log,
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
                cfg.CreateMap<EntityAnalysisModelInlineFunctionDto, EntityAnalysisModelInlineFunction>();
                cfg.CreateMap<EntityAnalysisModelInlineFunction, EntityAnalysisModelInlineFunctionDto>();
            }, NullLoggerFactory.Instance);

            mapper = new Mapper(config);
            repository = new EntityAnalysisModelInlineFunctionRepository(dbContext, userName);
            validator = new EntityAnalysisModelInlineFunctionDtoValidator(repository);
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
        public async Task<ActionResult<List<EntityAnalysisModelInlineFunctionDto>>> GetAsync(CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        8
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelInlineFunctionDto>>(await repository.GetAsync(token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelId/{entityAnalysisModelId:int}")]
        public async Task<ActionResult<List<EntityAnalysisModelInlineFunctionDto>>> GetByEntityAnalysisModelIdAsync(
            int entityAnalysisModelId, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        8
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelInlineFunctionDto>>(
                    await repository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<EntityAnalysisModelInlineFunctionDto>> GetByEntityAnalysisModelInlineFunctionIdAsync(int id, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        8
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<EntityAnalysisModelInlineFunctionDto>(await repository.GetByIdAsync(id, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(EntityAnalysisModelInlineFunctionDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<EntityAnalysisModelInlineFunctionDto>> CreateAsync(
            [FromBody] EntityAnalysisModelInlineFunctionDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        8
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model, token);
                if (results.IsValid)
                {
                    return Ok(await repository.InsertAsync(mapper.Map<EntityAnalysisModelInlineFunction>(model), token));
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
        [ProducesResponseType(typeof(EntityAnalysisModelInlineFunctionDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<EntityAnalysisModelInlineFunctionDto>> UpdateAsync(
            [FromBody] EntityAnalysisModelInlineFunctionDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        8
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model, token);
                if (results.IsValid)
                {
                    return Ok(await repository.UpdateAsync(mapper.Map<EntityAnalysisModelInlineFunction>(model), token));
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
        public async Task<ActionResult<List<EntityAnalysisModelInlineFunctionDto>>> DeleteAsync(int id, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        8
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
