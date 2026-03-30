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
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging.Abstractions;
    using Validators;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class VisualisationRegistryRoleController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly VisualisationRegistryRoleRepository repository;
        private readonly string userName;
        private readonly IValidator<VisualisationRegistryRoleDto> validator;

        public VisualisationRegistryRoleController(ILog log,
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
                cfg.CreateMap<VisualisationRegistryRole, VisualisationRegistryRoleDto>();
                cfg.CreateMap<VisualisationRegistryRoleDto, VisualisationRegistryRole>();
            }, NullLoggerFactory.Instance);

            mapper = new Mapper(config);
            repository = new VisualisationRegistryRoleRepository(dbContext, userName);
            validator = new VisualisationRegistryRoleDtoValidator();
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

        [HttpGet("ByVisualisationRegistryGuid/{guid:guid}")]
        public async Task<ActionResult<List<VisualisationRegistryRoleDto>>> ByVisualisationRegistryGuidAsync(Guid guid, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        31
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<VisualisationRegistryRoleDto>>(await repository.GetByVisualisationRegistryGuidAsync(guid, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        public async Task<ActionResult<List<VisualisationRegistryRoleDto>>> CreateAsync([FromBody] VisualisationRegistryRoleDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        31
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model, token);
                if (results.IsValid)
                {
                    return Ok(await repository.InsertAsync(mapper.Map<VisualisationRegistryRole>(model), token));
                }

                return BadRequest(results);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<List<VisualisationRegistryRoleDto>>> DeleteAsync(int id, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        31
                    }))
                {
                    return Forbid();
                }

                await repository.DeleteAsync(id, token);

                return Ok();
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
