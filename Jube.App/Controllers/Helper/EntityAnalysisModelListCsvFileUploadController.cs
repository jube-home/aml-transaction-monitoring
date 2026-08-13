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
    public class EntityAnalysisModelListCsvFileUploadController : Controller
    {
        private readonly DbContext dbContext;
        private readonly EntityAnalysisModelListValueCsvFileUploadRepository
            entityAnalysisModelListValueCsvFileUploadRepository;

        private readonly EntityAnalysisModelListValueRepository entityAnalysisModelListValueRepository;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string userName;

        public EntityAnalysisModelListCsvFileUploadController(ILog log, IHttpContextAccessor httpContextAccessor,
            DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            this.log = log;

            dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), log);
            permissionValidation = new PermissionValidation(dbContext, userName, log);

            entityAnalysisModelListValueRepository = new EntityAnalysisModelListValueRepository(dbContext, userName);
            entityAnalysisModelListValueCsvFileUploadRepository =
                new EntityAnalysisModelListValueCsvFileUploadRepository(dbContext, userName);
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
        public async Task<ActionResult> UploadAsync(List<IFormFile> files, int entityAnalysisModelListId, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        3
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

                            var entityAnalysisModelsListValue = new EntityAnalysisModelListValue
                            {
                                EntityAnalysisModelListId = entityAnalysisModelListId,
                                ListValue = splits?[0],
                                DeleteExpiryDate = splits is {Length: > 1} ? ParseDeleteExpiryDate(splits[1]) : null
                            };

                            await entityAnalysisModelListValueRepository.InsertAsync(entityAnalysisModelsListValue, token);

                            records += 1;
                        }
                        catch (Exception e)
                        {
                            errors++;
                            log.Error(e.ToString());
                        }
                    }

                    var entityAnalysisModelListCsvFileUpload = new EntityAnalysisModelListCsvFileUpload
                    {
                        FileName = file.FileName,
                        Records = records,
                        Errors = errors,
                        Length = file.Length,
                        EntityAnalysisModelListId = entityAnalysisModelListId
                    };

                    await entityAnalysisModelListValueCsvFileUploadRepository.InsertAsync(entityAnalysisModelListCsvFileUpload, token);
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
