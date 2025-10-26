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
    using System.Globalization;
    using global::MessagePack;
    using global::MessagePack.Formatters;
    using Jube.Dictionary.Models;
    using DictionaryNoBoxing=Dictionary.DictionaryNoBoxing;

    public class EnvelopeDictionaryNoBoxingMessagePackFormatter : IMessagePackFormatter<EnvelopeDictionaryNoBoxing>
    {
        public void Serialize(ref MessagePackWriter writer, EnvelopeDictionaryNoBoxing value,
            MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                return;
            }

            writer.Write(value.Version);

            if (value is not { Data: not null })
            {
                return;
            }
            writer.WriteMapHeader(value.Data.Count);

            foreach (var kv in value.Data)
            {
                writer.Write(kv.Key);

                switch (kv.Value.Type)
                {
                    case InternalValue.ValueType.Int:
                        writer.Write(kv.Value.AsInt());
                        break;
                    case InternalValue.ValueType.Double:
                        writer.Write(kv.Value.AsDouble());
                        break;
                    case InternalValue.ValueType.Bool:
                        writer.Write(kv.Value.AsBool());
                        break;
                    case InternalValue.ValueType.String:
                        writer.Write(kv.Value.AsString());
                        break;
                    case InternalValue.ValueType.DateTime:
                        writer.WriteInt64(kv.Value.AsDateTime().ToUniversalTime().ToBinary());
                        break;
                    case InternalValue.ValueType.None:
                    default:
                        writer.Write(kv.Value.AsString());
                        break;
                }
            }
        }

        public EnvelopeDictionaryNoBoxing Deserialize(ref MessagePackReader reader,
            MessagePackSerializerOptions options)
        {
            var envelope = new EnvelopeDictionaryNoBoxing
            {
                Version = reader.ReadByte()
            };

            var count = reader.ReadMapHeader();
            var data = new DictionaryNoBoxing(count);
            envelope.Data = data;

            for (var j = 0; j < count; j++)
            {
                var key = reader.ReadString();

                InternalValue value;

                switch (reader.NextMessagePackType)
                {
                    case MessagePackType.Integer:
                    {
                        var l = reader.ReadInt64();

                        if (l > Int32.MaxValue || l < Int32.MinValue)
                        {
                            value = new InternalValue(DateTime.FromBinary(l));
                        }
                        else
                        {
                            value = new InternalValue((int)l);
                        }

                        break;
                    }

                    case MessagePackType.Float:
                    {
                        value = new InternalValue(reader.ReadDouble());
                        break;
                    }
                    case MessagePackType.Boolean:
                    {
                        value = new InternalValue(reader.ReadBoolean());
                        break;
                    }
                    case MessagePackType.String:
                    {
                        var str = reader.ReadString();

                        value = DateTime.TryParse(str, null, DateTimeStyles.RoundtripKind,
                            out var dt)
                            ? new InternalValue(dt)
                            : new InternalValue(str);

                        break;

                    }
                    case MessagePackType.Unknown:
                    case MessagePackType.Nil:
                    case MessagePackType.Binary:
                    case MessagePackType.Array:
                    case MessagePackType.Map:
                    case MessagePackType.Extension:
                    default:
                    {
                        throw new MessagePackSerializationException(
                            $"Unsupported MessagePack type: {reader.NextMessagePackType}");
                    }
                }

                switch (value.Type)
                {
                    case InternalValue.ValueType.Int:
                    {
                        {
                            if (key != null)
                            {
                                data.TryAdd(key, value.AsInt());
                            }
                            break;
                        }
                    }
                    case InternalValue.ValueType.Double:
                    {
                        if (key != null)
                        {
                            data.TryAdd(key, value.AsDouble());
                        }
                        break;
                    }
                    case InternalValue.ValueType.Bool:
                    {
                        if (key != null)
                        {
                            data.TryAdd(key, value.AsBool());
                        }
                        break;
                    }
                    case InternalValue.ValueType.String:
                    {
                        if (key != null)
                        {
                            data.TryAdd(key, value.AsString());
                        }
                        break;
                    }
                    case InternalValue.ValueType.DateTime:
                    {
                        if (key != null)
                        {
                            data.TryAdd(key, value.AsDateTime());
                        }
                        break;
                    }
                    case InternalValue.ValueType.None:
                    {
                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return envelope;
        }
    }
}
