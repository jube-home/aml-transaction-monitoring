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

using System;
using System.Threading.Tasks;
using Jube.App.Code;
using Jube.Data.Context;
using Jube.Data.Query;
using Jube.Engine.Helpers;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jube.App.Controllers.Query
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class GetCaseJournalQueryController : Controller
    {
        private readonly DbContext _dbContext;
        private readonly ILog _log;
        private readonly PermissionValidation _permissionValidation;
        private readonly GetCaseJournalQuery _query;
        private readonly string _userName;

        public GetCaseJournalQueryController(ILog log,
            IHttpContextAccessor httpContextAccessor, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
                _userName = httpContextAccessor.HttpContext.User.Identity.Name;
            _log = log;

            _dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            _permissionValidation = new PermissionValidation(_dbContext, _userName);

            _query = new GetCaseJournalQuery(_dbContext, _userName);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext.Close();
                _dbContext.Dispose();
            }

            base.Dispose(disposing);
        }

        [HttpGet]
        public async Task<ActionResult> Get(string drillName, string drillValue, Guid caseWorkflowGuid, int limit,
            bool activationsOnly, double responseElevation)
        {
            try
            {
                if (!_permissionValidation.Validate(new[] { 1 })) return Forbid();

                return Ok(await _query.ExecuteAsync(drillName, drillValue, caseWorkflowGuid, limit,
                    activationsOnly ? 1 : 0, responseElevation).ConfigureAwait(false));
            }
            catch (Exception e)
            {
                _log.Error(e);
                return StatusCode(500);
            }
        }
    }
}