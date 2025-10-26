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

namespace Jube.Cache.Redis.Serialization
{
    using DictionaryNoBoxing.MessagePack;
    using MessagePack;
    using MessagePack.Resolvers;

    public static class MessagePackSerializerOptionsHelper
    {
        public static MessagePackSerializerOptions ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(bool compression)
        {
            var resolver = CompositeResolver.Create(
                NativeDecimalResolver.Instance,
                NativeGuidResolver.Instance,
                NativeDateTimeResolver.Instance,
                ContractlessStandardResolver.Instance
            );

            if (compression)
            {
                return ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray)
                    .WithResolver(resolver);
            }

            return ContractlessStandardResolver.Options
                .WithResolver(resolver);
        }

        public static MessagePackSerializerOptions StandardMessagePackSerializerWithCompressionOptions(bool compression)
        {
            var resolver = CompositeResolver.Create(
                NativeDecimalResolver.Instance,
                NativeGuidResolver.Instance,
                NativeDateTimeResolver.Instance,
                StandardResolver.Instance
            );

            if (compression)
            {
                return StandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray)
                    .WithResolver(resolver);
            }

            return StandardResolver.Options
                .WithResolver(resolver);
        }

        public static MessagePackSerializerOptions EnveloperMessagePackSerializerWithCompressionOptions(bool compression)
        {
            if (compression)
            {
                return StandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray)
                    .WithResolver(EnvelopeDictionaryNoBoxingResolver.Instance);
            }

            return StandardResolver.Options
                .WithResolver(EnvelopeDictionaryNoBoxingResolver.Instance);
        }
    }
}
