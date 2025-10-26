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
    using Code;
    using Code.signalr;
    using Data.Context;
    using Data.Repository;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.SignalR;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ActivationWatcherController : Controller
    {
        private readonly DefaultContractResolver contractResolver;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly ActivationWatcherRepository repository;
        private readonly string userName;
        private readonly IHubContext<WatcherHub> watcherHub;

        public ActivationWatcherController(ILog log, IHubContext<WatcherHub> watcherHub,
            IHttpContextAccessor httpContextAccessor, DynamicEnvironment dynamicEnvironment,
            DefaultContractResolver contractResolver)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            this.log = log;

            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            permissionValidation = new PermissionValidation(dbContext, userName);

            repository = new ActivationWatcherRepository(dbContext, userName);
            this.watcherHub = watcherHub;
            this.contractResolver = contractResolver;
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

        [HttpGet("Replay")]
        public ActionResult Replay(DateTime dateFrom, DateTime dateTo)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        30
                    }))
                {
                    return Forbid();
                }

                foreach (var activationWatcher in repository.GetByDateRangeAscending(dateFrom, dateTo, 1000))
                {
                    var stringRepresentationOfObj = JsonConvert.SerializeObject(activationWatcher, new JsonSerializerSettings
                    {
                        ContractResolver = contractResolver
                    });

                    watcherHub.Clients.Group("Tenant_" + activationWatcher.TenantRegistryId).SendAsync("ReceiveMessage", "Replay", stringRepresentationOfObj);
                }

                return Ok();
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
