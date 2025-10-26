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

            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            permissionValidation = new PermissionValidation(dbContext, userName);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<EntityAnalysisModelReprocessingRuleInstanceDto,
                    EntityAnalysisModelReprocessingRuleInstance>();
                cfg.CreateMap<EntityAnalysisModelReprocessingRuleInstance,
                    EntityAnalysisModelReprocessingRuleInstanceDto>();
                cfg.CreateMap<List<EntityAnalysisModelReprocessingRuleInstance>,
                        List<EntityAnalysisModelReprocessingRuleInstanceDto>>()
                    .ForMember("Item", opt => opt.Ignore());
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
        public ActionResult<List<EntityAnalysisModelReprocessingRuleInstanceDto>> Get()
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

                return Ok(mapper.Map<List<EntityAnalysisModelReprocessingRuleInstanceDto>>(repository.Get()));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelReprocessingId")]
        public ActionResult<List<EntityAnalysisModelReprocessingRuleInstanceDto>>
            GetByEntityAnalysisModelReprocessingId(int entityAnalysisModelReprocessingId)
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
                    repository.GetByEntityAnalysisModelsReprocessingRuleId(entityAnalysisModelReprocessingId)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public ActionResult<EntityAnalysisModelReprocessingRuleInstanceDto> GetById(int id)
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

                return Ok(mapper.Map<EntityAnalysisModelReprocessingRuleInstanceDto>(repository.GetById(id)));
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
        public ActionResult<EntityAnalysisModelReprocessingRuleInstanceDto> CreateByExistingUpdateUncompleted(
            [FromBody] EntityAnalysisModelReprocessingRuleInstanceDto model)
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

                var results = validator.Validate(model);
                if (results.IsValid)
                {
                    return Ok(repository.InsertByExistingUpdateUncompleted(
                        mapper.Map<EntityAnalysisModelReprocessingRuleInstance>(model)));
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
        public ActionResult<EntityAnalysisModelReprocessingRuleInstanceDto> Create(
            [FromBody] EntityAnalysisModelReprocessingRuleInstanceDto model)
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

                var results = validator.Validate(model);
                if (results.IsValid)
                {
                    return Ok(repository.Insert(mapper.Map<EntityAnalysisModelReprocessingRuleInstance>(model)));
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
        public ActionResult<EntityAnalysisModelReprocessingRuleInstanceDto> Update(
            [FromBody] EntityAnalysisModelReprocessingRuleInstanceDto model)
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

                var results = validator.Validate(model);
                if (results.IsValid)
                {
                    return Ok(repository.Update(mapper.Map<EntityAnalysisModelReprocessingRuleInstance>(model)));
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
        public ActionResult<List<EntityAnalysisModelReprocessingRuleInstanceDto>> Delete(int id)
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

                repository.Delete(id);
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
