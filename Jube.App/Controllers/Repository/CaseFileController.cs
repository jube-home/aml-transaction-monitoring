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

            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            permissionValidation = new PermissionValidation(dbContext, userName);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CaseFile, CaseFileDto>();
                cfg.CreateMap<CaseFileDto, CaseFile>();
                cfg.CreateMap<List<CaseFile>, List<CaseFileDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
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
        public ActionResult<CaseFileDto> FileUpload(IEnumerable<IFormFile> files, string caseKey, string caseKeyValue,
            int caseId)
        {
            if (!permissionValidation.Validate(new[]
                {
                    1
                }))
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
                file.CopyTo(ms);

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

                return Ok(mapper.Map<CaseFileDto>(repository.Insert(model)));
            }

            return Ok();
        }

        [HttpPost("Remove")]
        public ActionResult FileRemove(int id)
        {
            if (!permissionValidation.Validate(new[]
                {
                    1
                }))
            {
                return Forbid();
            }

            repository.Delete(id);

            return Ok();
        }

        [HttpGet]
        public ActionResult Generate(int id)
        {
            if (!permissionValidation.Validate(new[]
                {
                    1
                }))
            {
                return Forbid();
            }

            var model = repository.GetById(id);
            return new FileContentResult(model.Object, model.ContentType);
        }

        [HttpGet("ByCaseKeyValue")]
        public ActionResult<List<CaseFileDto>> GetByCaseKeyValue(string key, string value)
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

                return Ok(mapper.Map<List<CaseFile>>(repository.GetByCaseKeyValue(key, value)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
