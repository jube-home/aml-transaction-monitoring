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

namespace Jube.App.Controllers.Preservation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Code;
    using Cryptography.Exceptions;
    using Data.Context;
    using Dto;
    using DynamicEnvironment;
    using Jube.Preservation;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class PreservationController : Controller
    {
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;

        // ReSharper disable once NotAccessedField.Local
        private readonly ILog log;

        // ReSharper disable once NotAccessedField.Local
        private readonly PermissionValidation permissionValidation;
        private readonly string userName;

        public PreservationController(ILog log,
            IHttpContextAccessor httpContextAccessor, DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            this.log = log;
            dbContext = DataConnectionDbContext.GetNgpsqlDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            permissionValidation = new PermissionValidation(dbContext, userName, log);
            this.log = log;
            this.dynamicEnvironment = dynamicEnvironment;
        }

        [HttpPost("Import")]
        public async Task<ActionResult> UploadAsync(List<IFormFile> files, string password, CancellationToken token = default)
        {
            if (!permissionValidation.Validate(new[]
                {
                    38
                }))
            {
                return Forbid();
            }

            if (files.Count <= 0)
            {
                return BadRequest();
            }

            try
            {
                var importExportOptions = new ImportOptions()
                {
                    Password = password
                };

                var file = files.First();
                await using var stream = file.OpenReadStream();
                using var reader = new BinaryReader(stream, Encoding.Default, true);
                var bytes = reader.ReadBytes((int)stream.Length);

                await using var preservation = new Preservation(dbContext, userName,
                    dynamicEnvironment.AppSettings("PreservationSalt"),
                    dynamicEnvironment.AppSettings("JempFileLegacyEncryptionFallback").Equals("True", StringComparison.CurrentCultureIgnoreCase));

                await preservation.ImportAsync(bytes, importExportOptions, token).ConfigureAwait(false);
                return Ok();
            }
            catch (InvalidHmacException)
            {
                return BadRequest();
            }
            catch (InvalidDecryptionException)
            {
                return BadRequest();
            }
            catch (Exception ex)
            {
                log.Error($"Exception while exporting {ex}");
                throw;
            }
        }

        [HttpGet("ExportPeek")]
        [Produces("text/plain")]
        public async Task<ActionResult<string>> PreviewAsync(bool exhaustive, bool suppressions, bool lists, bool dictionaries,
            bool visualisations, CancellationToken token = default)
        {
            if (!permissionValidation.Validate(new[]
                {
                    38
                }))
            {
                return Forbid();
            }

            var importExportOptions = new ExportOptions
            {
                Exhaustive = exhaustive,
                Suppressions = suppressions,
                Lists = lists,
                Dictionaries = dictionaries,
                Visualisations = visualisations
            };

            var preservation = new Preservation(dbContext, userName);
            var payload = await preservation.ExportPeekAsync(importExportOptions, token).ConfigureAwait(false);
            return payload.Yaml;
        }

        [HttpPost("Export")]
        public async Task<ActionResult> ExportAsync([FromBody] ImportExportOptionsDto model, CancellationToken token = default)
        {
            if (!permissionValidation.Validate(new[]
                {
                    38
                }))
            {
                return Forbid();
            }

            try
            {
                var preservation = new Preservation(dbContext, userName,
                    dynamicEnvironment.AppSettings("PreservationSalt"),
                    dynamicEnvironment.AppSettings("JempFileLegacyEncryptionFallback").Equals("True", StringComparison.CurrentCultureIgnoreCase));

                var importExportOptions = new ExportOptions
                {
                    Password = model.Password,
                    Exhaustive = model.Exhaustive,
                    Suppressions = model.Suppressions,
                    Lists = model.Lists,
                    Dictionaries = model.Dictionaries,
                    Visualisations = model.Visualisations,
                    Roles = model.Roles
                };

                var export = await preservation.ExportAsync(importExportOptions, token).ConfigureAwait(false);

                return File(export.EncryptedBytes, "application/octet-stream", $"{export.Guid}.jemp");
            }
            catch (Exception ex)
            {
                log.Error($"Error exporting {ex}");

                return StatusCode(500);
            }
        }
    }
}
