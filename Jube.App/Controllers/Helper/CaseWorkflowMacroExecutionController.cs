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
    using System.Threading;
    using System.Threading.Tasks;
    using Case;
    using Code;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Dto;
    using DynamicEnvironment;
    using Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Extensions;
    using Engine.Helpers;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CaseWorkflowMacroExecutionController : Controller
    {
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly JsonSerializationHelper jsonSerializationHelper;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseRepository repositoryCase;
        private readonly CaseWorkflowMacroRepository repositoryCaseWorkflowMacro;
        private readonly CaseWorkflowStatusRepository repositoryCaseWorkflowStatus;
        private readonly string userName;

        public CaseWorkflowMacroExecutionController(ILog log,
            DynamicEnvironment dynamicEnvironment, IHttpContextAccessor httpContextAccessor, JsonSerializationHelper jsonSerializationHelper)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), log);
            this.log = log;
            this.dynamicEnvironment = dynamicEnvironment;
            repositoryCase = new CaseRepository(dbContext, userName);
            repositoryCaseWorkflowMacro = new CaseWorkflowMacroRepository(dbContext, userName);
            repositoryCaseWorkflowStatus = new CaseWorkflowStatusRepository(dbContext, userName);
            permissionValidation = new PermissionValidation(dbContext, userName, log);
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
        public async Task<ActionResult<CaseWorkflowMacroExecutionDto>> ExecuteAsync([FromBody] CaseWorkflowMacroExecutionDto model, CancellationToken token = default)
        {
            if (!permissionValidation.Validate(new[]
                {
                    1
                }))
            {
                return Forbid();
            }

            var existingCase = await repositoryCase.GetByIdActiveOnlyAsync(model.CaseId, token);

            if (existingCase == null)
            {
                return BadRequest();
            }

            var caseWorkflowMacro = await repositoryCaseWorkflowMacro.GetByIdActiveOnlyAsync(model.CaseWorkflowMacroId, token);

            if (caseWorkflowMacro == null)
            {
                return BadRequest();
            }

            var caseWorkflowStatus = await repositoryCaseWorkflowStatus.GetByGuidAsync(existingCase.CaseWorkflowStatusGuid, token);

            if (caseWorkflowStatus == null)
            {
                return BadRequest();
            }

            var context = JsonConvert.DeserializeObject<EntityAnalysisModelInstanceEntryPayload>(existingCase.Json, jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

            if (caseWorkflowMacro.EnableNotification == 1)
            {
                var notification = new Notification(log, dynamicEnvironment);
                var notificationSubject = context.ReplaceTokens(caseWorkflowMacro.NotificationSubject);
                var notificationDestination = context.ReplaceTokens(caseWorkflowMacro.NotificationDestination);
                var notificationBody = context.ReplaceTokens(caseWorkflowMacro.NotificationBody);

                await notification.SendAsync(caseWorkflowMacro.NotificationTypeId ?? 1,
                    notificationDestination,
                    notificationSubject,
                    notificationBody, token);
            }

            if (caseWorkflowMacro.EnableHttpEndpoint == 1)
            {
                var endpoint = context.ReplaceTokens(caseWorkflowMacro.HttpEndpoint);

                if (caseWorkflowMacro.HttpEndpointTypeId == 1)
                {
                    await SendHttpEndpoint.PostAsync(endpoint, PreparePostBodyString(model, existingCase, context, caseWorkflowStatus, caseWorkflowMacro), log);
                }
                else
                {
                    await SendHttpEndpoint.GetAsync(endpoint, log);
                }
            }

            return Ok(model);
        }

        private string PreparePostBodyString(CaseWorkflowMacroExecutionDto model, Case existingCase, EntityAnalysisModelInstanceEntryPayload payload, CaseWorkflowStatus caseWorkflowStatus, CaseWorkflowMacro caseWorkflowMacro)
        {
            var jObject = JObject.FromObject(model, jsonSerializationHelper.ArchiveJsonSerializer);
            jObject["caseWorkflowMacroName"] = caseWorkflowMacro.Name;

            var caseJObject = JObject.FromObject(existingCase, jsonSerializationHelper.ArchiveJsonSerializer);
            caseJObject.Remove("json");
            jObject["case"] = caseJObject;

            caseJObject["caseWorkflowStatus"] = caseWorkflowStatus.Name;

            var payloadJObject = JObject.FromObject(payload, jsonSerializationHelper.ArchiveJsonSerializer);
            caseJObject["payload"] = payloadJObject;

            return jObject.ToString();
        }
    }
}
