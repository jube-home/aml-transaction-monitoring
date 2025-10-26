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
    using System.Collections.Generic;
    using Code;
    using Data.Context;
    using Data.Repository;
    using Dto;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CaseWorkflowMacroExecutionController : Controller
    {
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string userName;

        public CaseWorkflowMacroExecutionController(ILog log,
            DynamicEnvironment dynamicEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            this.log = log;
            this.dynamicEnvironment = dynamicEnvironment;

            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(this.dynamicEnvironment.AppSettings("ConnectionString"));
            permissionValidation = new PermissionValidation(dbContext, userName);
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
        public ActionResult<CaseWorkflowMacroExecutionDto> Execute([FromBody] CaseWorkflowMacroExecutionDto model)
        {
            if (!permissionValidation.Validate(new[]
                {
                    1
                }))
            {
                return Forbid();
            }

            if (model.Payload == null)
            {
                return Ok(model);
            }

            var jObject = JObject.Parse(model.Payload);

            var values = new Dictionary<string, string>();
            foreach (var (key, value) in jObject)
            {
                if (value != null)
                {
                    values.Add(key, value.ToString());
                }
            }

            var caseWorkflowMacroRepository = new CaseWorkflowMacroRepository(dbContext, userName);

            var caseWorkflowMacro = caseWorkflowMacroRepository.GetById(model.CaseWorkflowMacroId);

            if (caseWorkflowMacro.EnableNotification != 1 && caseWorkflowMacro.EnableHttpEndpoint != 1)
            {
                return Ok(model);
            }

            if (caseWorkflowMacro.EnableNotification == 1)
            {
                var notification = new Notification(log, dynamicEnvironment);
                notification.Send(caseWorkflowMacro.NotificationTypeId ?? 1,
                    caseWorkflowMacro.NotificationDestination,
                    caseWorkflowMacro.NotificationSubject,
                    caseWorkflowMacro.NotificationBody, values);
            }

            if (caseWorkflowMacro.EnableHttpEndpoint != 1)
            {
                return Ok(model);
            }

            var sendHttpEndpoint = new SendHttpEndpoint();
            if (caseWorkflowMacro.HttpEndpointTypeId != null)
            {
                sendHttpEndpoint.Send(caseWorkflowMacro.HttpEndpoint,
                    caseWorkflowMacro.HttpEndpointTypeId.Value
                    , values);
            }

            return Ok(model);
        }
    }
}
