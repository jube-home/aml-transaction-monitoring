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

namespace Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload
{
    using System;
    using System.IO;
    using System.Linq;
    using AsyncInvocationCallbackToken;
    using Context;
    using Data.Extension;
    using Dictionary.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class BuildJsonResponses
    {
        public static byte[] BuildFullJson(AsyncInvocationCallbackToken payload,
            JsonSerializer serializer)
        {
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            var jsonWriter = new JsonTextWriter(streamWriter);

            serializer.Serialize(jsonWriter, payload);
            jsonWriter.Flush();
            streamWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream.ToArray();
        }

        public static byte[] BuildFullJson(EntityAnalysisModelInstanceEntryPayload payload,
            JsonSerializer serializer)
        {
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            var jsonWriter = new JsonTextWriter(streamWriter);

            serializer.Serialize(jsonWriter, payload);
            jsonWriter.Flush();
            streamWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream.ToArray();
        }

        public static byte[] BuildPartialResponsePayloadJson(Context context, JsonSerializer serializer)
        {
            var jObject = CreateJObject(context);
            AddCreateCaseToJObject(context, jObject);
            AddPayloadToJObject(context, jObject);
            AddInlineScriptPropertyPayloadToJObject(context, jObject);
            AddInlineFunctionToJObject(context, jObject);
            AddDictionaryToJObject(context, jObject);
            AddTtlCounterToJObject(context, jObject);
            AddSanctionToJObject(context, jObject);
            AddAbstractionToJObject(context, jObject);
            AddAbstractionCalculationToJObject(context, jObject);
            AddHttpAdaptationToJObject(context, jObject);
            AddExhaustiveAdaptationToJObject(context, jObject);
            AddActivationToJObject(context, jObject);

            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            var jsonWriter = new JsonTextWriter(streamWriter);

            jObject.WriteTo(jsonWriter);
            jsonWriter.Flush();
            streamWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream.ToArray();
        }

        private static void AddInlineScriptPropertyPayloadToJObject(Context context, JObject jObject)
        {
            var kvpEntityAnalysisModelPayloadJObject = (JObject)jObject["Payload"];
            if (kvpEntityAnalysisModelPayloadJObject == null)
            {
                kvpEntityAnalysisModelPayloadJObject = new JObject();
                jObject.Add("Payload", kvpEntityAnalysisModelPayloadJObject);
            }

            foreach (var entityAnalysisModelInlineScript in context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts)
            {
                foreach (var entityAnalysisModelInlineScriptPropertyAttribute in entityAnalysisModelInlineScript.EntityAnalysisModelInlineScriptPropertyAttributes)
                {
                    if (!entityAnalysisModelInlineScriptPropertyAttribute.Value.ResponsePayload)
                    {
                        continue;
                    }

                    var value = context.EntityAnalysisModelInstanceEntryPayload.Payload.FirstOrDefault(f => f.Key == entityAnalysisModelInlineScriptPropertyAttribute.Key);
                    if (value.Key != null)
                    {
                        switch (value.Value.Type)
                        {
                            case InternalValue.ValueType.String:
                            {
                                kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(entityAnalysisModelInlineScriptPropertyAttribute.Key, value.Value.AsString()));
                                break;
                            }
                            case InternalValue.ValueType.Guid:
                            {
                                kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(entityAnalysisModelInlineScriptPropertyAttribute.Key, value.Value.AsGuid()));
                                break;
                            }
                            case InternalValue.ValueType.DateTime:
                            {
                                kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(entityAnalysisModelInlineScriptPropertyAttribute.Key, value.Value.AsDateTime().ToString("O")));
                                break;
                            }
                            case InternalValue.ValueType.Bool:
                            {
                                kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(entityAnalysisModelInlineScriptPropertyAttribute.Key, value.Value.AsBool()));
                                break;
                            }
                            case InternalValue.ValueType.Int:
                            {
                                kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(entityAnalysisModelInlineScriptPropertyAttribute.Key, value.Value.AsInt()));
                                break;
                            }
                            case InternalValue.ValueType.Double:
                            {
                                kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(entityAnalysisModelInlineScriptPropertyAttribute.Key, value.Value.AsDouble()));
                                break;
                            }
                            case InternalValue.ValueType.None:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
        }

        private static void AddPayloadToJObject(Context context, JObject jObject)
        {
            var kvpEntityAnalysisModelRequestXPaths = context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Where(w => w.ResponsePayload).ToArray();
            if (!kvpEntityAnalysisModelRequestXPaths.Any())
            {
                return;
            }

            var kvpEntityAnalysisModelPayloadJObject = (JObject)jObject["Payload"];
            if (kvpEntityAnalysisModelPayloadJObject == null)
            {
                kvpEntityAnalysisModelPayloadJObject = new JObject();
                jObject.Add("Payload", kvpEntityAnalysisModelPayloadJObject);
            }

            foreach (var kvpEntityAnalysisModelRequestXPath in kvpEntityAnalysisModelRequestXPaths)
            {
                var value = context.EntityAnalysisModelInstanceEntryPayload.Payload.FirstOrDefault(f => f.Key == kvpEntityAnalysisModelRequestXPath.Name);
                if (value.Key != null)
                {
                    switch (value.Value.Type)
                    {
                        case InternalValue.ValueType.String:
                        {
                            kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(kvpEntityAnalysisModelRequestXPath.Name, value.Value.AsString()));
                            break;
                        }
                        case InternalValue.ValueType.Guid:
                        {
                            kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(kvpEntityAnalysisModelRequestXPath.Name, value.Value.AsGuid()));
                            break;
                        }
                        case InternalValue.ValueType.DateTime:
                        {
                            kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(kvpEntityAnalysisModelRequestXPath.Name, value.Value.AsDateTime().ToString("O")));
                            break;
                        }
                        case InternalValue.ValueType.Bool:
                        {
                            kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(kvpEntityAnalysisModelRequestXPath.Name, value.Value.AsBool()));
                            break;
                        }
                        case InternalValue.ValueType.Int:
                        {
                            kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(kvpEntityAnalysisModelRequestXPath.Name, value.Value.AsInt()));
                            break;
                        }
                        case InternalValue.ValueType.Double:
                        {
                            kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(kvpEntityAnalysisModelRequestXPath.Name, value.Value.AsDouble()));
                            break;
                        }
                        case InternalValue.ValueType.None:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private static void AddInlineFunctionToJObject(Context context, JObject jObject)
        {
            var kvpEntityAnalysisModelInlineFunctions = context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineFunctions.Where(w => w.ResponsePayload).ToArray();
            if (!kvpEntityAnalysisModelInlineFunctions.Any())
            {
                return;
            }

            var kvpEntityAnalysisModelPayloadJObject = (JObject)jObject["Payload"];
            if (kvpEntityAnalysisModelPayloadJObject == null)
            {
                kvpEntityAnalysisModelPayloadJObject = new JObject();
                jObject.Add("Payload", kvpEntityAnalysisModelPayloadJObject);
            }

            foreach (var kvpEntityAnalysisModelInlineFunction in kvpEntityAnalysisModelInlineFunctions)
            {
                var value = context.EntityAnalysisModelInstanceEntryPayload.Payload.FirstOrDefault(f => f.Key == kvpEntityAnalysisModelInlineFunction.Name);
                if (value.Key != null)
                {
                    switch (value.Value.Type)
                    {
                        case InternalValue.ValueType.String:
                        {
                            kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(kvpEntityAnalysisModelInlineFunction.Name, value.Value.AsString()));
                            break;
                        }
                        case InternalValue.ValueType.Guid:
                        {
                            kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(kvpEntityAnalysisModelInlineFunction.Name, value.Value.AsGuid()));
                            break;
                        }
                        case InternalValue.ValueType.DateTime:
                        {
                            kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(kvpEntityAnalysisModelInlineFunction.Name, value.Value.AsDateTime().ToString("O")));
                            break;
                        }
                        case InternalValue.ValueType.Bool:
                        {
                            kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(kvpEntityAnalysisModelInlineFunction.Name, value.Value.AsBool()));
                            break;
                        }
                        case InternalValue.ValueType.Int:
                        {
                            kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(kvpEntityAnalysisModelInlineFunction.Name, value.Value.AsInt()));
                            break;
                        }
                        case InternalValue.ValueType.Double:
                        {
                            kvpEntityAnalysisModelPayloadJObject.Add(new JProperty(kvpEntityAnalysisModelInlineFunction.Name, value.Value.AsDouble()));
                            break;
                        }
                        case InternalValue.ValueType.None:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private static void AddActivationToJObject(Context context, JObject jObject)
        {

            var kvpModelActivationRules = context.EntityAnalysisModel.Collections.ModelActivationRules.Where(w => w.ResponsePayload).ToArray();
            if (!kvpModelActivationRules.Any())
            {
                return;
            }

            var kvpModelActivationRulesJObject = new JObject();
            jObject.Add("Activation", kvpModelActivationRulesJObject);
            foreach (var kvpModelActivationRule in kvpModelActivationRules)
            {
                var (key, value) = context.EntityAnalysisModelInstanceEntryPayload.Activation.FirstOrDefault(f => f.Key == kvpModelActivationRule.Name);
                if (key != null)
                {
                    kvpModelActivationRulesJObject.Add(new JProperty(kvpModelActivationRule.Name, JObject.FromObject(value)));
                }
            }
        }

        private static void AddExhaustiveAdaptationToJObject(Context context, JObject jObject)
        {

            var kvpExhaustiveModels = context.EntityAnalysisModel.Collections.ExhaustiveModels.Where(w => w.ResponsePayload).ToArray();
            if (!kvpExhaustiveModels.Any())
            {
                return;
            }

            var kvpExhaustiveModelsJObject = new JObject();
            jObject.Add("ExhaustiveAdaptation", kvpExhaustiveModelsJObject);
            foreach (var kvpExhaustiveModel in kvpExhaustiveModels)
            {
                var (key, value) = context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation.FirstOrDefault(f => f.Key == kvpExhaustiveModel.Name);
                if (key != null)
                {
                    kvpExhaustiveModelsJObject.Add(new JProperty(kvpExhaustiveModel.Name, value.AsDouble()));
                }
            }
        }

        private static void AddHttpAdaptationToJObject(Context context, JObject jObject)
        {

            var kvpEntityAnalysisModelAdaptations = context.EntityAnalysisModel.Collections.EntityAnalysisModelAdaptations.Where(w => w.ResponsePayload).ToArray();
            if (!kvpEntityAnalysisModelAdaptations.Any())
            {
                return;
            }

            var kvpEntityAnalysisModelAdaptationsJObject = new JObject();
            jObject.Add("HttpAdaptation", kvpEntityAnalysisModelAdaptationsJObject);
            foreach (var kvpEntityAnalysisModelAdaptation in kvpEntityAnalysisModelAdaptations)
            {
                var (key, value) = context.EntityAnalysisModelInstanceEntryPayload.HttpAdaptation.FirstOrDefault(f => f.Key == kvpEntityAnalysisModelAdaptation.Name);
                if (key != null)
                {
                    kvpEntityAnalysisModelAdaptationsJObject.Add(new JProperty(kvpEntityAnalysisModelAdaptation.Name, value.Value));
                }
            }
        }

        private static void AddAbstractionCalculationToJObject(Context context, JObject jObject)
        {

            var kvpEntityAnalysisModelAbstractionCalculations = context.EntityAnalysisModel.Collections.EntityAnalysisModelAbstractionCalculations.Where(w => w.ResponsePayload).ToArray();
            if (!kvpEntityAnalysisModelAbstractionCalculations.Any())
            {
                return;
            }

            var kvpEntityAnalysisModelAbstractionCalculationsJObject = new JObject();
            jObject.Add("AbstractionCalculation", kvpEntityAnalysisModelAbstractionCalculationsJObject);
            foreach (var kvpEntityAnalysisModelAbstractionCalculation in kvpEntityAnalysisModelAbstractionCalculations)
            {
                var (key, value) = context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation.FirstOrDefault(f => f.Key == kvpEntityAnalysisModelAbstractionCalculation.Name);
                if (key != null)
                {
                    kvpEntityAnalysisModelAbstractionCalculationsJObject.Add(new JProperty(kvpEntityAnalysisModelAbstractionCalculation.Name, value.AsDouble()));
                }
            }
        }

        private static void AddAbstractionToJObject(Context context, JObject jObject)
        {

            var kvpAbstractions = context.EntityAnalysisModel.Collections.ModelAbstractionRules.Where(w => w.ResponsePayload).ToArray();
            if (!kvpAbstractions.Any())
            {
                return;
            }

            var kvpAbstractionsJObject = new JObject();
            jObject.Add("Abstraction", kvpAbstractionsJObject);
            foreach (var kvpAbstraction in kvpAbstractions)
            {
                var (key, value) = context.EntityAnalysisModelInstanceEntryPayload.Abstraction.FirstOrDefault(f => f.Key == kvpAbstraction.Name);
                if (key != null)
                {
                    kvpAbstractionsJObject.Add(new JProperty(kvpAbstraction.Name, value.AsDouble()));
                }
            }
        }

        private static void AddSanctionToJObject(Context context, JObject jObject)
        {

            var kvpEntityAnalysisModelSanctions = context.EntityAnalysisModel.Collections.EntityAnalysisModelSanctions.Where(w => w.ResponsePayload).ToArray();
            if (!kvpEntityAnalysisModelSanctions.Any())
            {
                return;
            }

            var kvpEntityAnalysisModelSanctionsJObject = new JObject();
            jObject.Add("Sanction", kvpEntityAnalysisModelSanctionsJObject);
            foreach (var kvpEntityAnalysisModelSanction in kvpEntityAnalysisModelSanctions)
            {
                var (key, value) = context.EntityAnalysisModelInstanceEntryPayload.Sanction.FirstOrDefault(f => f.Key == kvpEntityAnalysisModelSanction.Name);
                if (key != null)
                {
                    kvpEntityAnalysisModelSanctionsJObject.Add(new JProperty(kvpEntityAnalysisModelSanction.Name, value.AsDouble()));
                }
            }
        }

        private static void AddTtlCounterToJObject(Context context, JObject jObject)
        {

            var kvpModelTtlCounters = context.EntityAnalysisModel.Collections.ModelTtlCounters.Where(w => w.ResponsePayload).ToArray();
            if (!kvpModelTtlCounters.Any())
            {
                return;
            }

            var kvpModelTtlCountersJObject = new JObject();
            jObject.Add("TtlCounter", kvpModelTtlCountersJObject);
            foreach (var kvpModelTtlCounter in kvpModelTtlCounters)
            {
                var (key, value) = context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.FirstOrDefault(f => f.Key == kvpModelTtlCounter.Name);
                if (key != null)
                {
                    kvpModelTtlCountersJObject.Add(new JProperty(kvpModelTtlCounter.Name, value.AsDouble()));
                }
            }
        }

        private static void AddDictionaryToJObject(Context context, JObject jObject)
        {

            var responsePayloadDictionaryNames = context.EntityAnalysisModel.Dependencies.KvpDictionaries
                .Where(w => w.Value.ResponsePayload)
                .Select(s => s.Value.Name)
                .ToHashSet();

            if (responsePayloadDictionaryNames.Count <= 0)
            {
                return;
            }

            var kvpDictionariesJObject = new JObject();
            jObject.Add("Dictionary", kvpDictionariesJObject);
            foreach (var kvpDictionary in context.EntityAnalysisModelInstanceEntryPayload.Dictionary)
            {
                if (responsePayloadDictionaryNames.Contains(kvpDictionary.Key))
                {
                    kvpDictionariesJObject.Add(new JProperty(kvpDictionary.Key, kvpDictionary.Value.AsDouble()));
                }
            }
        }

        private static void AddCreateCaseToJObject(Context context, JObject jObject)
        {

            if (context.EntityAnalysisModelInstanceEntryPayload.CreateCase != null)
            {
                jObject.Add("CreateCase", JToken.FromObject(context.EntityAnalysisModelInstanceEntryPayload.CreateCase));
            }
        }

        private static JObject CreateJObject(Context context)
        {

            var jObject = new JObject
            {
                {
                    "CreatedDate", context.EntityAnalysisModelInstanceEntryPayload.CreatedDate
                },
                {
                    "EntityAnalysisModelInstanceEntryGuid", context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                },
                {
                    "EntityInstanceEntryId", context.EntityAnalysisModelInstanceEntryPayload.EntityInstanceEntryId
                },
                {
                    "ReferenceDate", context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate
                },
                {
                    "ResponseElevation", JToken.FromObject(context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation)
                }
            };
            return jObject;
        }
    }
}
