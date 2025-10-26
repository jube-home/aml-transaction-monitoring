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

namespace Jube.Dictionary.Interfaces
{
    using System.Diagnostics.CodeAnalysis;

    public interface ILruCacheConcurrentSizedDictionary<TKey, TValue>
    {
        long TotalSize
        {
            get;
        }
        ICollection<TKey> Keys
        {
            get;
        }
        ICollection<TValue> Values
        {
            get;
        }
        int Count
        {
            get;
        }
        bool IsReadOnly
        {
            get;
        }
        TValue this[TKey key]
        {
            get;
            set;
        }
        bool ContainsKey(TKey key);
        long EstimatedSizeBytes();
        void Add(KeyValuePair<TKey, TValue> item);
        void Add(TKey key, TValue value);
        IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();
        void Clear();
        bool Remove(TKey key);
        bool Remove(KeyValuePair<TKey, TValue> item);
        bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);
        bool Contains(KeyValuePair<TKey, TValue> item);
        void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex);
    }
}
