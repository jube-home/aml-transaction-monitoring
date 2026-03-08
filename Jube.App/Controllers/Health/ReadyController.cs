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

namespace Jube.App.Controllers.Health
{
    using System.Threading.Tasks;
    using Code.WatcherDispatch;
    using DynamicEnvironment;
    using Engine;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Hosting;

    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ReadyController : Controller
    {
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly Engine engine;
        private readonly Relay relay;
        private readonly IHostApplicationLifetime lifetime;

        public ReadyController(IHostApplicationLifetime lifetime,
            DynamicEnvironment dynamicEnvironment, Engine engine = null, Relay relay = null)
        {
            this.engine = engine;
            this.relay = relay;
            this.dynamicEnvironment = dynamicEnvironment;
            this.lifetime = lifetime;
        }

        [HttpGet]
        public Task<ActionResult> GetAsync()
        {
            if (lifetime.ApplicationStopping.IsCancellationRequested)
            {
                return Task.FromResult<ActionResult>(StatusCode(503));
            }

            if (dynamicEnvironment.AppSettings("EnableEngine").Equals("True"))
            {
                if (engine.Context is not { Ready: true })
                {
                    return Task.FromResult<ActionResult>(StatusCode(503));
                }
            }

            if (!dynamicEnvironment.AppSettings("StreamingActivationWatcher").Equals("True"))
            {
                return Task.FromResult<ActionResult>(Ok());
            }

            return Task.FromResult<ActionResult>(relay is not { Ready: true } ? StatusCode(503) : Ok());
        }
    }
}
