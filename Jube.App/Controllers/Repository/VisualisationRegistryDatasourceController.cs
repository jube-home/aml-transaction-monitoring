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
    using Data.Validation;
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
    public class VisualisationRegistryDatasourceController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly VisualisationRegistryDatasourceRepository repository;
        private readonly string userName;
        private readonly IValidator<VisualisationRegistryDatasourceDto> validator;

        public VisualisationRegistryDatasourceController(ILog log,
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
                cfg.CreateMap<VisualisationRegistryDatasourceDto, VisualisationRegistryDatasource>();
                cfg.CreateMap<VisualisationRegistryDatasource, VisualisationRegistryDatasourceDto>();
                cfg.CreateMap<List<VisualisationRegistryDatasource>, List<VisualisationRegistryDatasourceDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            mapper = new Mapper(config);
            repository = new VisualisationRegistryDatasourceRepository(dbContext, userName);
            validator = new VisualisationRegistryDatasourceValidator();
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
        public ActionResult<List<VisualisationRegistryDatasourceDto>> Get()
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        33
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<VisualisationRegistryDatasourceDto>>(repository.Get()));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByVisualisationRegistryId/{visualisationRegistryId:int}")]
        public ActionResult<List<VisualisationRegistryDatasourceDto>> GetByVisualisationRegistryId(
            int visualisationRegistryId)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        33, 28, 1
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<VisualisationRegistryDatasourceDto>>(
                    repository.GetByVisualisationRegistryIdOrderById(visualisationRegistryId)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByVisualisationRegistryIdActiveOnly/{visualisationRegistryId:int}")]
        public ActionResult<List<VisualisationRegistryDatasourceDto>> GetByVisualisationRegistryIdActiveOnly(
            int visualisationRegistryId)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        33, 28, 1
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<VisualisationRegistryDatasourceDto>>(
                    repository.GetByVisualisationRegistryIdActiveOnly(visualisationRegistryId)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public ActionResult<VisualisationRegistryDatasourceDto> GetById(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        33
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<VisualisationRegistryDatasourceDto>(repository.GetById(id)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(VisualisationRegistryDatasourceDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<VisualisationRegistryDatasourceDto> Create(
            [FromBody] VisualisationRegistryDatasourceDto model)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        33
                    }))
                {
                    return Forbid();
                }

                var results = validator.Validate(model);
                if (!results.IsValid)
                {
                    return BadRequest(results);
                }

                var visualisationRegistryDatasource =
                    repository.InsertWithValidationAsync(mapper.Map<VisualisationRegistryDatasource>(model));


                return Ok(visualisationRegistryDatasource);
            }
            catch (SqlValidationFailed e)
            {
                var results = new ValidationResult();
                results.Errors.Add(new ValidationFailure("SQL", e.Message));

                if (log.IsInfoEnabled)
                {
                    log.Info(e);
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
        [ProducesResponseType(typeof(VisualisationRegistryDatasourceDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<VisualisationRegistryDatasourceDto> Update(
            [FromBody] VisualisationRegistryDatasourceDto model)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        33
                    }))
                {
                    return Forbid();
                }

                var results = validator.Validate(model);
                if (results.IsValid)
                {
                    return Ok(
                        repository.UpdateWithValidationAsync(mapper.Map<VisualisationRegistryDatasource>(model)));
                }

                return BadRequest(results);
            }
            catch (SqlValidationFailed e)
            {
                var results = new ValidationResult();
                results.Errors.Add(new ValidationFailure("SQL", e.Message));

                if (log.IsInfoEnabled)
                {
                    log.Info(e);
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
        public ActionResult<List<VisualisationRegistryDatasourceDto>> Delete(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        33
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
