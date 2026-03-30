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
    using FluentValidation.Results;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging.Abstractions;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class UserInTenantController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly UserInTenantRepository repository;
        private readonly string userName;

        public UserInTenantController(ILog log,
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
                cfg.CreateMap<UserInTenantDto, UserInTenant>();
                cfg.CreateMap<UserInTenant, UserInTenantDto>();
            }, NullLoggerFactory.Instance);

            mapper = new Mapper(config);
            repository = new UserInTenantRepository(dbContext, userName);
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

        [HttpPut]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<TenantRegistryDto>> UpdateAsync(int tenantRegistryId, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Landlord)
                {
                    return Forbid();
                }

                await repository.UpdateAsync(userName, tenantRegistryId, token);

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

        [HttpGet]
        public async Task<ActionResult<List<UserInTenantDto>>> GetAsync(CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        1
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<UserInTenantDto>>(await repository.GetAsync(token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("GetCurrentTenantRegistry")]
        public Task<ActionResult<UserInTenantDto>> GetCurrentTenantRegistryAsync()
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        1
                    }))
                {
                    return Task.FromResult<ActionResult<UserInTenantDto>>(Forbid());
                }

                return Task.FromResult<ActionResult<UserInTenantDto>>(Ok(mapper.Map<UserInTenantDto>(repository.GetCurrentTenantRegistry())));
            }
            catch (Exception e)
            {
                log.Error(e);
                return Task.FromResult<ActionResult<UserInTenantDto>>(StatusCode(500));
            }
        }
    }
}
