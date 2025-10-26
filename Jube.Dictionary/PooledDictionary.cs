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
    using System;
    using System.Buffers;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    [Serializable]
    public class PooledDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    {
        private const float LoadFactor = 0.75f;
        private readonly bool fixedInitialSize;
        private readonly ArrayPool<int> intPool = ArrayPool<int>.Shared;
        private readonly ArrayPool<TKey> keyPool = ArrayPool<TKey>.Shared;
        private readonly ArrayPool<TValue> valuePool = ArrayPool<TValue>.Shared;

        private int capacity;
        private int[] hashes;
        private TKey[] keys;
        private TValue[] values;

        public PooledDictionary(int? initialCapacity = null)
        {
            if (initialCapacity.HasValue)
            {
                capacity = initialCapacity.Value;
                fixedInitialSize = true;
            }
            else
            {
                capacity = 16;
                fixedInitialSize = false;
            }

            hashes = intPool.Rent(capacity);
            keys = keyPool.Rent(capacity);
            values = valuePool.Rent(capacity);
        }

        public int Count { get; private set; }
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return keys.Where((_, i) => hashes[i] != 0).ToArray();
            }
        }
        public ICollection<TValue> Values
        {
            get
            {
                return values.Where((_, i) => hashes[i] != 0).ToArray();
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                {
                    return value;
                }
                throw new KeyNotFoundException($"Key '{key}' not found.");
            }
            set
            {
                var index = FindIndex(key);
                if (index >= 0)
                {
                    values[index] = value; // update existing
                }
                else
                {
                    Add(key, value); // add new
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            EnsureCapacity();

            var hash = key.GetHashCode() & 0x7FFFFFFF;
            var index = hash % capacity;

            for (var i = 0; i < capacity; i++)
            {
                var probeIndex = (index + i) % capacity;

                if (hashes[probeIndex] == 0)
                {
                    hashes[probeIndex] = hash;
                    keys[probeIndex] = key;
                    values[probeIndex] = value;
                    Count++;
                    return;
                }

                if (hashes[probeIndex] == hash && Equals(keys[probeIndex], key))
                {
                    throw new ArgumentException("Key already exists");
                }
            }

            throw new InvalidOperationException("Dictionary is full after resize attempt.");
        }

        public bool TryAdd(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            EnsureCapacity();

            var hash = key.GetHashCode() & 0x7FFFFFFF;
            var index = hash % capacity;

            for (var i = 0; i < capacity; i++)
            {
                var probeIndex = (index + i) % capacity;

                if (hashes[probeIndex] == 0)
                {
                    hashes[probeIndex] = hash;
                    keys[probeIndex] = key;
                    values[probeIndex] = value;
                    Count++;
                    return true;
                }

                if (hashes[probeIndex] == hash && Equals(keys[probeIndex], key))
                {
                    return false; // key exists
                }
            }

            throw new InvalidOperationException("Dictionary is full after resize attempt.");
        }

        public bool ContainsKey(TKey key) => FindIndex(key) >= 0;

        public bool TryGetValue(TKey key, out TValue value)
        {
            var index = FindIndex(key);
            if (index >= 0)
            {
                value = values[index];
                return true;
            }

            value = default!;
            return false;
        }

        public bool Remove(TKey key)
        {
            var index = FindIndex(key);
            if (index < 0)
            {
                return false;
            }

            hashes[index] = 0;
            keys[index] = default!;
            values[index] = default!;
            Count--;
            return true;
        }

        public void Clear()
        {
            for (var i = 0; i < capacity; i++)
            {
                hashes[i] = 0;
                keys[i] = default!;
                values[i] = default!;
            }
            Count = 0;
        }

        // ICollection<KeyValuePair<TKey,TValue>> members
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            var index = FindIndex(item.Key);
            return index >= 0 && Equals(values[index], item.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            var index = FindIndex(item.Key);
            if (index < 0 || !Equals(values[index], item.Value))
            {
                return false;
            }
            hashes[index] = 0;
            keys[index] = default!;
            values[index] = default!;
            Count--;
            return true;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var kv in this)
            {
                array[arrayIndex++] = kv;
            }
        }

        // IEnumerable<KeyValuePair<TKey,TValue>>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            for (var i = 0; i < capacity; i++)
            {
                if (hashes[i] != 0)
                {
                    yield return new KeyValuePair<TKey, TValue>(keys[i], values[i]);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private int FindIndex(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var hash = key.GetHashCode() & 0x7FFFFFFF;
            var index = hash % capacity;

            for (var i = 0; i < capacity; i++)
            {
                var probeIndex = (index + i) % capacity;
                if (hashes[probeIndex] == 0)
                {
                    return -1;
                }
                if (hashes[probeIndex] == hash && Equals(keys[probeIndex], key))
                {
                    return probeIndex;
                }
            }

            return -1;
        }

        private void EnsureCapacity()
        {
            if (Count < capacity * LoadFactor)
            {
                return;
            }

            if (fixedInitialSize)
            {
                Resize(capacity + 1);
            }
            else
            {
                Resize(capacity * 2);
            }
        }

        private void Resize(int newCapacity)
        {
            int[] newHashes = null!;
            TKey[] newKeys = null!;
            TValue[] newValues = null!;

            try
            {
                newHashes = intPool.Rent(newCapacity);
                newKeys = keyPool.Rent(newCapacity);
                newValues = valuePool.Rent(newCapacity);

                for (var i = 0; i < capacity; i++)
                {
                    if (hashes[i] == 0)
                    {
                        continue;
                    }
                    
                    var hash = hashes[i];
                    var index = hash % newCapacity;
                    while (newHashes[index] != 0)
                    {
                        index = (index + 1) % newCapacity;
                    }
                    newHashes[index] = hash;
                    newKeys[index] = keys[i];
                    newValues[index] = values[i];
                }

                intPool.Return(hashes, true);
                keyPool.Return(keys, true);
                valuePool.Return(values, true);

                hashes = newHashes;
                keys = newKeys;
                values = newValues;
                capacity = newCapacity;
            }
            catch
            {
                intPool.Return(newHashes, true);
                keyPool.Return(newKeys, true);
                valuePool.Return(newValues, true);
                throw;
            }
        }

        // IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PooledDictionary() => Dispose(false);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            
            intPool.Return(hashes, true);
            keyPool.Return(keys, true);
            valuePool.Return(values, true);
        }
    }
}