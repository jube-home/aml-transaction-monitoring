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
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Data.Security;
    using Dto.UserRegistry;
    using DynamicEnvironment;
    using FluentValidation;
    using FluentValidation.Results;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Validators;
    using PermissionValidation=Code.PermissionValidation;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class UserRegistryController : Controller
    {
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly UserRegistryRepository repository;
        private readonly string userName;
        private readonly IValidator<UserRegistryDto> validator;

        public UserRegistryController(ILog log,
            DynamicEnvironment dynamicEnvironment,
            IHttpContextAccessor httpContextAccessor)
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
                cfg.CreateMap<UserRegistryDto, UserRegistry>();
                cfg.CreateMap<UserRegistry, UserRegistryDto>();
            });

            mapper = new Mapper(config);
            repository = new UserRegistryRepository(dbContext, userName);
            validator = new UserRegistryDtoValidator(repository);
            this.dynamicEnvironment = dynamicEnvironment;
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
        public async Task<ActionResult<List<UserRegistryDto>>> GetAsync(CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        35
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<UserRegistryDto>>(await repository.GetAsync(token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelId/{roleRegistryId:int}")]
        public async Task<ActionResult<List<UserRegistryDto>>> GetByRoleRegistryIdAsync(int roleRegistryId, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        35
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<UserRegistryDto>>(await repository.GetByRoleRegistryIdAsync(roleRegistryId, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserRegistryDto>> GetByIdAsync(int id, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        35
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<UserRegistryDto>(await repository.GetByIdAsync(id, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(UserRegistryDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<UserRegistryDto>> CreateAsync([FromBody] UserRegistryDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        35
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model, token);
                if (results.IsValid)
                {
                    return Ok(await repository.InsertAsync(mapper.Map<UserRegistry>(model), token));
                }

                return BadRequest(results);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("SetPassword/{id:int}")]
        [ProducesResponseType(typeof(UserRegistryDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<UserRegistryPasswordResponseDto>> UpdatePasswordAsync(int id,
            CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        35
                    }))
                {
                    return Forbid();
                }

                var userRegistryPasswordResponseDto = new UserRegistryPasswordResponseDto
                {
                    Password = HashPassword.CreatePasswordInClear(8),
                    PasswordExpiryDate = DateTime.Now
                };

                var hashedPassword = HashPassword.GenerateHash(userRegistryPasswordResponseDto.Password,
                    dynamicEnvironment.AppSettings("PasswordHashingKey"));

                await repository.SetPasswordAsync(id, hashedPassword, userRegistryPasswordResponseDto.PasswordExpiryDate, token);

                return userRegistryPasswordResponseDto;
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPut]
        [ProducesResponseType(typeof(UserRegistryDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<UserRegistryDto>> UpdateAsync([FromBody] UserRegistryDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        35
                    }))
                {
                    return Forbid();
                }

                var results = await validator.ValidateAsync(model, token);
                if (results.IsValid)
                {
                    return Ok(await repository.UpdateAsync(mapper.Map<UserRegistry>(model), token));
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
        public async Task<ActionResult<List<UserRegistryDto>>> DeleteAsync(int id, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        35
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
