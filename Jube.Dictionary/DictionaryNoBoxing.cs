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

    public sealed class DictionaryNoBoxing : IDictionaryNoBoxing, IEnumerable<KeyValuePair<string, InternalValue>>,
        IDisposable, ISized
    {
        private const int DefaultCapacity = 8;
        private long estimatedSizeBytes;
        private string[] keys;
        private InternalValue[] values;

        public DictionaryNoBoxing()
        {
            keys = new string[DefaultCapacity];
            values = new InternalValue[DefaultCapacity];
            Count = 0;
        }

        public DictionaryNoBoxing(int capacity = DefaultCapacity)
        {
            keys = new string[capacity];
            values = new InternalValue[capacity];
            Count = 0;
        }

        public InternalValue this[string key]
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

        public int Count
        {
            get;
            private set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(string key, string? value)
        {
            return TryAddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(string key, double value)
        {
            return TryAddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(string key, bool value)
        {
            return TryAddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(string key, DateTime value)
        {
            return TryAddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(string key, int value)
        {
            return TryAddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(string key, Guid value)
        {
            return TryAddInternal(key, new InternalValue(value));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(string key)
        {
            return IndexOfKey(key) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(string key)
        {
            var idx = IndexOfKey(key);
            if (idx < 0)
            {
                return false;
            }

            estimatedSizeBytes -= EstimateStringSize(keys[idx]) + EstimateValueSize(in values[idx]);

            Array.Copy(keys, idx + 1, keys, idx, Count - idx - 1);
            Array.Copy(values, idx + 1, values, idx, Count - idx - 1);

            Count--;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(string key, out InternalValue value)
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(keys, values, Count);
        }

        public void Dispose()
        {
            // Clear internal fields
            Array.Clear(keys, 0, Count);
            Array.Clear(values, 0, Count);
 #pragma warning disable CS8625// Cannot convert null literal to non-nullable reference type.
            keys = null;
            values = null;
 #pragma warning restore CS8625// Cannot convert null literal to non-nullable reference type.
            Count = 0;

            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<KeyValuePair<string, InternalValue>> IEnumerable<KeyValuePair<string, InternalValue>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        private static long EstimateStringSize(string? s)
        {
            return s == null ? 0 : sizeof(char) * (long)s.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long EstimateValueSize(in InternalValue value)
        {
            return value.Type switch
            {
                InternalValue.ValueType.String => EstimateStringSize(value.AsString()),
                InternalValue.ValueType.Int => sizeof(int),
                InternalValue.ValueType.Double => sizeof(double),
                InternalValue.ValueType.Bool => sizeof(bool),
                InternalValue.ValueType.DateTime => sizeof(long),// stored as ticks
                _ => 0
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize(int newSize)
        {
            var newKeys = new string[newSize];
            var newValues = new InternalValue[newSize];

            Array.Copy(keys, newKeys, Count);
            Array.Copy(values, newValues, Count);

            keys = newKeys;
            values = newValues;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IndexOfKey(string key)
        {
            for (var i = 0; i < Count; i++)
            {
                if (keys[i] == key)
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(string key, string? value)
        {
            AddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(string key, double value)
        {
            AddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(string key, bool value)
        {
            AddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(string key, DateTime value)
        {
            AddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(string key, int value)
        {
            AddInternal(key, new InternalValue(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddInternal(string key, object value)
        {
            if (ContainsKey(key))
            {
                return;
            }

            var internalValue = value switch
            {
                int intValue => new InternalValue(intValue),
                double doubleValue => new InternalValue(doubleValue),
                _ => new InternalValue(value.ToString())
            };

            if (Count >= keys.Length)
            {
                Resize(keys.Length * 2);
            }

            keys[Count] = String.Intern(key);
            values[Count] = internalValue;
            Count++;

            estimatedSizeBytes += EstimateStringSize(key) + EstimateValueSize(in internalValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryAddInternal(string key, InternalValue value)
        {
            if (ContainsKey(key))
            {
                return false;
            }

            if (Count >= keys.Length)
            {
                Resize(keys.Length * 2);
            }

            keys[Count] = String.Intern(key);
            values[Count] = value;
            Count++;

            estimatedSizeBytes += EstimateStringSize(key) + EstimateValueSize(in value);

            return true;
        }

        ~DictionaryNoBoxing()
        {
            Dispose();
        }

        public struct Enumerator : IEnumerator<KeyValuePair<string, InternalValue>>
        {
            private readonly string[] keys;
            private readonly InternalValue[] values;
            private readonly int count;
            private int index;

            internal Enumerator(string[] keys, InternalValue[] values, int count)
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

            public KeyValuePair<string, InternalValue> Current
            {
                get
                {
                    return new KeyValuePair<string, InternalValue>(keys[index], values[index]);
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

            public void Dispose()
            {
            }
        }
    }
}
