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

using System.Linq;
using System.Threading.Tasks;
using Jube.App.Code;
using Jube.App.Code.signalr;
using Jube.Data.Context;
using Jube.Engine.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Jube.App.Controllers.Helper
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class RegisterSignalrConnectionController : Controller
    {
        private readonly DbContext _dbContext;
        private readonly PermissionValidation _permissionValidation;
        private readonly int _tenantRegistryId;
        private readonly string _userName;
        private readonly IHubContext<WatcherHub> _watcherHub;

        public RegisterSignalrConnectionController(IHubContext<WatcherHub> watcherHub,
            IHttpContextAccessor httpContextAccessor, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
                _userName = httpContextAccessor.HttpContext.User.Identity.Name;

            _dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            _permissionValidation = new PermissionValidation(_dbContext, _userName);

            _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == _userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
            _watcherHub = watcherHub;
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

        [HttpGet("{id}")]
        public async Task<IActionResult> RegisterSignalrConnectionByConnectionIdAsync(string id)
        {
            if (!_permissionValidation.Validate(new[] { 30 })) return Forbid();

            await _watcherHub.Groups.AddToGroupAsync(id, "Tenant_" + _tenantRegistryId).ConfigureAwait(false);

            return Ok();
        }
    }
}