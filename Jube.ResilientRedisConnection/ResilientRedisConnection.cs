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

    public class ResilientRedisConnection : IDisposable
    {
        private readonly IConnectionMultiplexer multiplexer;

        public ResilientRedisConnection(IConnectionMultiplexer multiplexer, ILog log, int maxRetries = 10)
        {
            this.multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));

            FailoverPolicy = Policy
                .Handle<RedisConnectionException>()
                .Or<RedisTimeoutException>()
                .Or<RedisServerException>(ex =>
                    ex.Message.StartsWith("LOADING", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.StartsWith("MASTERDOWN", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.StartsWith("CLUSTERDOWN", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.StartsWith("TRYAGAIN", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.StartsWith("MOVED", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.StartsWith("ASK", StringComparison.OrdinalIgnoreCase))
                .Or<RedisException>(ex =>
                    ex.Message.Contains("No connection is available", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("multiplexer is not connected", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("The message was already in a completed state", StringComparison.OrdinalIgnoreCase))
                .WaitAndRetryAsync(
                    maxRetries,
                    attempt =>
                        TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 10)) +
                        TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500)),
                    (ex, delay, retryCount, _) =>
                    {
                        log.Warn($"Redis failover: attempt {retryCount} of {maxRetries}, " +
                                 $"waiting {delay.TotalSeconds:F1}s. {ex.Message}");
                    });
        }
        private IAsyncPolicy FailoverPolicy { get; }

        public void Dispose()
        {
            multiplexer.Dispose();
        }

        public ResilientRedisDatabase GetDatabase(int db = -1)
        {
            return new ResilientRedisDatabase(multiplexer.GetDatabase(db), FailoverPolicy);
        }
    }
}
