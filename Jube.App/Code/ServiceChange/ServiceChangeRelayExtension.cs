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
    using System.Threading.Tasks;
    using log4net;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.DependencyInjection;
    using signalr;

    public static class ServiceChangeRelayExtension
    {
        public static async Task StartServiceChangeRelayAsync(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();

            var relay = scope.ServiceProvider.GetService<ServiceChangeRelay>();
            if (relay == null)
            {
                return;
            }

            var serviceChangeHub = scope.ServiceProvider.GetRequiredService<IHubContext<ServiceChangeHub>>();
            var serviceChangeBus = scope.ServiceProvider.GetRequiredService<IServiceChangeBus>();
            var log = scope.ServiceProvider.GetRequiredService<ILog>();

            await relay.StartAsync(serviceChangeHub, serviceChangeBus, log).ConfigureAwait(false);
        }
    }
}
