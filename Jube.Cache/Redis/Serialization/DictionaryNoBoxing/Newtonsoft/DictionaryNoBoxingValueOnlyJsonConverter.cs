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

namespace Jube.Cache.Redis.Serialization.DictionaryNoBoxing.Newtonsoft
{
    using global::Newtonsoft.Json;
    using Jube.Dictionary.Models;
    using DictionaryNoBoxing=Dictionary.DictionaryNoBoxing;

    public class DictionaryNoBoxingValueOnlyNewtonsoftConverter : JsonConverter<DictionaryNoBoxing>
    {
        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override void WriteJson(JsonWriter writer, DictionaryNoBoxing value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            if (value != null)
            {
                foreach (var kv in value)
                {
                    writer.WritePropertyName(kv.Key);
                    switch (kv.Value.Type)
                    {
                        case InternalValue.ValueType.String:
                            writer.WriteValue(kv.Value.AsString());
                            break;
                        case InternalValue.ValueType.DateTime:
                            writer.WriteValue(kv.Value.AsDateTime().ToString("O"));
                            break;
                        case InternalValue.ValueType.Bool:
                            writer.WriteValue(kv.Value.AsBool());
                            break;
                        case InternalValue.ValueType.Double:
                            writer.WriteValue(kv.Value.AsDouble());
                            break;
                        case InternalValue.ValueType.Int:
                            writer.WriteValue(kv.Value.AsInt());
                            break;
                        case InternalValue.ValueType.None:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            writer.WriteEndObject();
        }

        public override DictionaryNoBoxing ReadJson(JsonReader reader, Type objectType, DictionaryNoBoxing existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Deserialization not implemented.");
        }
    }
}
