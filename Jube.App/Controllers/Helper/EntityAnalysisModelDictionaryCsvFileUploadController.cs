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

namespace Jube.App.Controllers.Helper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Code;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class EntityAnalysisModelDictionaryCsvFileUploadController : Controller
    {

        private readonly DbContext dbContext;
        private readonly EntityAnalysisModelDictionaryCsvFileUploadRepository
            entityAnalysisModelDictionaryCsvFileUploadRepository;
        private readonly EntityAnalysisModelDictionaryKvpRepository entityAnalysisModelDictionaryKvpRepository;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string userName;

        public EntityAnalysisModelDictionaryCsvFileUploadController(ILog log, IHttpContextAccessor httpContextAccessor,
            DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            this.log = log;

            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            permissionValidation = new PermissionValidation(dbContext, userName);

            entityAnalysisModelDictionaryKvpRepository =
                new EntityAnalysisModelDictionaryKvpRepository(dbContext, userName);

            entityAnalysisModelDictionaryCsvFileUploadRepository =
                new EntityAnalysisModelDictionaryCsvFileUploadRepository(dbContext, userName);
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

        [HttpPost]
        public IActionResult Upload(List<IFormFile> files, int entityAnalysisModelDictionaryId)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        4
                    }))
                {
                    return Forbid();
                }

                foreach (var file in files)
                {
                    using var reader = new StreamReader(file.OpenReadStream());

                    var records = 0;
                    var errors = 0;

                    while (reader.Peek() >= 0)
                    {
                        try
                        {
                            var splits = reader.ReadLine()?.Split(",");
                            if (splits != null)
                            {
                                var entityAnalysisModelDictionaryKvp = entityAnalysisModelDictionaryKvpRepository
                                    .GetByIdKvpKey(entityAnalysisModelDictionaryId,
                                        splits[0]);

                                if (splits.Length > 1)
                                {
                                    if (entityAnalysisModelDictionaryKvp == null)
                                    {
                                        var entityAnalysisModelsDictionaryKvp = new EntityAnalysisModelDictionaryKvp
                                        {
                                            EntityAnalysisModelDictionaryId = entityAnalysisModelDictionaryId,
                                            KvpKey = splits[0],
                                            KvpValue = Double.Parse(splits[1])
                                        };

                                        entityAnalysisModelDictionaryKvpRepository.Insert(
                                            entityAnalysisModelsDictionaryKvp);
                                    }
                                    else
                                    {
                                        entityAnalysisModelDictionaryKvp.KvpValue = Double.Parse(splits[1]);
                                        entityAnalysisModelDictionaryKvpRepository.Update(entityAnalysisModelDictionaryKvp);
                                    }
                                }
                            }

                            records += 1;
                        }
                        catch (Exception e)
                        {
                            log.Error(e.ToString());
                        }
                    }

                    var entityAnalysisModelDictionaryCsvFileUpload = new EntityAnalysisModelDictionaryCsvFileUpload
                    {
                        FileName = file.FileName,
                        Records = records,
                        Errors = errors,
                        Length = file.Length,
                        EntityAnalysisModelDictionaryId = entityAnalysisModelDictionaryId
                    };

                    entityAnalysisModelDictionaryCsvFileUploadRepository.Insert(
                        entityAnalysisModelDictionaryCsvFileUpload);
                }

                return StatusCode(200);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
