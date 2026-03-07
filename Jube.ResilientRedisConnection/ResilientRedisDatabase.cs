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
    using Polly;
    using StackExchange.Redis;

    public class ResilientRedisDatabase(IDatabase inner, IAsyncPolicy policy)
    {
        private readonly IAsyncPolicy policy = policy ?? throw new ArgumentNullException(nameof(policy));

        public IDatabase UnderlyingDatabase
        {
            get;
        } = inner ?? throw new ArgumentNullException(nameof(inner));

        public Task<bool> StringSetAsync(RedisKey key, RedisValue value,
            TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.StringSetAsync(key, value, expiry, when, flags),
                new Context());
        }

        public Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.StringGetAsync(key, flags),
                new Context());
        }

        public Task<bool> StringSetAsync(KeyValuePair<RedisKey, RedisValue>[] values,
            When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.StringSetAsync(values, when, flags),
                new Context());
        }

        public Task<RedisValue[]> StringGetAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.StringGetAsync(keys, flags),
                new Context());
        }

        public Task<bool> KeyDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.KeyDeleteAsync(key, flags),
                new Context());
        }

        public Task<bool> KeyExistsAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.KeyExistsAsync(key, flags),
                new Context());
        }

        public Task<bool> KeyExpireAsync(RedisKey key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.KeyExpireAsync(key, expiry, flags),
                new Context());
        }

        public Task<bool> HashSetAsync(RedisKey key, RedisValue field, RedisValue value,
            When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashSetAsync(key, field, value, when, flags),
                new Context());
        }

        public Task HashSetAsync(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashSetAsync(key, hashFields, flags),
                new Context());
        }

        public Task<RedisValue> HashGetAsync(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashGetAsync(key, field, flags),
                new Context());
        }

        public Task<HashEntry[]> HashGetAllAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashGetAllAsync(key, flags),
                new Context());
        }

        public Task<bool> HashDeleteAsync(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashDeleteAsync(key, field, flags),
                new Context());
        }

        public Task<long> ListRightPushAsync(RedisKey key, RedisValue value, When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.ListRightPushAsync(key, value, when, flags),
                new Context());
        }

        public Task<RedisValue> ListLeftPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.ListLeftPopAsync(key, flags),
                new Context());
        }

        public Task<long> ListLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.ListLengthAsync(key, flags),
                new Context());
        }

        public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score,
            CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetAddAsync(key, member, score, flags),
                new Context());
        }

        public Task<SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(RedisKey key,
            long start = 0, long stop = -1, Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetRangeByRankWithScoresAsync(key, start, stop, order, flags),
                new Context());
        }

        public ITransaction CreateTransaction(object? asyncState = null)
        {
            return UnderlyingDatabase.CreateTransaction(asyncState);
        }

        public Task<RedisResult> ScriptEvaluateAsync(string script, RedisKey[]? keys = null,
            RedisValue[]? values = null, CommandFlags flags = CommandFlags.None)
        {
            return policy.ExecuteAsync(_ =>
                    UnderlyingDatabase.ScriptEvaluateAsync(script, keys, values, flags),
                new Context());
        }

        public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry = null,
            When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return StringSetAsync(key, value, expiry, when, flags).GetAwaiter().GetResult();
        }

        public RedisValue StringGet(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return StringGetAsync(key, flags).GetAwaiter().GetResult();
        }
    }
}
