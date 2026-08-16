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
    using AutoMapper;
    using Case;
    using Code;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Dto;
    using Dto.Mapping;
    using DynamicEnvironment;
    using Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Extensions;
    using Engine.Helpers;
    using FluentValidation;
    using FluentValidation.Results;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Validators;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CaseWorkflowFormEntryController : Controller
    {
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly JsonSerializationHelper jsonSerializationHelper;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseWorkflowFormRepository repositoryCaseWorkflowForm;
        private readonly CaseWorkflowFormEntryRepository repositoryCaseWorkflowFormEntry;
        private readonly CaseWorkflowStatusRepository repositoryCaseWorkflowStatus;
        private readonly string userName;
        private readonly IValidator<CaseWorkflowFormEntryDto> validator;

        public CaseWorkflowFormEntryController(ILog log,
            DynamicEnvironment dynamicEnvironment, IHttpContextAccessor httpContextAccessor, JsonSerializationHelper jsonSerializationHelper)
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
                cfg.CreateMap<CaseWorkflowFormEntry, CaseWorkflowFormEntryDto>();
                cfg.CreateMap<CaseWorkflowFormEntryDto, CaseWorkflowFormEntry>();
                cfg.CreateMap<DateTime?, DateTimeOffset?>().ConvertUsing<NullableDateTimeToDateTimeOffsetConverter>();
                cfg.CreateMap<DateTime, DateTimeOffset>().ConvertUsing(src => new DateTimeOffset(DateTime.SpecifyKind(src, DateTimeKind.Utc)));
            }, NullLoggerFactory.Instance);

            mapper = new Mapper(config);
            repositoryCaseWorkflowFormEntry = new CaseWorkflowFormEntryRepository(dbContext, userName);
            repositoryCaseWorkflowStatus = new CaseWorkflowStatusRepository(dbContext, userName);
            repositoryCaseWorkflowForm = new CaseWorkflowFormRepository(dbContext, userName);
            this.jsonSerializationHelper = jsonSerializationHelper;
            validator = new CaseWorkflowFormEntryDtoValidator();
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

        [HttpPost]
        [ProducesResponseType(typeof(CaseWorkflowFormEntryDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<CaseWorkflowFormEntryDto>> CreateAsync([FromBody] CaseWorkflowFormEntryDto model, CancellationToken token = default)
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

                var results = await validator.ValidateAsync(model, token);

                if (!results.IsValid)
                {
                    return BadRequest(results);
                }

                var repositoryCase = new CaseRepository(dbContext, userName);
                var existingCase = await repositoryCase.GetByIdActiveOnlyAsync(model.CaseId, token);

                if (existingCase == null || model.Payload == null)
                {
                    return BadRequest();
                }

                var caseWorkflowStatus = await repositoryCaseWorkflowStatus.GetByGuidAsync(existingCase.CaseWorkflowStatusGuid, token);

                if (caseWorkflowStatus == null)
                {
                    return BadRequest();
                }

                var caseWorkflowForm = await repositoryCaseWorkflowForm.GetByIdAsync(model.CaseWorkflowFormId, token);

                if (caseWorkflowForm == null)
                {
                    return BadRequest();
                }

                var caseWorkflowEntry = mapper.Map<CaseWorkflowFormEntry>(model);
                var entry = await repositoryCaseWorkflowFormEntry.InsertAsync(caseWorkflowEntry, token);

                var repositoryCaseWorkflowFormEntryValue =
                    new CaseWorkflowFormEntryValueRepository(dbContext, userName);

                foreach (var (key, value) in model.Payload)
                {
                    if (value == null)
                    {
                        continue;
                    }

                    var caseWorkflowFormEntryValue = new CaseWorkflowFormEntryValue
                    {
                        CaseWorkflowFormEntryId = entry.Id,
                        Name = key,
                        Value = value.ToString()
                    };

                    await repositoryCaseWorkflowFormEntryValue.InsertAsync(caseWorkflowFormEntryValue, token);
                }

                if (caseWorkflowForm.EnableNotification != 1 && caseWorkflowForm.EnableHttpEndpoint != 1)
                {
                    return Ok(entry);
                }

                var archivePayload = JsonConvert.DeserializeObject<EntityAnalysisModelInstanceEntryPayload>(existingCase.Json, jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

                if (caseWorkflowForm.EnableNotification == 1)
                {
                    var notification = new Notification(log, dynamicEnvironment);
                    var notificationSubject = archivePayload.ReplaceTokens(caseWorkflowForm.NotificationSubject);
                    var notificationDestination = archivePayload.ReplaceTokens(caseWorkflowForm.NotificationDestination);
                    var notificationBody = archivePayload.ReplaceTokens(caseWorkflowForm.NotificationBody);

                    await notification.SendAsync(caseWorkflowForm.NotificationTypeId ?? 1,
                        notificationDestination,
                        notificationSubject,
                        notificationBody, token);
                }

                if (caseWorkflowForm.EnableHttpEndpoint == 1)
                {
                    var endpoint = archivePayload.ReplaceTokens(caseWorkflowForm.HttpEndpoint);
                    if (caseWorkflowStatus.HttpEndpointTypeId == 1)
                    {
                        await SendHttpEndpoint.PostAsync(endpoint, PreparePostBodyString(caseWorkflowEntry, model.Payload, archivePayload, existingCase, caseWorkflowStatus, caseWorkflowForm), log);
                    }
                    else
                    {
                        await SendHttpEndpoint.GetAsync(endpoint, log);
                    }
                }

                return Ok(entry);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        private string PreparePostBodyString(CaseWorkflowFormEntry caseWorkflowEntry, Dictionary<string, object> formPayload, EntityAnalysisModelInstanceEntryPayload payload, Case existingCase, CaseWorkflowStatus caseWorkflowStatus, CaseWorkflowForm caseWorkflowForm)
        {
            var jObject = JObject.FromObject(caseWorkflowEntry, jsonSerializationHelper.ArchiveJsonSerializer);
            var payloadJObject = JObject.FromObject(formPayload);
            jObject["payload"] = payloadJObject;
            jObject["caseWorkflowFormName"] = caseWorkflowForm.Name;

            var caseJObject = JObject.FromObject(existingCase, jsonSerializationHelper.ArchiveJsonSerializer);
            caseJObject.Remove("json");
            jObject["case"] = caseJObject;

            caseJObject["caseWorkflowStatus"] = caseWorkflowStatus.Name;

            var casePayloadJObject = JObject.FromObject(payload, jsonSerializationHelper.ArchiveJsonSerializer);
            caseJObject["payload"] = casePayloadJObject;

            return jObject.ToString();
        }

        [HttpGet("ByCaseKeyValue")]
        public async Task<ActionResult<List<CaseWorkflowFormEntryDto>>> GetByCaseKeyValueAsync(string key, string value, CancellationToken token = default)
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

                return Ok(mapper.Map<List<CaseWorkflowFormEntry>>(await repositoryCaseWorkflowFormEntry.GetByCaseKeyValueActiveOnlyAsync(key, value, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
