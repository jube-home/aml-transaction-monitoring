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

using Jube.Service.Reactivity.Interfaces;

namespace Jube.App.Code.ServiceChange
{
    using System;
    using System.Threading.Tasks;
    using log4net;
    using Microsoft.AspNetCore.SignalR;
    using signalr;
    
    public sealed class ServiceChangeRelay
    {
        // ReSharper disable once NotAccessedField.Local
        private IDisposable subscription;

        public Task StartAsync(IHubContext<ServiceChangeHub> serviceChangeHub, IServiceChangeBus serviceChangeBus, ILog log)
        {
            subscription = serviceChangeBus.Subscribe(async change =>
            {
                try
                {
                    await serviceChangeHub.Clients.Group("Tenant_" + change.TenantRegistryId)
                        .SendAsync("ServiceChange", change).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.Error($"ServiceChangeRelay: failed to relay {change.Area}.{change.Kind} to Tenant_{change.TenantRegistryId}", ex);
                }
            });

            return Task.CompletedTask;
        }
    }
}
