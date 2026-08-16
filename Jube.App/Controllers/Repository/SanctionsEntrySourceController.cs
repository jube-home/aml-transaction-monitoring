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
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using Code;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Dto;
    using Dto.Mapping;
    using Dto.Sanctions;
    using DynamicEnvironment;
    using Engine.Sanctions;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.VisualBasic.FileIO;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class SanctionEntrySourceController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly SanctionEntrySourceRepository repository;
        private readonly string userName;

        public SanctionEntrySourceController(ILog log,
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
                cfg.CreateMap<PermissionSpecificationDto, SanctionEntrySource>();
                cfg.CreateMap<SanctionEntrySource, SanctionEntrySourceDto>();
                cfg.CreateMap<DateTime?, DateTimeOffset?>().ConvertUsing<NullableDateTimeToDateTimeOffsetConverter>();
                cfg.CreateMap<DateTime, DateTimeOffset>().ConvertUsing(src => new DateTimeOffset(DateTime.SpecifyKind(src, DateTimeKind.Utc)));
            }, NullLoggerFactory.Instance);

            mapper = new Mapper(config);
            repository = new SanctionEntrySourceRepository(dbContext);
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

        [HttpPost("import")]
        public async Task<ActionResult> ImportAsync([FromForm] SanctionEntrySourceUploadDto model, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Landlord)
                {
                    return Forbid();
                }

                if (model.Files == null || model.Files.Length == 0)
                {
                    return BadRequest("A file is required.");
                }

                var sanctionEntrySource = await repository.GetByIdAsync(model.Id, token);

                if (sanctionEntrySource == null)
                {
                    return BadRequest("Could not locate Sanction Entry Source.");
                }

                await using var stream = model.Files.OpenReadStream();

                using var tfp = new TextFieldParser(stream);
                if (sanctionEntrySource.Delimiter != null)
                {
                    tfp.Delimiters = new[]
                    {
                        sanctionEntrySource.Delimiter.Value.ToString()
                    };

                    tfp.TextFieldType = FieldType.Delimited;

                    await ProcessTextFieldParserAsync(tfp, sanctionEntrySource, token);
                }
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }

            return Ok();
        }

        [HttpGet]
        public async Task<ActionResult<List<SanctionEntrySourceDto>>> GetAsync(CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Landlord)
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<SanctionEntrySourceDto>>(await repository.GetAsync(token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        private async Task ProcessTextFieldParserAsync(TextFieldParser tfp, SanctionEntrySource sanctionEntrySource, CancellationToken token = default)
        {
            var sanctionsEntryRepository = new SanctionsEntryRepository(dbContext);
            var sanctionEntryImportRepository = new SanctionEntryImportRepository(dbContext);
            var sanctionEntryRejectionRepository = new SanctionEntryRejectionRepository(dbContext);

            var sanctionEntryImport = await sanctionEntryImportRepository.InsertAsync(new SanctionEntryImport
            {
                SanctionEntrySourceId = sanctionEntrySource.Id,
                StartDate = DateTime.UtcNow,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow
            }, token).ConfigureAwait(false);

            var inserted = 0;
            var revived = 0;
            var unchanged = 0;

            try
            {
                var result = await SanctionEntryFileImporter.ImportAsync(tfp, sanctionEntrySource.Id,
                    sanctionEntrySource.MultiPartStringIndex, sanctionEntrySource.ReferenceIndex,
                    sanctionEntrySource.Skip ?? 0,
                    async (record, ct) =>
                    {
                        var sanctionEntry = new SanctionEntry
                        {
                            SanctionEntryElementValue = record.ElementValue,
                            SanctionEntrySourceId = sanctionEntrySource.Id,
                            SanctionPayload = record.Payload,
                            SanctionEntryReference = record.Reference,
                            SanctionEntryHash = record.Hash,
                            CreatedDate = DateTime.UtcNow,
                            CreatedUser = userName
                        };

                        var (_, outcome) = await sanctionsEntryRepository.UpsertAsync(sanctionEntry, ct)
                            .ConfigureAwait(false);

                        switch (outcome)
                        {
                            case SanctionEntryUpsertOutcome.Inserted:
                                inserted++;
                                break;
                            case SanctionEntryUpsertOutcome.Revived:
                                revived++;
                                break;
                            default:
                                unchanged++;
                                break;
                        }
                    },
                    async (rejection, ct) =>
                    {
                        await sanctionEntryRejectionRepository.InsertAsync(new SanctionEntryRejection
                        {
                            SanctionEntryImportId = sanctionEntryImport.Id,
                            SanctionEntrySourceId = sanctionEntrySource.Id,
                            RowNumber = rejection.RowNumber,
                            RawData = rejection.RawData,
                            ReasonId = (int)rejection.ReasonId,
                            CreatedDate = DateTime.UtcNow
                        }, ct).ConfigureAwait(false);
                    },
                    log,
                    token).ConfigureAwait(false);

                var removed = await SanctionEntryFileImporter.ReconcileRemovedAsync(sanctionsEntryRepository,
                    sanctionEntrySource.Id, result.Hashes, userName, log, token).ConfigureAwait(false);

                sanctionEntryImport.EndDate = DateTime.UtcNow;
                sanctionEntryImport.TotalRows = result.TotalRows;
                sanctionEntryImport.InsertedCount = inserted;
                sanctionEntryImport.RevivedCount = revived;
                sanctionEntryImport.UnchangedCount = unchanged;
                sanctionEntryImport.RemovedCount = removed.Count;
                sanctionEntryImport.RejectedCount = result.RejectedRows;
                sanctionEntryImport.Successful = 1;

                await sanctionEntryImportRepository.UpdateAsync(sanctionEntryImport, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                sanctionEntryImport.EndDate = DateTime.UtcNow;
                sanctionEntryImport.Successful = 0;
                sanctionEntryImport.ErrorMessage = ex.Message;

                await sanctionEntryImportRepository.UpdateAsync(sanctionEntryImport, token).ConfigureAwait(false);

                log.Error($"ProcessTextFieldParser: has produced an error {ex}");
            }
        }
    }
}
