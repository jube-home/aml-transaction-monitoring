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

    public class ResilientRedisDatabase(IDatabase inner, IAsyncPolicy idempotentPolicy, IAsyncPolicy nonIdempotentPolicy)
    {
        private readonly IAsyncPolicy idempotentPolicy = idempotentPolicy ?? throw new ArgumentNullException(nameof(idempotentPolicy));

        private IDatabase UnderlyingDatabase
        {
            get;
        } = inner ?? throw new ArgumentNullException(nameof(inner));

        public Task<bool> KeyExistsAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.KeyExistsAsync(key, flags),
                new Context());
        }

        private Task<bool> KeyRenameAsync(RedisKey key, RedisKey newKey, When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.KeyRenameAsync(key, newKey, when, flags),
                new Context());
        }

        public bool KeyRename(RedisKey key, RedisKey newKey, When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            return KeyRenameAsync(key, newKey, when, flags).GetAwaiter().GetResult();
        }

        public Task<bool> HashSetAsync(RedisKey key, RedisValue field, RedisValue value,
            When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashSetAsync(key, field, value, when, flags),
                new Context());
        }

        private Task HashSetAsync(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashSetAsync(key, hashFields, flags),
                new Context());
        }

        public void HashSet(RedisKey key, RedisValue field, RedisValue value,
            When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            HashSetAsync(key, field, value, when, flags).GetAwaiter().GetResult();
        }

        public void HashSet(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None)
        {
            HashSetAsync(key, hashFields, flags).GetAwaiter().GetResult();
        }

        public Task<RedisValue> HashGetAsync(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashGetAsync(key, field, flags),
                new Context());
        }

        public Task<RedisValue[]> HashGetAsync(RedisKey key, RedisValue[] fields, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashGetAsync(key, fields, flags),
                new Context());
        }

        public Task<bool> HashDeleteAsync(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashDeleteAsync(key, field, flags),
                new Context());
        }

        public Task<long> HashDeleteAsync(RedisKey key, RedisValue[] fields, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashDeleteAsync(key, fields, flags),
                new Context());
        }

        public bool HashDelete(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None)
        {
            return HashDeleteAsync(key, field, flags).GetAwaiter().GetResult();
        }

        public Task<long> HashIncrementAsync(RedisKey key, RedisValue field, long value,
            CommandFlags flags = CommandFlags.None)
        {
            return nonIdempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashIncrementAsync(key, field, value, flags),
                new Context());
        }

        public Task<double> HashIncrementAsync(RedisKey key, RedisValue field, double value,
            CommandFlags flags = CommandFlags.None)
        {
            return nonIdempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashIncrementAsync(key, field, value, flags),
                new Context());
        }

        public Task<long> HashDecrementAsync(RedisKey key, RedisValue field, long value,
            CommandFlags flags = CommandFlags.None)
        {
            return nonIdempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashDecrementAsync(key, field, value, flags),
                new Context());
        }

        public Task<double> HashDecrementAsync(RedisKey key, RedisValue field, double value,
            CommandFlags flags = CommandFlags.None)
        {
            return nonIdempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashDecrementAsync(key, field, value, flags),
                new Context());
        }

        public Task<long> HashStringLengthAsync(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.HashStringLengthAsync(key, field, flags),
                new Context());
        }

        public IAsyncEnumerable<HashEntry> HashScanAsync(RedisKey key, RedisValue pattern = default,
            int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None)
        {
            return UnderlyingDatabase.HashScanAsync(key, pattern, pageSize, cursor, pageOffset, flags);
        }

        public IEnumerable<HashEntry> HashScan(RedisKey key, RedisValue pattern = default,
            int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None)
        {
            return UnderlyingDatabase.HashScan(key, pattern, pageSize, cursor, pageOffset, flags);
        }

        public Task<bool> SetAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SetAddAsync(key, value, flags),
                new Context());
        }

        public Task<bool> SetRemoveAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SetRemoveAsync(key, value, flags),
                new Context());
        }

        public Task<RedisValue[]> SetMembersAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SetMembersAsync(key, flags),
                new Context());
        }

        public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score,
            CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetAddAsync(key, member, score, flags),
                new Context());
        }

        public Task<bool> SortedSetUpdateAsync(RedisKey key, RedisValue member, double score,
            SortedSetWhen when = SortedSetWhen.Always, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetUpdateAsync(key, member, score, when, flags),
                new Context());
        }

        public Task<bool> SortedSetRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetRemoveAsync(key, member, flags),
                new Context());
        }

        public Task<long> SortedSetRemoveAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetRemoveAsync(key, members, flags),
                new Context());
        }

        public Task<SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(RedisKey key,
            long start = 0, long stop = -1, Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetRangeByRankWithScoresAsync(key, start, stop, order, flags),
                new Context());
        }

        public Task<SortedSetEntry[]> SortedSetRangeByScoreWithScoresAsync(RedisKey key,
            double start = Double.NegativeInfinity, double stop = Double.PositiveInfinity,
            Exclude exclude = Exclude.None, Order order = Order.Ascending,
            long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
        {
            return idempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.SortedSetRangeByScoreWithScoresAsync(key, start, stop, exclude, order, skip, take, flags),
                new Context());
        }

        public Task<long> PublishAsync(RedisChannel channel, RedisValue message,
            CommandFlags flags = CommandFlags.None)
        {
            return nonIdempotentPolicy.ExecuteAsync(_ =>
                    UnderlyingDatabase.PublishAsync(channel, message, flags),
                new Context());
        }
    }
}
