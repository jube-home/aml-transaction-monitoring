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
    using Dictionary;
    using Dictionary.Models;
    using global::MessagePack;
    using global::MessagePack.Formatters;

     #pragma warning disable MsgPack009// Analyzer doesn't recognise resolver-based disambiguation for closed generics
    public class EnvelopeDictionaryNoBoxingStringMessagePackFormatter : IMessagePackFormatter<EnvelopeDictionaryNoBoxing<string>>
     #pragma warning restore MsgPack009
    {
        public void Serialize(ref MessagePackWriter writer, EnvelopeDictionaryNoBoxing<string> value,
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
                    case InternalValue.ValueType.Guid:
                    default:
                        writer.Write(kv.Value.AsString());
                        break;
                }
            }
        }

        public EnvelopeDictionaryNoBoxing<string> Deserialize(ref MessagePackReader reader,
            MessagePackSerializerOptions options)
        {
            var envelope = new EnvelopeDictionaryNoBoxing<string>
            {
                Version = reader.ReadByte()
            };

            var count = reader.ReadMapHeader();
            var data = new DictionaryNoBoxing<string>(count);
            envelope.Data = data;

            for (var j = 0; j < count; j++)
            {
                var key = reader.ReadString();

                switch (reader.NextMessagePackType)
                {
                    case MessagePackType.Integer:
                    {
                        var l = reader.ReadInt64();
                        try
                        {
                            if (l > Int32.MaxValue || l < Int32.MinValue)
                            {
                                data.AddUnchecked(key, DateTime.FromBinary(l));
                            }
                            else
                            {
                                data.AddUnchecked(key, (int)l);
                            }
                        }
                        catch (Exception)
                        {
                            // element-level failure only: skip this entry, keep the envelope intact
                        }
                        break;
                    }
                    case MessagePackType.Float:
                    {
                        var d = reader.ReadDouble();
                        try { data.AddUnchecked(key, d); }
                        catch (Exception)
                        {
                            /* skip */
                        }
                        break;
                    }
                    case MessagePackType.Boolean:
                    {
                        var b = reader.ReadBoolean();
                        try { data.AddUnchecked(key, b); }
                        catch (Exception)
                        {
                            /* skip */
                        }
                        break;
                    }
                    case MessagePackType.String:
                    {
                        var s = reader.ReadString();
                        try { data.AddUnchecked(key, s); }
                        catch (Exception)
                        {
                            /* skip */
                        }
                        break;
                    }
                    case MessagePackType.Unknown:
                    case MessagePackType.Nil:
                    case MessagePackType.Binary:
                    case MessagePackType.Array:
                    case MessagePackType.Map:
                    case MessagePackType.Extension:
                    default:
                        throw new MessagePackSerializationException(
                            $"Unsupported MessagePack type: {reader.NextMessagePackType}");
                }
            }

            return envelope;
        }
    }
}
