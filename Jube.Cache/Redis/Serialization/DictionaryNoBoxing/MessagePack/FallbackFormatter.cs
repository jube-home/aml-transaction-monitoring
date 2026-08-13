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
    using global::MessagePack.Formatters;

    public static class FallbackFormatter<T>
    {
        public static readonly IMessagePackFormatter<T> NotSupportedFormatter = CreateThrowingFormatter();

        private static IMessagePackFormatter<T> CreateThrowingFormatter()
        {
            throw new NotSupportedException(
                $"{nameof(EnvelopeDictionaryNoBoxingResolver)} does not have a no-boxing formatter " +
                $"registered for {typeof(T).FullName}. Add an explicit formatter for this TKey - " +
                "do not fall back to a generic/dynamic resolver in this hot path.");
        }
    }
}
