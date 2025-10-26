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

            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            permissionValidation = new PermissionValidation(dbContext, userName);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UserRegistryDto, UserRegistry>();
                cfg.CreateMap<UserRegistry, UserRegistryDto>();
                cfg.CreateMap<List<UserRegistry>, List<UserRegistryDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            mapper = new Mapper(config);
            repository = new UserRegistryRepository(dbContext, userName);
            validator = new UserRegistryDtoValidator();
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
        public ActionResult<List<UserRegistryDto>> Get()
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

                return Ok(mapper.Map<List<UserRegistryDto>>(repository.Get()));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelId/{roleRegistryId:int}")]
        public ActionResult<List<UserRegistryDto>> GetByRoleRegistryId(int roleRegistryId)
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

                return Ok(mapper.Map<List<UserRegistryDto>>(repository.GetByRoleRegistryId(roleRegistryId)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id:int}")]
        public ActionResult<UserRegistryDto> GetById(int id)
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

                return Ok(mapper.Map<UserRegistryDto>(repository.GetById(id)));
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
        public ActionResult<UserRegistryDto> Create([FromBody] UserRegistryDto model)
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

                var results = validator.Validate(model);
                if (results.IsValid)
                {
                    return Ok(repository.Insert(mapper.Map<UserRegistry>(model)));
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
        public ActionResult<UserRegistryPasswordResponseDto> UpdatePassword(int id)
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

                repository.SetPassword(id, hashedPassword, userRegistryPasswordResponseDto.PasswordExpiryDate);

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
        public ActionResult<UserRegistryDto> Update([FromBody] UserRegistryDto model)
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

                var results = validator.Validate(model);
                if (results.IsValid)
                {
                    return Ok(repository.Update(mapper.Map<UserRegistry>(model)));
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
        public ActionResult<List<UserRegistryDto>> Delete(int id)
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
