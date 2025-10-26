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
    public class EntityAnalysisModelTtlCounterController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelTtlCounterRepository repository;
        private readonly string userName;
        private readonly IValidator<EntityAnalysisModelTtlCounterDto> validator;

        public EntityAnalysisModelTtlCounterController(ILog log,
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
                cfg.CreateMap<EntityAnalysisModelTtlCounterDto, EntityAnalysisModelTtlCounter>();
                cfg.CreateMap<EntityAnalysisModelTtlCounter, EntityAnalysisModelTtlCounterDto>();
                cfg.CreateMap<List<EntityAnalysisModelTtlCounter>, List<EntityAnalysisModelTtlCounterDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            mapper = new Mapper(config);
            repository = new EntityAnalysisModelTtlCounterRepository(dbContext, userName);
            validator = new EntityAnalysisModelTtlCounterDtoValidator();
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
        public ActionResult<List<EntityAnalysisModelTtlCounterDto>> Get()
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        12
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelTtlCounterDto>>(repository.Get()));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelId/{id:int}")]
        public ActionResult<List<EntityAnalysisModelTtlCounterDto>> GetByEntityAnalysisModelId(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        12, 17
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelTtlCounterDto>>(
                    repository.GetByEntityAnalysisModelIdOrderById(id)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelGuid/{guid:guid}")]
        public ActionResult<List<EntityAnalysisModelTtlCounterDto>> GetByEntityAnalysisModelGuid(Guid guid)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        12, 17
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<EntityAnalysisModelTtlCounterDto>>(
                    repository.GetByEntityAnalysisModelGuid(guid)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public ActionResult<EntityAnalysisModelTtlCounterDto> GetById(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        12
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<EntityAnalysisModelTtlCounterDto>(repository.GetById(id)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(EntityAnalysisModelTtlCounterDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<EntityAnalysisModelTtlCounterDto> Create([FromBody] EntityAnalysisModelTtlCounterDto model)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        12
                    }))
                {
                    return Forbid();
                }

                var results = validator.Validate(model);
                if (results.IsValid)
                {
                    return Ok(repository.Insert(mapper.Map<EntityAnalysisModelTtlCounter>(model)));
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
        [ProducesResponseType(typeof(EntityAnalysisModelTtlCounterDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<EntityAnalysisModelRequestXPathDto> Update(
            [FromBody] EntityAnalysisModelTtlCounterDto model)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        12
                    }))
                {
                    return Forbid();
                }

                var results = validator.Validate(model);
                if (results.IsValid)
                {
                    return Ok(repository.Update(mapper.Map<EntityAnalysisModelTtlCounter>(model)));
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
        public ActionResult<List<EntityAnalysisModelTtlCounterDto>> Delete(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        12
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
