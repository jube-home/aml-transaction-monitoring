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
    using System.Runtime.CompilerServices;
    using Interfaces;
    using Models;

    public sealed class DictionaryNoBoxing<TKey> : IDictionaryNoBoxing<TKey>, IEnumerable<KeyValuePair<TKey, InternalValue>>,
        IDisposable, ISized
        where TKey : notnull
    {
        private const int DefaultCapacity = 8;
        private static readonly EqualityComparer<TKey> KeyComparer = EqualityComparer<TKey>.Default;
        private long estimatedSizeBytes;
        private TKey[] keys;
        private InternalValue[] values;

        public DictionaryNoBoxing()
        {
            keys = new TKey[DefaultCapacity];
            values = new InternalValue[DefaultCapacity];
            Count = 0;
        }

        public DictionaryNoBoxing(int capacity = DefaultCapacity)
        {
            keys = new TKey[capacity];
            values = new InternalValue[capacity];
            Count = 0;
        }

        public InternalValue this[TKey key]
        {
            get
            {
                var idx = IndexOfKey(key);
                return idx >= 0 ? values[idx] : new InternalValue();
            }
            set
            {
                var idx = IndexOfKey(key);
                if (idx >= 0)
                {
                    estimatedSizeBytes -= EstimateValueSize(in values[idx]);
                    values[idx] = value;
                    estimatedSizeBytes += EstimateValueSize(in value);
                }
                else
                {
                    AddInternal(key, value);
                }
            }
        }

        public int Count { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, string? value)
        {
            return TryAddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, double value)
        {
            return TryAddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, bool value)
        {
            return TryAddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, DateTime value)
        {
            return TryAddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, int value)
        {
            return TryAddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(TKey key)
        {
            return IndexOfKey(key) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key)
        {
            var idx = IndexOfKey(key);
            if (idx < 0)
            {
                return false;
            }

            estimatedSizeBytes -= EstimateKeySize(keys[idx]) + EstimateValueSize(in values[idx]);

            Array.Copy(keys, idx + 1, keys, idx, Count - idx - 1);
            Array.Copy(values, idx + 1, values, idx, Count - idx - 1);

            keys[Count - 1] = default(TKey)!;
            Count--;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(TKey key, out InternalValue value)
        {
            var idx = IndexOfKey(key);
            if (idx >= 0)
            {
                value = values[idx];
                return true;
            }

            value = default(InternalValue);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Array.Clear(keys, 0, Count);
            Array.Clear(values, 0, Count);
            Count = 0;
            estimatedSizeBytes = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(keys, values, Count);
        }

        public void Dispose()
        {
            Array.Clear(keys, 0, Count);
            Array.Clear(values, 0, Count);
#pragma warning disable CS8625
            keys = null;
            values = null;
#pragma warning restore CS8625
            Count = 0;
            GC.SuppressFinalize(this);
        }

        IEnumerator<KeyValuePair<TKey, InternalValue>> IEnumerable<KeyValuePair<TKey, InternalValue>>.GetEnumerator()
        {
            return GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long EstimatedSizeBytes()
        {
            return estimatedSizeBytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddUnchecked(TKey key, InternalValue value)
        {
            if (Count >= keys.Length)
            {
                Resize(keys.Length * 2);
            }

            keys[Count] = key;
            values[Count] = value;
            Count++;

            estimatedSizeBytes += EstimateKeySize(key) + EstimateValueSize(in value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddUnchecked(TKey key, string? value)
        {
            AddUnchecked(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddUnchecked(TKey key, double value)
        {
            AddUnchecked(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddUnchecked(TKey key, bool value)
        {
            AddUnchecked(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddUnchecked(TKey key, DateTime value)
        {
            AddUnchecked(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddUnchecked(TKey key, int value)
        {
            AddUnchecked(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, Guid value)
        {
            return TryAddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TKey key, InternalValue value)
        {
            AddInternal(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TKey key, string? value)
        {
            AddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TKey key, double value)
        {
            AddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TKey key, bool value)
        {
            AddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TKey key, DateTime value)
        {
            AddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TKey key, int value)
        {
            AddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IndexOfKey(TKey key)
        {
            for (var i = 0; i < Count; i++)
            {
                if (KeyComparer.Equals(keys[i], key))
                {
                    return i;
                }
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize(int newSize)
        {
            var newKeys = new TKey[newSize];
            var newValues = new InternalValue[newSize];
            Array.Copy(keys, newKeys, Count);
            Array.Copy(values, newValues, Count);
            keys = newKeys;
            values = newValues;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddInternal(TKey key, InternalValue value)
        {
            if (ContainsKey(key))
            {
                return;
            }

            if (Count >= keys.Length)
            {
                Resize(keys.Length * 2);
            }

            keys[Count] = key;
            values[Count] = value;
            Count++;

            estimatedSizeBytes += EstimateKeySize(key) + EstimateValueSize(in value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryAddInternal(TKey key, InternalValue value)
        {
            if (ContainsKey(key))
            {
                return false;
            }

            if (Count >= keys.Length)
            {
                Resize(keys.Length * 2);
            }

            keys[Count] = key is string s ? (TKey)(object)String.Intern(s) : key;
            values[Count] = value;
            Count++;

            estimatedSizeBytes += EstimateKeySize(key) + EstimateValueSize(in value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long EstimateKeySize(TKey key)
        {
            return key switch
            {
                string s => sizeof(char) * (long)s.Length,
                int => sizeof(int),
                long => sizeof(long),
                double => sizeof(double),
                Guid => 16,
                _ => 0
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long EstimateValueSize(in InternalValue value)
        {
            return value.Type switch
            {
                InternalValue.ValueType.String => value.AsString() is {} s ? sizeof(char) * (long)s.Length : 0,
                InternalValue.ValueType.Int => sizeof(int),
                InternalValue.ValueType.Double => sizeof(double),
                InternalValue.ValueType.Bool => sizeof(bool),
                InternalValue.ValueType.DateTime => sizeof(long),
                _ => 0
            };
        }

        ~DictionaryNoBoxing()
        {
            Dispose();
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, InternalValue>>
        {
            private readonly TKey[] keys;
            private readonly InternalValue[] values;
            private readonly int count;
            private int index;

            internal Enumerator(TKey[] keys, InternalValue[] values, int count)
            {
                this.keys = keys;
                this.values = values;
                this.count = count;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < count;
            }

            public KeyValuePair<TKey, InternalValue> Current
            {
                get
                {
                    return new KeyValuePair<TKey, InternalValue>(keys[index], values[index]);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public void Reset()
            {
                index = -1;
            }

            public void Dispose() {}
        }
    }
}
