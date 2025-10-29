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
    using System.Net;
    using Data.Context;
    using Extensions;
    using log4net;
    using Redis;
    using StackExchange.Redis;

    public class CacheService
    {
        private readonly bool localCache;
        private readonly long localCacheBytes;
        private readonly bool localCacheFill;
        private readonly bool messagePackCompression;
        private readonly bool storePayloadCountsAndBytes;
        private readonly bool publishSubscribe;

        public CacheService(string redisConnectionString, string postgresConnectionString, bool localCache, bool localCacheFill, long localCacheBytes,
            bool messagePackCompression, bool storePayloadCountsAndBytes,bool publishSubscribe, ILog log)
        {
            Log = log;
            this.localCache = localCache;
            this.localCacheFill = localCacheFill;
            this.localCacheBytes = localCacheBytes;
            this.messagePackCompression = messagePackCompression;
            this.storePayloadCountsAndBytes = storePayloadCountsAndBytes;
            this.publishSubscribe = publishSubscribe;

            ConnectionMultiplexer =
                ConnectionMultiplexer.Connect(redisConnectionString);

            RedisDatabase = ConnectionMultiplexer.GetDatabase();

            DbContext =
                DataConnectionDbContext.GetDbContextDataConnection(postgresConnectionString);
        }
        private DbContext DbContext
        {
            get;
        }

        public ConnectionMultiplexer ConnectionMultiplexer { get; set; }
        public IDatabase RedisDatabase { get; set; }
        public CacheAbstractionRepository CacheAbstractionRepository { get; set; }
        public CachePayloadLatestRepository CachePayloadLatestRepository { get; set; }
        public CachePayloadRepository CachePayloadRepository { get; set; }
        public CacheReferenceDate CacheReferenceDate { get; set; }
        public CacheSanctionRepository CacheSanctionRepository { get; set; }
        public CacheTtlCounterEntryRepository CacheTtlCounterEntryRepository { get; set; }
        public CacheTtlCounterRepository CacheTtlCounterRepository { get; set; }

        private ILog Log
        {
            get;
        }

        public async Task InstantiateRepositories()
        {
            CacheAbstractionRepository = new CacheAbstractionRepository(RedisDatabase, Log);
            CachePayloadLatestRepository = new CachePayloadLatestRepository(RedisDatabase, Log);

            CachePayloadRepository = await CachePayloadRepository.CreateAsync(ConnectionMultiplexer, RedisDatabase,
                DbContext, Log, CommandFlags.FireAndForget,
                localCache, localCacheFill, localCacheBytes, messagePackCompression, storePayloadCountsAndBytes,publishSubscribe);

            CacheReferenceDate = new CacheReferenceDate(RedisDatabase, Log);
            CacheSanctionRepository = new CacheSanctionRepository(RedisDatabase, Log);
            CacheTtlCounterEntryRepository = new CacheTtlCounterEntryRepository(RedisDatabase, Log);
            CacheTtlCounterRepository = new CacheTtlCounterRepository(RedisDatabase, Log);
        }
    }
}
