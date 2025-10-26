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
    using System.Linq;
    using System.Threading.Tasks;
    using Code;
    using Code.signalr;
    using Data.Context;
    using DynamicEnvironment;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.SignalR;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class RegisterSignalrConnectionController : Controller
    {
        private readonly DbContext dbContext;
        private readonly PermissionValidation permissionValidation;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly IHubContext<WatcherHub> watcherHub;

        public RegisterSignalrConnectionController(IHubContext<WatcherHub> watcherHub,
            IHttpContextAccessor httpContextAccessor, DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            permissionValidation = new PermissionValidation(dbContext, userName);

            tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
            this.watcherHub = watcherHub;
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

        [HttpGet("{id}")]
        public async Task<IActionResult> RegisterSignalrConnectionByConnectionIdAsync(string id)
        {
            if (!permissionValidation.Validate(new[]
                {
                    30
                }))
            {
                return Forbid();
            }

            await watcherHub.Groups.AddToGroupAsync(id, "Tenant_" + tenantRegistryId).ConfigureAwait(false);

            return Ok();
        }
    }
}
