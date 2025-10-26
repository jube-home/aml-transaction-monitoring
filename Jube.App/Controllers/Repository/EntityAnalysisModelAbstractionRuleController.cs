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
    public class EntityAnalysisModelAbstractionRuleController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelAbstractionRuleRepository repository;
        private readonly string userName;
        private readonly IValidator<EntityAnalysisModelAbstractionRuleDto> validator;

        public EntityAnalysisModelAbstractionRuleController(ILog log,
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
                cfg.CreateMap<EntityAnalysisModelAbstractionRuleDto, EntityAnalysisModelAbstractionRule>();
                cfg.CreateMap<EntityAnalysisModelAbstractionRule, EntityAnalysisModelAbstractionRuleDto>();
                cfg.CreateMap<List<EntityAnalysisModelAbstractionRule>, List<EntityAnalysisModelAbstractionRuleDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            mapper = new Mapper(config);
            repository = new EntityAnalysisModelAbstractionRuleRepository(dbContext, userName);
            validator = new EntityAnalysisModelAbstractionRuleDtoValidator();
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
        public ActionResult<List<EntityAnalysisModelAbstractionRuleDto>> Get()
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        13
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelAbstractionRuleDto>>(repository.Get()));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelId/{entityAnalysisModelId:int}")]
        public ActionResult<List<EntityAnalysisModelAbstractionRuleDto>> GetByEntityAnalysisModelId(
            int entityAnalysisModelId)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        13
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelAbstractionRuleDto>>(
                    repository.GetByEntityAnalysisModelIdOrderByIdDesc(entityAnalysisModelId)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public ActionResult<EntityAnalysisModelAbstractionRuleDto> GetByEntityAnalysisModelAbstractionRuleId(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        13
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<EntityAnalysisModelAbstractionRuleDto>(repository.GetById(id)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(EntityAnalysisModelAbstractionRuleDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<EntityAnalysisModelAbstractionRuleDto> Create(
            [FromBody] EntityAnalysisModelAbstractionRuleDto model)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        13
                    }))
                {
                    return Forbid();
                }

                var results = validator.Validate(model);
                if (results.IsValid)
                {
                    return Ok(repository.Insert(mapper.Map<EntityAnalysisModelAbstractionRule>(model)));
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
        [ProducesResponseType(typeof(EntityAnalysisModelAbstractionRuleDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<EntityAnalysisModelAbstractionRuleDto> Update(
            [FromBody] EntityAnalysisModelAbstractionRuleDto model)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        13
                    }))
                {
                    return Forbid();
                }

                var results = validator.Validate(model);
                if (results.IsValid)
                {
                    return Ok(repository.Update(mapper.Map<EntityAnalysisModelAbstractionRule>(model)));
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
        public ActionResult<List<EntityAnalysisModelAbstractionRuleDto>> Delete(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        13
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
