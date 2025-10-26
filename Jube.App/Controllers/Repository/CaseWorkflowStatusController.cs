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
    public class CaseWorkflowStatusController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseWorkflowStatusRepository repository;
        private readonly string userName;
        private readonly IValidator<CaseWorkflowStatusDto> validator;

        public CaseWorkflowStatusController(ILog log,
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
                cfg.CreateMap<CaseWorkflowStatus, CaseWorkflowStatusDto>();
                cfg.CreateMap<CaseWorkflowStatusDto, CaseWorkflowStatus>();
                cfg.CreateMap<List<CaseWorkflowStatus>, List<CaseWorkflowStatusDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            mapper = new Mapper(config);
            repository = new CaseWorkflowStatusRepository(dbContext, userName);
            validator = new CaseWorkflowStatusDtoValidator();
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
        public ActionResult<List<CaseWorkflowStatusDto>> ByCasesWorkflowIdActiveOnly(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        19, 1
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<CaseWorkflowStatusDto>>(repository.GetByCasesWorkflowIdActiveOnly(id)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCasesWorkflowGuidActiveOnly/{guid:guid}")]
        public ActionResult<List<CaseWorkflowStatusDto>> ByCasesWorkflowGuidActiveOnly(Guid guid)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        19, 1
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<CaseWorkflowStatusDto>>(repository.GetByCasesWorkflowGuidActiveOnly(guid)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        public ActionResult<List<CaseWorkflowStatusDto>> Get()
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        19
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<CaseWorkflowStatusDto>>(repository.Get()));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCaseWorkflowId/{id:int}")]
        public ActionResult<List<CaseWorkflowStatusDto>> GetByEntityAnalysisModelId(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        17, 19, 25, 1
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<CaseWorkflowStatusDto>>(repository.GetByCasesWorkflowIdOrderById(id)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCaseWorkflowGuid/{guid:guid}")]
        public ActionResult<List<CaseWorkflowStatusDto>> GetByEntityAnalysisModelGuid(Guid guid)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        17, 19, 25, 1
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<CaseWorkflowStatusDto>>(repository.GetByCasesWorkflowGuid(guid)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public ActionResult<CaseWorkflowStatusDto> GetById(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        19
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<CaseWorkflowStatusDto>(repository.GetById(id)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(CaseWorkflowStatusDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<CaseWorkflowStatusDto> Create([FromBody] CaseWorkflowStatusDto model)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        19
                    }))
                {
                    return Forbid();
                }

                var results = validator.Validate(model);
                if (results.IsValid)
                {
                    return Ok(repository.Insert(mapper.Map<CaseWorkflowStatus>(model)));
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
        [ProducesResponseType(typeof(CaseWorkflowStatusDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<CaseWorkflowStatusDto> Update([FromBody] CaseWorkflowStatusDto model)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        19
                    }))
                {
                    return Forbid();
                }

                var results = validator.Validate(model);
                if (results.IsValid)
                {
                    return Ok(repository.Update(mapper.Map<CaseWorkflowStatus>(model)));
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
        public ActionResult<List<CaseWorkflowStatusDto>> Get(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        19
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
