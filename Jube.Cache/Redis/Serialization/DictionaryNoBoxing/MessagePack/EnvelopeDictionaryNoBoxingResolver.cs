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

namespace Jube.Cache.Redis.Serialization.DictionaryNoBoxing.MessagePack
{
    using System.Collections.Concurrent;
    using global::MessagePack;
    using global::MessagePack.Formatters;
    using global::MessagePack.Resolvers;

    public class EnvelopeDictionaryNoBoxingResolver : IFormatterResolver
    {
        public static readonly EnvelopeDictionaryNoBoxingResolver Instance = new EnvelopeDictionaryNoBoxingResolver();

        private readonly ConcurrentDictionary<Type, object> formatterCache = new ConcurrentDictionary<Type, object>();

        private EnvelopeDictionaryNoBoxingResolver() {}

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            // If the formatter is already cached, return it directly.
            if (formatterCache.TryGetValue(typeof(T), out var cachedFormatter))
            {
                return (IMessagePackFormatter<T>)cachedFormatter;
            }

            // If not cached, perform a direct check for our specific type and cache the formatter if applicable.
            if (typeof(T) != typeof(EnvelopeDictionaryNoBoxing))
            {
                return StandardResolver.Instance.GetFormatter<T>();
            }
            
            var formatter = new EnvelopeDictionaryNoBoxingMessagePackFormatter();
            formatterCache[typeof(T)] = formatter;
            return (IMessagePackFormatter<T>)formatter;

            // Fallback to the standard resolver for other types.
        }
    }
}
