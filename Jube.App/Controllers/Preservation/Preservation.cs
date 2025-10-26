namespace Jube.App.Controllers.Preservation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Code;
    using Cryptography.Exceptions;
    using Data.Context;
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

            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));

            permissionValidation = new PermissionValidation(dbContext, userName);
            this.log = log;
            this.dynamicEnvironment = dynamicEnvironment;
        }

        [HttpPost("Import")]
        public IActionResult Upload(List<IFormFile> files, string password, bool exhaustive,
            bool suppressions,
            bool lists, bool dictionaries, bool visualisations)
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
                var importExportOptions = new ImportExportOptions
                {
                    Password = password,
                    Exhaustive = exhaustive,
                    Suppressions = suppressions,
                    Lists = lists,
                    Dictionaries = dictionaries,
                    Visualisations = visualisations
                };

                var preservation = new Preservation(dbContext, userName,
                    dynamicEnvironment.AppSettings("PreservationSalt"));

                foreach (var file in files)
                {
                    using var stream = file.OpenReadStream();
                    using var reader = new BinaryReader(stream);
                    var bytes = reader.ReadBytes((int)stream.Length);
                    preservation.Import(bytes, importExportOptions);
                    return Ok();
                }

                return BadRequest();
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
        public ActionResult<string> Preview(bool exhaustive, bool suppressions, bool lists, bool dictionaries,
            bool visualisations)
        {
            if (!permissionValidation.Validate(new[]
                {
                    38
                }))
            {
                return Forbid();
            }

            var importExportOptions = new ImportExportOptions
            {
                Exhaustive = exhaustive,
                Suppressions = suppressions,
                Lists = lists,
                Dictionaries = dictionaries,
                Visualisations = visualisations
            };

            var preservation = new Preservation(dbContext, userName);
            var payload = preservation.ExportPeek(importExportOptions);
            return payload.Yaml;
        }

        [HttpGet("Export")]
        public IActionResult Export(string password, bool exhaustive, bool suppressions, bool lists, bool dictionaries,
            bool visualisations)
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
                    dynamicEnvironment.AppSettings("PreservationSalt"));

                var importExportOptions = new ImportExportOptions
                {
                    Password = password,
                    Exhaustive = exhaustive,
                    Suppressions = suppressions,
                    Lists = lists,
                    Dictionaries = dictionaries,
                    Visualisations = visualisations
                };

                var export = preservation.Export(importExportOptions);
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
