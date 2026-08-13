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
        private static readonly MessagePackSerializerOptions ContractlessStandardResolverWithCompression;
        private static readonly MessagePackSerializerOptions ContractlessStandardResolverWithoutCompression;
        private static readonly MessagePackSerializerOptions StandardWithCompression;
        private static readonly MessagePackSerializerOptions StandardWithoutCompression;
        private static readonly MessagePackSerializerOptions EnvelopeWithCompression;
        private static readonly MessagePackSerializerOptions EnvelopeWithoutCompression;

        static MessagePackSerializerOptionsHelper()
        {
            var contractlessResolver = CompositeResolver.Create(
                NativeDecimalResolver.Instance,
                NativeGuidResolver.Instance,
                NativeDateTimeResolver.Instance,
                ContractlessStandardResolver.Instance);

            ContractlessStandardResolverWithCompression = ContractlessStandardResolver.Options
                .WithCompression(MessagePackCompression.Lz4BlockArray)
                .WithResolver(contractlessResolver);

            ContractlessStandardResolverWithoutCompression = ContractlessStandardResolver.Options
                .WithResolver(contractlessResolver);

            var standardResolver = CompositeResolver.Create(
                NativeDecimalResolver.Instance,
                NativeGuidResolver.Instance,
                NativeDateTimeResolver.Instance,
                StandardResolver.Instance);

            StandardWithCompression = StandardResolver.Options
                .WithCompression(MessagePackCompression.Lz4BlockArray)
                .WithResolver(standardResolver);

            StandardWithoutCompression = StandardResolver.Options
                .WithResolver(standardResolver);

            EnvelopeWithCompression = StandardResolver.Options
                .WithCompression(MessagePackCompression.Lz4BlockArray)
                .WithResolver(EnvelopeDictionaryNoBoxingResolver.Instance);

            EnvelopeWithoutCompression = StandardResolver.Options
                .WithResolver(EnvelopeDictionaryNoBoxingResolver.Instance);
        }

        public static MessagePackSerializerOptions EnveloperMessagePackSerializerWithCompressionOptions(bool compression)
        {
            return compression ? EnvelopeWithCompression : EnvelopeWithoutCompression;
        }

        public static MessagePackSerializerOptions ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(bool compression)
        {
            return compression ? ContractlessStandardResolverWithCompression : ContractlessStandardResolverWithoutCompression;
        }

        public static MessagePackSerializerOptions StandardMessagePackSerializerWithCompressionOptions(bool compression)
        {
            return compression ? StandardWithCompression : StandardWithoutCompression;
        }
    }
}
