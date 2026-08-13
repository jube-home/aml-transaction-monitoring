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

namespace Jube.Cache
{
    using System.Collections.Concurrent;
    using log4net;
    using Redis;
    using Redis.CacheUserRegistryApiKey;
    using Redis.Callback;
    using ResilientRedisConnection;
    using StackExchange.Redis;
    using TaskCancellation;

    public class CacheService
    {
        private readonly bool activationRuleIdempotency;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Callback>> callbacks;
        private readonly int callbackTimeout;
        private readonly bool localCache;
        private readonly long localCacheBytes;
        private readonly bool localCacheFill;
        private readonly TimeSpan maxLruAge;
        private readonly bool messagePackCompression;
        private readonly string postgresConnectionString;
        private readonly bool publishSubscribe;
        private readonly bool storePayloadCountsAndBytes;

        public CacheService(string redisConnectionString,
            string postgresConnectionString,
            ConcurrentDictionary<Guid, TaskCompletionSource<Callback>> callbacks, int callbackTimeout,
            bool localCache, bool localCacheFill, long localCacheBytes,
            bool messagePackCompression, bool storePayloadCountsAndBytes, bool publishSubscribe,
            bool hsetOffload,
            TimeSpan maxLruAge,
            bool activationRuleIdempotency, ILog log)
        {
            Log = log;
            this.localCache = localCache;
            this.localCacheFill = localCacheFill;
            this.localCacheBytes = localCacheBytes;
            this.messagePackCompression = messagePackCompression;
            this.storePayloadCountsAndBytes = storePayloadCountsAndBytes;
            this.publishSubscribe = publishSubscribe;
            this.callbacks = callbacks;
            this.callbackTimeout = callbackTimeout;
            this.postgresConnectionString = postgresConnectionString;
            this.maxLruAge = maxLruAge;
            this.activationRuleIdempotency = activationRuleIdempotency;

            var options = ConfigurationOptions.Parse(redisConnectionString);
            options.ReconnectRetryPolicy = new ExponentialRetry(
                100,
                3000);

            ConnectionMultiplexer = ConnectionMultiplexer.Connect(options);
            ResilientRedisResilientRedisDatabase = new ResilientRedisConnection(ConnectionMultiplexer, postgresConnectionString, hsetOffload, log).GetDatabase();

            CacheAbstractionRepository = new CacheAbstractionRepository(ResilientRedisResilientRedisDatabase, Log);
            CachePayloadLatestRepository = new CachePayloadLatestRepository(postgresConnectionString, ResilientRedisResilientRedisDatabase, Log);
            CacheReferenceDateRepository = new CacheReferenceDateRepository(ResilientRedisResilientRedisDatabase, Log);
            CacheSanctionRepository = new CacheSanctionRepository(ResilientRedisResilientRedisDatabase, Log);
            CacheTtlCounterEntryRepository = new CacheTtlCounterEntryRepository(ResilientRedisResilientRedisDatabase, Log);
            CacheTtlCounterRepository = new CacheTtlCounterRepository(ResilientRedisResilientRedisDatabase, Log);
            CacheWalRepository = new CacheWalRepository(ResilientRedisResilientRedisDatabase, Log);
            CacheUserRegistryApiKeyRepository = new CacheUserRegistryApiKeyRepository(ConnectionMultiplexer, ResilientRedisResilientRedisDatabase, Log);
            CacheTtlCounterIdempotencyRepository = new CacheTtlCounterIdempotencyRepository(ResilientRedisResilientRedisDatabase, log);
            CacheActivationCaseIdempotencyRepository = new CacheActivationCaseIdempotencyRepository(ResilientRedisResilientRedisDatabase, log);
            CacheActivationNotificationIdempotencyRepository = new CacheActivationNotificationIdempotencyRepository(ResilientRedisResilientRedisDatabase, log);
        }

        public Task InstantiateRepositoriesTask { get; set; }
        public ConnectionMultiplexer ConnectionMultiplexer { get; set; }
        public IHybridResilientRedisDatabase ResilientRedisResilientRedisDatabase { get; set; }
        public CacheAbstractionRepository CacheAbstractionRepository { get; set; }
        public CachePayloadLatestRepository CachePayloadLatestRepository { get; set; }
        public CachePayloadRepository CachePayloadRepository { get; set; }
        public CacheReferenceDateRepository CacheReferenceDateRepository { get; set; }
        public CacheSanctionRepository CacheSanctionRepository { get; set; }
        public CacheTtlCounterEntryRepository CacheTtlCounterEntryRepository { get; set; }
        public CacheTtlCounterRepository CacheTtlCounterRepository { get; set; }
        public CacheCallbackPublishSubscribe CacheCallbackPublishSubscribe { get; set; }
        public CacheWalRepository CacheWalRepository { get; set; }
        public CacheUserRegistryApiKeyRepository CacheUserRegistryApiKeyRepository { get; set; }
        public CacheTtlCounterIdempotencyRepository CacheTtlCounterIdempotencyRepository { get; set; }
        public CacheActivationCaseIdempotencyRepository CacheActivationCaseIdempotencyRepository { get; set; }
        public CacheActivationNotificationIdempotencyRepository CacheActivationNotificationIdempotencyRepository { get; set; }

        public bool Ready { get; private set; }

        private ILog Log
        {
            get;
        }

        public async Task StartAsync(TaskCoordinator taskCoordinator)
        {
            CachePayloadRepository = await CachePayloadRepository.CreateAsync(ConnectionMultiplexer, ResilientRedisResilientRedisDatabase,
                postgresConnectionString, Log,
                localCache, localCacheFill, localCacheBytes, messagePackCompression,
                storePayloadCountsAndBytes, publishSubscribe, maxLruAge, activationRuleIdempotency,
                taskCoordinator.CancellationToken).ConfigureAwait(false);

            CacheCallbackPublishSubscribe = new CacheCallbackPublishSubscribe(ConnectionMultiplexer, ResilientRedisResilientRedisDatabase, callbacks, callbackTimeout, Log, taskCoordinator);

            Ready = true;
        }
    }
}
