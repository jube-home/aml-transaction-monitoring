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
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
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

            dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), log);
            permissionValidation = new PermissionValidation(dbContext, userName, log);

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
        public async Task<ActionResult> UploadAsync(List<IFormFile> files, int entityAnalysisModelDictionaryId, CancellationToken token = default)
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
                            var splits = (await reader.ReadLineAsync(token))?.Split(",");
                            if (splits != null)
                            {
                                var entityAnalysisModelDictionaryKvp = await entityAnalysisModelDictionaryKvpRepository
                                    .GetByIdKvpKeyAsync(entityAnalysisModelDictionaryId,
                                        splits[0], token);

                                if (splits.Length > 1)
                                {
                                    var deleteExpiryDateSpecified = splits.Length > 2;
                                    var deleteExpiryDate = deleteExpiryDateSpecified
                                        ? ParseDeleteExpiryDate(splits[2])
                                        : null;

                                    if (entityAnalysisModelDictionaryKvp == null)
                                    {
                                        var entityAnalysisModelsDictionaryKvp = new EntityAnalysisModelDictionaryKvp
                                        {
                                            EntityAnalysisModelDictionaryId = entityAnalysisModelDictionaryId,
                                            KvpKey = splits[0],
                                            KvpValue = Double.Parse(splits[1]),
                                            DeleteExpiryDate = deleteExpiryDate
                                        };

                                        await entityAnalysisModelDictionaryKvpRepository.InsertAsync(
                                            entityAnalysisModelsDictionaryKvp, token);
                                    }
                                    else
                                    {
                                        entityAnalysisModelDictionaryKvp.KvpValue = Double.Parse(splits[1]);

                                        if (deleteExpiryDateSpecified)
                                        {
                                            entityAnalysisModelDictionaryKvp.DeleteExpiryDate = deleteExpiryDate;
                                        }

                                        await entityAnalysisModelDictionaryKvpRepository.UpdateAsync(entityAnalysisModelDictionaryKvp, token);
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

                    await entityAnalysisModelDictionaryCsvFileUploadRepository.InsertAsync(
                        entityAnalysisModelDictionaryCsvFileUpload, token);
                }

                return StatusCode(200);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        private static DateTime? ParseDeleteExpiryDate(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   DateTime.TryParseExact(value, "O", CultureInfo.InvariantCulture,
                       DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
                ? parsed
                : null;
        }
    }
}
