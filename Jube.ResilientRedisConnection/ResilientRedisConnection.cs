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

        public ResilientRedisConnection(IConnectionMultiplexer multiplexer, ILog log, int maxRetries = 5)
        {
            this.multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));

            var circuitBreaker = Policy
                .Handle<RedisConnectionException>()
                .Or<RedisTimeoutException>()
                .Or<RedisServerException>(ex =>
                    ex.Message.Contains("LOADING", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("MASTERDOWN", StringComparison.OrdinalIgnoreCase))
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

            var fullRetry = Policy
                .Handle<RedisConnectionException>()
                .Or<RedisTimeoutException>()
                .Or<RedisServerException>(ex =>
                    ex.Message.Contains("LOADING", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("MASTERDOWN", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("CLUSTERDOWN", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("TRYAGAIN", StringComparison.OrdinalIgnoreCase))
                .Or<RedisException>(ex =>
                    ex.Message.Contains("No connection is available", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("multiplexer is not connected", StringComparison.OrdinalIgnoreCase))
                .WaitAndRetryAsync(
                    maxRetries,
                    attempt => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 10)) +
                               TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500)),
                    (ex, _, count, _) =>
                    {
                        if (count == maxRetries)
                        {
                            log.Warn($"Redis Retry threshold hit: {count}/{maxRetries}. {ex.Message}");
                        }
                    });

            var connectionRetry = Policy
                .Handle<RedisConnectionException>()
                .Or<RedisServerException>(ex =>
                    ex.Message.Contains("READONLY", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("TRYAGAIN", StringComparison.OrdinalIgnoreCase))
                .Or<RedisException>(ex =>
                    ex.Message.Contains("No connection is available", StringComparison.OrdinalIgnoreCase))
                .WaitAndRetryAsync(
                    maxRetries,
                    attempt => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 10)) +
                               TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500)),
                    (ex, _, count, _) =>
                    {
                        if (count == maxRetries)
                        {
                            log.Warn($"Redis Retry threshold hit: {count}/{maxRetries}. {ex.Message}");
                        }
                    }
                );

            IdempotentPolicy = Policy.WrapAsync(circuitBreaker, fullRetry);
            NonIdempotentPolicy = Policy.WrapAsync(circuitBreaker, connectionRetry);
        }
        private IAsyncPolicy IdempotentPolicy { get; }
        private IAsyncPolicy NonIdempotentPolicy { get; }

        public void Dispose()
        {
            multiplexer.Dispose();
        }

        public ResilientRedisDatabase GetDatabase(int db = -1)
        {
            return new ResilientRedisDatabase(multiplexer.GetDatabase(db), IdempotentPolicy, NonIdempotentPolicy);
        }
    }
}
