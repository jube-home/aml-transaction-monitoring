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

namespace Jube.App.Code.signalr
{
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Repository;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.SignalR;
    
    public class ServiceChangeHub(DynamicEnvironment dynamicEnvironment, ILog log) : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userName = Context.User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(userName))
            {
                await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                    dynamicEnvironment.AppSettings("ConnectionString"), log);

                var tenantRegistryId = await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, userName);

                if (tenantRegistryId is { } id)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Tenant_" + id);
                }
            }

            await base.OnConnectedAsync();
        }
    }
}
