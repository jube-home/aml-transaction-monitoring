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

namespace Jube.Cache.Redis.CacheUserRegistryApiKey
{
    using System.Net;
    using Events;
    using log4net;
    using ResilientRedisConnection;
    using StackExchange.Redis;

    public class CacheUserRegistryApiKeyRepository
    {
        private readonly ConnectionMultiplexer connectionMultiplexer;
        private readonly ILog log;

        private readonly ResilientRedisDatabase redisDatabase;

        public CacheUserRegistryApiKeyRepository(ConnectionMultiplexer connectionMultiplexer, ResilientRedisDatabase redisDatabase,
            ILog log)
        {
            this.connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
            this.redisDatabase = redisDatabase;
            this.log = log;

            SubscribeToRedisHashEvents();
        }
        public event EventHandler<CaseUserRegistryKeyEventArguments> OnCaseUserRegistryApiKeySetEvent;
        public event EventHandler<CaseUserRegistryKeyEventArguments> OnCaseUserRegistryApiKeyRemoveEvent;
        public async Task PublishSetAsync(string apiKeyHash)
        {
            try
            {
                await redisDatabase.PublishAsync(
                    RedisChannel.Pattern($"UserRegistryApiKeySet:{Dns.GetHostName()}")
                    , new RedisValue(apiKeyHash));
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }

        public async Task PublishRemoveAsync(string apiKeyHash)
        {
            try
            {
                await redisDatabase.PublishAsync(
                    RedisChannel.Pattern($"UserRegistryApiKeyRemove:{Dns.GetHostName()}")
                    , new RedisValue(apiKeyHash));
            }
            catch (Exception ex)
            {
                log.Error($"Cache Redis: Has created an exception as {ex}.");
            }
        }

        private void SubscribeToRedisHashEvents()
        {
            {
                SubscribeToSet();
                SubscribeToRemove();
            }
            return;

            void SubscribeToSet()
            {
                var subscriber = connectionMultiplexer.GetSubscriber();
                subscriber.Subscribe(RedisChannel.Pattern("UserRegistryApiKeySet:*"), (_, value) =>
                {
                    OnCaseUserRegistryApiKeySetEvent?.Invoke(this, new CaseUserRegistryKeyEventArguments
                    {
                        ApiKeyHash = value
                    });
                });
            }

            void SubscribeToRemove()
            {
                var subscriber = connectionMultiplexer.GetSubscriber();
                subscriber.Subscribe(RedisChannel.Pattern("UserRegistryApiKeyRemove:*"), (_, value) =>
                {
                    OnCaseUserRegistryApiKeyRemoveEvent?.Invoke(this, new CaseUserRegistryKeyEventArguments
                    {
                        ApiKeyHash = value
                    });
                });
            }
        }
    }
}
