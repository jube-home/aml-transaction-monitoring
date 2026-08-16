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
    using StackExchange.Redis;

    public interface IHybridResilientRedisDatabase
    {
        public bool KeyExists(RedisKey key, CommandFlags flags = CommandFlags.None);
        Task<bool> KeyExistsAsync(RedisKey key, CommandFlags flags = CommandFlags.None);
        bool KeyRename(RedisKey key, RedisKey newKey, When when = When.Always, CommandFlags flags = CommandFlags.None);
        Task<bool> HashSetAsync(RedisKey key, RedisValue field, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None);
        void HashSet(RedisKey key, RedisValue field, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None);
        void HashSet(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None);
        Task<RedisValue> HashGetAsync(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None);
        Task<RedisValue[]> HashGetAsync(RedisKey key, RedisValue[] fields, CommandFlags flags = CommandFlags.None);
        Task<bool> HashDeleteAsync(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None);
        bool HashDelete(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None);
        Task<long> HashDeleteAsync(RedisKey key, RedisValue[] fields, CommandFlags flags = CommandFlags.None);
        Task<long> HashIncrementAsync(RedisKey key, RedisValue field, long value, CommandFlags flags = CommandFlags.None);
        Task<double> HashIncrementAsync(RedisKey key, RedisValue field, double value, CommandFlags flags = CommandFlags.None);
        Task<long> HashDecrementAsync(RedisKey key, RedisValue field, long value, CommandFlags flags = CommandFlags.None);
        Task<double> HashDecrementAsync(RedisKey key, RedisValue field, double value, CommandFlags flags = CommandFlags.None);
        Task<long> HashStringLengthAsync(RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None);
        IAsyncEnumerable<HashEntry> HashScanAsync(RedisKey key, RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None);
        IEnumerable<HashEntry> HashScan(RedisKey key, RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None);
        Task<bool> SetAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None);
        Task<bool> SetContainsAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None);
        Task<bool> SetRemoveAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None);
        Task<long> SetRemoveAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None);
        Task<RedisValue[]> SetMembersAsync(RedisKey key, CommandFlags flags = CommandFlags.None);
        Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score, CommandFlags flags = CommandFlags.None);
        public Task<long> SortedSetLengthAsync(RedisKey key, double min = Double.NegativeInfinity,
            double max = Double.PositiveInfinity, Exclude exclude = Exclude.None,
            CommandFlags flags = CommandFlags.None);
        Task<long> SortedSetRemoveRangeByScoreAsync(RedisKey key, double start = Double.NegativeInfinity, double stop = Double.PositiveInfinity,
            Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None);
        Task<bool> SortedSetUpdateAsync(RedisKey key, RedisValue member, double score, SortedSetWhen when = SortedSetWhen.Always, CommandFlags flags = CommandFlags.None);
        Task<bool> SortedSetRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None);
        Task<long> SortedSetRemoveAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None);
        Task<SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None);
        Task<SortedSetEntry[]> SortedSetRangeByScoreWithScoresAsync(RedisKey key, double start = Double.NegativeInfinity, double stop = Double.PositiveInfinity, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None);
        Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, CommandFlags flags = CommandFlags.None);
        bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry = null, CommandFlags flags = CommandFlags.None);
        Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = CommandFlags.None);
        Task<long> PublishAsync(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None);
    }
}
