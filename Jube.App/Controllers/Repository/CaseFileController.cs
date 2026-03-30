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
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using Code;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Dto;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging.Abstractions;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CaseFileController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseFileRepository repository;
        private readonly string userName;

        public CaseFileController(ILog log,
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
                cfg.CreateMap<CaseFile, CaseFileDto>();
                cfg.CreateMap<CaseFileDto, CaseFile>();
            }, NullLoggerFactory.Instance);

            mapper = new Mapper(config);
            repository = new CaseFileRepository(dbContext, userName);
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

        [HttpPost("Upload")]
        public async Task<ActionResult<CaseFileDto>> FileUploadAsync(IEnumerable<IFormFile> files, string caseKey, string caseKeyValue,
            int caseId, CancellationToken token = default)
        {
            if (!permissionValidation.Validate(new[]
                {
                    1
                }))
            {
                return Forbid();
            }

            var repositoryCase = new CaseRepository(dbContext, userName);
            var existing = await repositoryCase.GetByIdActiveOnlyAsync(caseId, token);

            if (existing == null)
            {
                return Forbid();
            }

            foreach (var file in files)
            {
                if (file.Length <= 0)
                {
                    continue;
                }

                var ms = new MemoryStream();
                await file.CopyToAsync(ms, token);

                var model = new CaseFile
                {
                    Object = ms.ToArray(),
                    CaseKey = caseKey,
                    CaseKeyValue = caseKeyValue,
                    CaseId = caseId,
                    Extension = Path.GetExtension(file.FileName),
                    Size = file.Length,
                    Name = file.FileName,
                    ContentType = file.ContentType
                };

                return Ok(mapper.Map<CaseFileDto>(await repository.InsertAsync(model, token)));
            }

            return Ok();
        }

        [HttpPost("Remove")]
        public async Task<ActionResult> FileRemoveAsync(int id, CancellationToken token = default)
        {
            if (!permissionValidation.Validate(new[]
                {
                    1
                }))
            {
                return Forbid();
            }

            var existingFile = await repository.GetByIdAsync(id, token);
            if (existingFile == null)
            {
                return Forbid();
            }

            var repositoryCase = new CaseRepository(dbContext, userName);

            if (existingFile.CaseId == null)
            {
                return Forbid();
            }

            var existingCase = await repositoryCase.GetByIdActiveOnlyAsync(existingFile.CaseId.Value, token);

            if (existingCase == null)
            {
                return Forbid();
            }

            await repository.DeleteAsync(id, token);

            return Ok();
        }

        [HttpGet]
        public async Task<ActionResult> GenerateAsync(int id, CancellationToken token = default)
        {
            if (!permissionValidation.Validate(new[]
                {
                    1
                }))
            {
                return Forbid();
            }

            var existingFile = await repository.GetByIdAsync(id, token);
            if (existingFile == null)
            {
                return Forbid();
            }

            var repositoryCase = new CaseRepository(dbContext, userName);

            if (existingFile.CaseId == null)
            {
                return Forbid();
            }

            var existingCase = await repositoryCase.GetByIdActiveOnlyAsync(existingFile.CaseId.Value, token);

            if (existingCase == null)
            {
                return Forbid();
            }

            var model = await repository.GetByIdAsync(id, token);
            return new FileContentResult(model.Object, model.ContentType);
        }

        [HttpGet("ByCaseKeyValue")]
        public async Task<ActionResult<List<CaseFileDto>>> GetByCaseKeyValueAsync(string key, string value, CancellationToken token = default)
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

                return Ok(mapper.Map<List<CaseFile>>(await repository.GetByCaseKeyValueActiveOnlyAsync(key, value, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
