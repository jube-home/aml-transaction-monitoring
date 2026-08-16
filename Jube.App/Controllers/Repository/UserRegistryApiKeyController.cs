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
    using ApiTokensCache;
    using AutoMapper;
    using Cache;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Dto;
    using Dto.Mapping;
    using Dto.UserRegistry;
    using DynamicEnvironment;
    using FluentValidation;
    using FluentValidation.Results;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging.Abstractions;
    using Validators;
    using PermissionValidation=Code.PermissionValidation;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class UserRegistryApiKeyController : Controller
    {
        private readonly CacheService cacheService;
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly UserRegistryApiKeyRepository repository;
        private readonly string userName;
        private readonly IValidator<UserRegistryApiKeyDto> validator;

        public UserRegistryApiKeyController(ILog log, CacheService cacheService,
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
                cfg.CreateMap<UserRegistryApiKeyDto, UserRegistryApiKey>();
                cfg.CreateMap<UserRegistryApiKey, UserRegistryApiKeyDto>();
                cfg.CreateMap<DateTime?, DateTimeOffset?>().ConvertUsing<NullableDateTimeToDateTimeOffsetConverter>();
                cfg.CreateMap<DateTime, DateTimeOffset>().ConvertUsing(src => new DateTimeOffset(DateTime.SpecifyKind(src, DateTimeKind.Utc)));
            }, NullLoggerFactory.Instance);

            mapper = new Mapper(config);
            repository = new UserRegistryApiKeyRepository(dbContext, userName);
            validator = new UserRegistryApiKeyRequestDtoValidator();
            this.cacheService = cacheService;
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

        [HttpGet("ByUserRegistryId/{userRegistryId:int}")]
        public async Task<ActionResult<List<UserRegistryDto>>> GetByUserRegistryIdAsync(int userRegistryId, CancellationToken token = default)
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

                return Ok(mapper.Map<List<UserRegistryApiKeyDto>>(await repository.GetByUserRegistryIdAsync(userRegistryId, token)));
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
        public async Task<ActionResult<UserRegistryDto>> CreateAsync([FromBody] UserRegistryApiKeyDto model, CancellationToken token = default)
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
                if (!results.IsValid)
                {
                    return BadRequest(results);
                }

                var userRegistryRepository = new UserRegistryRepository(dbContext, userName);
                var userRegistry = await userRegistryRepository.GetByIdAsync(model.UserRegistryId, token);

                if (userRegistry == null)
                {
                    return BadRequest();
                }

                var apiKey = ApiKeyHelper.Generate(userRegistry.Name, dynamicEnvironment.AppSettings("ApiHmacKey"));

                var insertModel = mapper.Map<UserRegistryApiKey>(model);
                insertModel.ApiKey = apiKey.ApiKeyHash;
                insertModel.ApiKeyDisplay = apiKey.ApiKeyDisplay;

                if (ApiKeyHelper.TryParse(apiKey.ApiKey, dynamicEnvironment.AppSettings("ApiHmacKey"), out var components))
                {
                    if (components?.ApiKeyHash != apiKey.ApiKeyHash)
                    {
                        return BadRequest();
                    }
                }

                var insertedModel = await repository.InsertAsync(insertModel, token);
                insertModel.ApiKeyDisplay = apiKey.ApiKey;

                await cacheService.CacheUserRegistryApiKeyRepository.PublishSetAsync(apiKey.ApiKeyHash);

                return Ok(mapper.Map<UserRegistryApiKey>(insertedModel));
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

                var apiKeyHash = await repository.GetByIdAsync(id, token);
                if (apiKeyHash == null)
                {
                    return Ok();
                }

                await repository.DeleteAsync(id, token);
                await cacheService.CacheUserRegistryApiKeyRepository.PublishRemoveAsync(apiKeyHash.ApiKey);

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
