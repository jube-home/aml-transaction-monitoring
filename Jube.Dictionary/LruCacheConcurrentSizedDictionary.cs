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

namespace Jube.Dictionary
{
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Diagnostics.CodeAnalysis;
    using Interfaces;

    public class LruCacheConcurrentSizedDictionary<TKey, TValue> : ISized, IDictionary<TKey, TValue>, ILruCacheConcurrentSizedDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheEntry> dict = new ConcurrentDictionary<TKey, CacheEntry>();
        private readonly long evictionThreshold;
        private readonly ConcurrentQueue<TKey> lruQueue = new ConcurrentQueue<TKey>();
        private readonly long maxSizeBytes;
        private readonly Func<TValue, long> sizeEstimator;
        private long add;
        private long addBytes;
        private long evictionBytes;
        private long evictionCount;
        private volatile bool isEvicting;
        private long remove;
        private long removeBytes;
        private long request;
        private long requestBytes;
        private long totalSize;
        private long update;
        private long updateBytes;

        // ReSharper disable once ConvertToPrimaryConstructor
        public LruCacheConcurrentSizedDictionary(Func<TValue, long> sizeEstimator,
            long maxSizeBytes = 3L * 1024 * 1024 * 1024,
            double evictionThresholdRatio = 0.9)
        {
            this.maxSizeBytes = maxSizeBytes;
            evictionThreshold = (long)(maxSizeBytes * evictionThresholdRatio);
            this.sizeEstimator = sizeEstimator ?? throw new ArgumentNullException(nameof(sizeEstimator));
        }

        public long EvictionBytes
        {
            get
            {
                return Interlocked.Read(ref evictionBytes);
            }
        }

        public long EvictionCount
        {
            get
            {
                return Interlocked.Read(ref evictionCount);
            }
        }

        public long RequestBytes
        {
            get
            {
                return Interlocked.Read(ref requestBytes);
            }
        }

        public long RequestCount
        {
            get
            {
                return Interlocked.Read(ref request);
            }
        }

        public long UpdateCount
        {
            get
            {
                return Interlocked.Read(ref update);
            }
        }

        public long UpdateBytes
        {
            get
            {
                return Interlocked.Read(ref updateBytes);
            }
        }

        public long AddCounter
        {
            get
            {
                return Interlocked.Read(ref add);
            }
        }

        public long AddBytes
        {
            get
            {
                return Interlocked.Read(ref addBytes);
            }
        }

        public long RemoveCount
        {
            get
            {
                return Interlocked.Read(ref remove);
            }
        }

        public long RemoveBytes
        {
            get
            {
                return Interlocked.Read(ref removeBytes);
            }
        }

        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            AddOrUpdate(item.Key, item.Value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Clear()
        {
            dict.Clear();
            while (lruQueue.TryDequeue(out _)) {}
            Interlocked.Exchange(ref totalSize, 0);
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return dict.Keys.ToList();
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                return dict.Values.Select(e => e.Value).ToList();
            }
        }

        public int Count
        {
            get
            {
                return dict.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                if (!dict.TryGetValue(key, out var entry))
                {
                    throw new KeyNotFoundException();
                }
                UpdateLru(key);
                return entry.Value;
            }
            set
            {
                AddOrUpdate(key, value);
            }
        }

        public void Add(TKey key, TValue value)
        {
            AddOrUpdate(key, value);
        }

        public bool Remove(TKey key)
        {
            if (!dict.TryRemove(key, out var entry))
            {
                return false;
            }

            Interlocked.Add(ref totalSize, -entry.Size);
            Interlocked.Add(ref requestBytes, -entry.Size);
            Interlocked.Add(ref remove, -entry.Size);
            Interlocked.Add(ref removeBytes, 1);

            // ReSharper disable once RedundantAssignment
            entry = null;//This is important to make sure the GC identifies it.

            return true;
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (dict.TryGetValue(key, out var entry))
            {
                UpdateLru(key);
                value = entry.Value;
                return true;
            }
            value = default(TValue);
            return false;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dict.TryGetValue(item.Key, out var entry) && EqualityComparer<TValue>.Default.Equals(entry.Value, item.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var kvp in this)
            {
                array[arrayIndex++] = kvp;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Contains(item) && Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dict.Select(kvp => new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value.Value)).GetEnumerator();
        }

        public long TotalSize
        {
            get
            {
                return Interlocked.Read(ref totalSize);
            }
        }
        public long EstimatedSizeBytes()
        {
            return TotalSize;
        }
        private long EstimateSize(TValue value)
        {
            return sizeEstimator(value);
        }

        public void ResetCounters()
        {
            Interlocked.Exchange(ref evictionBytes, 0);
            Interlocked.Exchange(ref evictionCount, 0);
            Interlocked.Exchange(ref requestBytes, 0);
            Interlocked.Exchange(ref request, 0);
            Interlocked.Exchange(ref update, 0);
            Interlocked.Exchange(ref updateBytes, 0);
            Interlocked.Exchange(ref add, 0);
            Interlocked.Exchange(ref addBytes, 0);
            Interlocked.Exchange(ref remove, 0);
            Interlocked.Exchange(ref removeBytes, 0);
        }

        private void UpdateLru(TKey key)
        {
            lruQueue.Enqueue(key);
        }

        private void AddOrUpdate(TKey key, TValue value)
        {
            var newSize = EstimateSize(value);

            dict.AddOrUpdate(key,
                k =>
                {
                    lruQueue.Enqueue(k);
                    Interlocked.Add(ref totalSize, newSize);
                    Interlocked.Add(ref request, 1);
                    Interlocked.Add(ref requestBytes, newSize);
                    Interlocked.Add(ref add, 1);
                    Interlocked.Add(ref addBytes, newSize);
                    EvictIfNeeded();
                    return new CacheEntry(value, newSize);
                },
                (k, existing) =>
                {
                    var delta = newSize - existing.Size;
                    Interlocked.Add(ref totalSize, delta);
                    Interlocked.Add(ref request, 1);
                    Interlocked.Add(ref requestBytes, delta);
                    Interlocked.Add(ref update, 1);
                    Interlocked.Add(ref updateBytes, delta);
                    lruQueue.Enqueue(k);
                    EvictIfNeeded();
                    return new CacheEntry(value, newSize);
                });
        }

        private void EvictIfNeeded()
        {
            if (TotalSize <= maxSizeBytes || isEvicting)
            {
                return;
            }

            isEvicting = true;
            try
            {
                EvictItems(evictionThreshold);
            }
            finally
            {
                isEvicting = false;
            }
        }

        private void EvictItems(long targetSize)
        {
            while (TotalSize > targetSize && lruQueue.TryDequeue(out var key))
            {
                if (!dict.TryRemove(key, out var entry))
                {
                    continue;
                }
                Interlocked.Add(ref evictionCount, 1);
                Interlocked.Add(ref evictionBytes, entry.Size);
                Interlocked.Add(ref totalSize, -entry.Size);
            }
        }

        private class CacheEntry(TValue value, long size)
        {
            public TValue Value { get; } = value;
            public long Size { get; } = size;
        }
    }
}
