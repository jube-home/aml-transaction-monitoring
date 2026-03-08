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
    public class EntityAnalysisModelRequestXPathController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelRequestXPathRepository repository;
        private readonly string userName;
        private readonly IValidator<EntityAnalysisModelRequestXPathDto> validator;

        public EntityAnalysisModelRequestXPathController(ILog log,
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
                cfg.CreateMap<EntityAnalysisModelRequestXPathDto, EntityAnalysisModelRequestXpath>();
                cfg.CreateMap<EntityAnalysisModelRequestXpath, EntityAnalysisModelRequestXPathDto>();
            });

            mapper = new Mapper(config);
            repository = new EntityAnalysisModelRequestXPathRepository(dbContext, userName);
            validator = new EntityAnalysisModelRequestXPathDtoValidator(repository);
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
        public async Task<ActionResult<List<EntityAnalysisModelRequestXPathDto>>> GetAsync(CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        7
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelRequestXPathDto>>(await repository.GetAsync(token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelId/{entityAnalysisModelId:int}")]
        public async Task<ActionResult<List<EntityAnalysisModelRequestXPathDto>>> GetByEntityAnalysisModelIdAsync(
            int entityAnalysisModelId, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        7, 13
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelRequestXPathDto>>(
                    await repository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCasesWorkflowId/{casesWorkflowId:int}")]
        public async Task<ActionResult<List<EntityAnalysisModelRequestXPathDto>>> GetByCasesWorkflowIdAsync(int casesWorkflowId, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        7
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelRequestXPathDto>>(
                    await repository.GetByCasesWorkflowIdAsync(casesWorkflowId, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("BySuppressionKey")]
        public async Task<ActionResult<List<EntityAnalysisModelRequestXPathDto>>> GetBySuppressionKeyAsync(CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        2
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelRequestXPathDto>>(await repository.GetBySuppressionKeysAsync(token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelId/{entityAnalysisModelId:int}/ByStringIntegerFloatDataType")]
        public async Task<ActionResult<List<EntityAnalysisModelRequestXPathDto>>> GetByEntityAnalysisModelIdByDataTypeAsync(
            int entityAnalysisModelId, int dataTypeId, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        7, 12
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelRequestXPathDto>>(
                    await repository.GetByEntityAnalysisModelIdByDataTypeAsync(entityAnalysisModelId, token, 1, 2, 3)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<EntityAnalysisModelRequestXPathDto>> GetByIdAsync(int id, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        7
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<EntityAnalysisModelRequestXPathDto>(await repository.GetByIdAsync(id, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(EntityAnalysisModelRequestXPathDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<EntityAnalysisModelRequestXPathDto>> CreateAsync(
            [FromBody] EntityAnalysisModelRequestXPathDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        7
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model, token);
                if (results.IsValid)
                {
                    return Ok(await repository.InsertIncrementCacheIndexIdAsync(mapper.Map<EntityAnalysisModelRequestXpath>(model), token));
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
        [ProducesResponseType(typeof(EntityAnalysisModelRequestXPathDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<EntityAnalysisModelRequestXPathDto>> UpdateAsync(
            [FromBody] EntityAnalysisModelRequestXPathDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        7
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model, token);
                if (results.IsValid)
                {
                    return Ok(await repository.UpdateAsync(mapper.Map<EntityAnalysisModelRequestXpath>(model), token));
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
        public async Task<ActionResult<List<EntityAnalysisModelRequestXPathDto>>> DeleteAsync(int id, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        7
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
