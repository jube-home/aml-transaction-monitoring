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
    public class CaseNoteController : Controller
    {
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly JsonSerializationHelper jsonSerializationHelper;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseRepository repositoryCase;
        private readonly CaseNoteRepository repositoryCaseNote;
        private readonly CaseWorkflowActionRepository repositoryCaseWorkflowAction;
        private readonly CaseWorkflowStatusRepository repositoryCaseWorkflowStatus;
        private readonly string userName;
        private readonly IValidator<CaseNoteDto> validator;

        public CaseNoteController(ILog log,
            DynamicEnvironment dynamicEnvironment
            , IHttpContextAccessor httpContextAccessor, JsonSerializationHelper jsonSerializationHelper)
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
                cfg.CreateMap<CaseNote, CaseNoteDto>();
                cfg.CreateMap<CaseNoteDto, CaseNote>();
                cfg.CreateMap<DateTime?, DateTimeOffset?>().ConvertUsing<NullableDateTimeToDateTimeOffsetConverter>();
                cfg.CreateMap<DateTime, DateTimeOffset>().ConvertUsing(src => new DateTimeOffset(DateTime.SpecifyKind(src, DateTimeKind.Utc)));
            }, NullLoggerFactory.Instance);

            mapper = new Mapper(config);
            repositoryCaseNote = new CaseNoteRepository(dbContext, userName);
            repositoryCase = new CaseRepository(dbContext, userName);
            repositoryCaseWorkflowAction = new CaseWorkflowActionRepository(dbContext, userName);
            repositoryCaseWorkflowStatus = new CaseWorkflowStatusRepository(dbContext, userName);
            validator = new CaseNoteDtoValidator();
            this.dynamicEnvironment = dynamicEnvironment;
            this.jsonSerializationHelper = jsonSerializationHelper;
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
        public async Task<ActionResult<CaseNoteDto>> InsertAsync([FromBody] CaseNoteDto model, CancellationToken token = default)
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
                return BadRequest();
            }

            var existingCase = await repositoryCase.GetByIdActiveOnlyAsync(model.CaseId, token);

            if (existingCase == null)
            {
                return BadRequest();
            }

            var caseWorkflowAction = await repositoryCaseWorkflowAction.GetByIdActiveOnlyAsync(model.ActionId, token);

            if (caseWorkflowAction == null)
            {
                return BadRequest();
            }

            var caseWorkflowStatus = await repositoryCaseWorkflowStatus.GetByGuidAsync(existingCase.CaseWorkflowStatusGuid, token);

            if (caseWorkflowStatus == null)
            {
                return BadRequest();
            }

            var caseNote = await repositoryCaseNote.InsertAsync(mapper.Map<CaseNote>(model), token);

            if (caseWorkflowAction.EnableNotification != 1 && caseWorkflowAction.EnableHttpEndpoint != 1)
            {
                return Ok(caseNote);
            }

            var payload = JsonConvert.DeserializeObject<EntityAnalysisModelInstanceEntryPayload>(existingCase.Json, jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

            if (caseWorkflowAction.EnableNotification == 1)
            {
                var notification = new Notification(log, dynamicEnvironment);
                var notificationSubject = payload.ReplaceTokens(caseWorkflowAction.NotificationSubject);
                var notificationDestination = payload.ReplaceTokens(caseWorkflowAction.NotificationDestination);
                var notificationBody = payload.ReplaceTokens(caseWorkflowAction.NotificationBody);

                await notification.SendAsync(caseWorkflowAction.NotificationTypeId ?? 1,
                    notificationDestination,
                    notificationSubject,
                    notificationBody, token);
            }

            if (caseWorkflowAction.EnableHttpEndpoint != 1)
            {
                return Ok(await repositoryCaseNote.InsertAsync(caseNote, token));
            }

            var endpoint = payload.ReplaceTokens(caseWorkflowAction.HttpEndpoint);

            if (caseWorkflowAction.HttpEndpointTypeId == 1)
            {
                await SendHttpEndpoint.PostAsync(endpoint, PreparePostBodyString(caseNote, existingCase, caseWorkflowStatus, caseWorkflowAction, payload), log);
            }
            else
            {
                await SendHttpEndpoint.GetAsync(endpoint, log);
            }

            return Ok(caseNote);
        }

        private string PreparePostBodyString(CaseNote caseNote, Case existingCase, CaseWorkflowStatus caseWorkflowStatus, CaseWorkflowAction caseWorkflowAction, EntityAnalysisModelInstanceEntryPayload payload)
        {
            var jObject = JObject.FromObject(caseNote, jsonSerializationHelper.ArchiveJsonSerializer);
            jObject["caseWorkflowActionName"] = caseWorkflowAction.Name;

            var caseJObject = JObject.FromObject(existingCase, jsonSerializationHelper.ArchiveJsonSerializer);
            caseJObject.Remove("json");
            jObject["case"] = caseJObject;

            caseJObject["caseWorkflowStatus"] = caseWorkflowStatus.Name;

            var payloadJObject = JObject.FromObject(payload, jsonSerializationHelper.ArchiveJsonSerializer);
            caseJObject["payload"] = payloadJObject;

            return jObject.ToString();
        }

        [HttpGet("ByCaseKeyValue")]
        public async Task<ActionResult<List<CaseNoteDto>>> GetByCaseKeyValueAsync(string key, string value, CancellationToken token = default)
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

                return Ok(mapper.Map<List<CaseNote>>(await repositoryCaseNote.GetByCaseKeyValueAsync(key, value, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
