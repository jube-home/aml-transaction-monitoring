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

namespace Jube.Cache.Redis.Models
{
    using Dictionary;

    public class LocalCacheInstanceKey(LruCacheConcurrentSizedDictionary<string, byte[]> lruCacheConcurrentSizedDictionary)
    {
        public readonly LruCacheConcurrentSizedDictionary<string, byte[]> LruCacheConcurrentSizedDictionary = lruCacheConcurrentSizedDictionary;
        public int DualMiss = 0;
        public int HashRemove = 0;
        public int HashRemoveMiss = 0;
        public int HashRemoveSubscription = 0;
        public int HashRemoveSubscriptionMiss = 0;
        public int HashSetSubscriptions = 0;
        public int Misses = 0;
        public long MissRemoteResponseTime = 0;
        public long UnpackResponseTime = 0;

        public int Requests = 0;
    }
}
