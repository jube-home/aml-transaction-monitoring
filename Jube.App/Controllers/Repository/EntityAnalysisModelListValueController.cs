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
    public class EntityAnalysisModelListValueController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelListValueRepository repository;
        private readonly string userName;
        private readonly IValidator<EntityAnalysisModelListValueDto> validator;

        public EntityAnalysisModelListValueController(ILog log,
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
                cfg.CreateMap<EntityAnalysisModelListValueDto, EntityAnalysisModelListValue>();
                cfg.CreateMap<EntityAnalysisModelListValue, EntityAnalysisModelListValueDto>();
            }, NullLoggerFactory.Instance);

            mapper = new Mapper(config);
            repository = new EntityAnalysisModelListValueRepository(dbContext, userName);
            validator = new EntityAnalysisModelsListValueDtoValidator();
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
        public async Task<ActionResult<List<EntityAnalysisModelListValueDto>>> GetAsync(CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        3
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelListValueDto>>(await repository.GetAsync(token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelListId")]
        public async Task<ActionResult<List<EntityAnalysisModelListValueDto>>> GetByEntityAnalysisModelIdAsync(
            int entityAnalysisModelListId, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        3
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelListValueDto>>(
                    await repository.GetByEntityAnalysisModelListIdOrderByIdAsync(entityAnalysisModelListId, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<EntityAnalysisModelListValueDto>> GetByIdAsync(int id, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        3
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<EntityAnalysisModelListValueDto>(await repository.GetByIdAsync(id, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(EntityAnalysisModelListValueDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<EntityAnalysisModelListValueDto>> CreateAsync([FromBody] EntityAnalysisModelListValueDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        3
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model, token);
                if (results.IsValid)
                {
                    return Ok(await repository.InsertAsync(mapper.Map<EntityAnalysisModelListValue>(model), token));
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
        [ProducesResponseType(typeof(EntityAnalysisModelListValueDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<EntityAnalysisModelListValue>> UpdateAsync([FromBody] EntityAnalysisModelListValueDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        3
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model, token);
                if (results.IsValid)
                {
                    return Ok(await repository.UpdateAsync(mapper.Map<EntityAnalysisModelListValue>(model), token));
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
        public async Task<ActionResult<List<EntityAnalysisModelListValueDto>>>
            DeleteAsync(int id, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        3
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
