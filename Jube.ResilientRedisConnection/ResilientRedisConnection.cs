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

namespace Jube.ResilientRedisConnection
{
    using log4net;
    using Polly;
    using StackExchange.Redis;

    public class ResilientRedisConnection(IConnectionMultiplexer multiplexer, string connectionString, bool hsetOffload, ILog log)
        : IDisposable
    {

        // NoOpAsync: satisfies ResilientRedisDatabase's IAsyncPolicy dependency without the
        // circuit breaker / retry machinery. ExecuteAsync just invokes the delegate directly -
        // no shared circuit state, no lock, no WrapAsync nesting, no extra Task layer per call.
        // Connection-level resilience (reconnect, failover) is left to SE.Redis's multiplexer,
        // which already handles it via ConnectRetry / ReconnectRetryPolicy / AbortOnConnectFail.
        // the wrapper has been kept for future use and logging opportunities.
        private static readonly IAsyncPolicy NoOpPolicy = Policy.NoOpAsync();
        private readonly string connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        private readonly IConnectionMultiplexer multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));

        public void Dispose()
        {
            multiplexer.Dispose();
        }

        public IHybridResilientRedisDatabase GetDatabase(int db = -1)
        {
            return new ResilientRedisDatabase(multiplexer.GetDatabase(db), NoOpPolicy, NoOpPolicy, connectionString, log, hsetOffload);
        }
    }
}
