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
    using Code;
    using Data.Context;
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

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CaseWorkflowDisplayExecutionController : Controller
    {
        private readonly DbContext dbContext;
        private readonly JsonSerializationHelper jsonSerializationHelper;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseRepository repositoryCase;
        private readonly CaseWorkflowDisplayRepository repositoryCaseWorkflowDisplay;
        private readonly CaseWorkflowStatusRepository repositoryCaseWorkflowStatus;
        private readonly string userName;

        public CaseWorkflowDisplayExecutionController(
            DynamicEnvironment dynamicEnvironment, IHttpContextAccessor httpContextAccessor,
            JsonSerializationHelper jsonSerializationHelper, ILog log)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), log);
            repositoryCase = new CaseRepository(dbContext, userName);
            repositoryCaseWorkflowDisplay = new CaseWorkflowDisplayRepository(dbContext, userName);
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
        public async Task<ActionResult<string>> ExecuteAsync([FromBody] CaseWorkflowDisplayExecutionDto model, CancellationToken token = default)
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

            var caseWorkflowDisplay = await repositoryCaseWorkflowDisplay.GetByIdActiveOnlyAsync(model.CaseWorkflowDisplayId, token);

            if (caseWorkflowDisplay == null)
            {
                return BadRequest();
            }

            var caseWorkflowStatus = await repositoryCaseWorkflowStatus.GetByGuidAsync(existingCase.CaseWorkflowStatusGuid, token);

            if (caseWorkflowStatus == null)
            {
                return BadRequest();
            }

            var payload = JsonConvert.DeserializeObject<EntityAnalysisModelInstanceEntryPayload>(existingCase.Json, jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);
            var replacedHtml = payload.ReplaceTokens(caseWorkflowDisplay.Html);

            return Ok(replacedHtml);
        }
    }
}
