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

namespace Jube.Engine.EntityAnalysisModelInvoke.Extraction
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Context;
    using Context.Extensions;
    using Cryptography;
    using Data.Poco;
    using Dictionary;
    using DynamicEnvironment;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Extensions.YourNamespace.Extensions;
    using Helpers;
    using log4net;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using EntityAnalysisModel=EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

    public class EntityAnalysisModelJsonExtractor(
        EntityAnalysisModel entityAnalysisModel,
        Dictionary<int, EntityAnalysisModel> availableModels,
        DynamicEnvironment environment,
        ILog log)
    {
        public Context CreateContext(
            MemoryStream inputStream)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var entityAnalysisModelInstanceEntryPayload = EntityAnalysisModelInstanceEntryPayloadHelpers.Create(entityAnalysisModel);
            entityAnalysisModelInstanceEntryPayload.Payload = new DictionaryNoBoxing<string>(entityAnalysisModel.References.PayloadInitialSize);

            var reportDatabaseValues = new List<ArchiveKey>();
            entityAnalysisModelInstanceEntryPayload.ArchiveKeys = reportDatabaseValues;

            entityAnalysisModelInstanceEntryPayload.JObject = ParseJson(inputStream, entityAnalysisModel, entityAnalysisModelInstanceEntryPayload);

            var (entryId, referenceDate) = ExtractEntryAndReferenceDate(entityAnalysisModelInstanceEntryPayload.JObject, entityAnalysisModel, entityAnalysisModelInstanceEntryPayload.Payload);

            entityAnalysisModelInstanceEntryPayload.EntityInstanceEntryId = entryId;
            entityAnalysisModelInstanceEntryPayload.ReferenceDate = referenceDate;

            ProcessRequestXPaths(entityAnalysisModelInstanceEntryPayload.JObject, entityAnalysisModelInstanceEntryPayload, entityAnalysisModelInstanceEntryPayload.Payload, reportDatabaseValues, false);

            return new Context
            {
                StartBytesUsed = GC.GetAllocatedBytesForCurrentThread(),
                EntityAnalysisModel = entityAnalysisModel,
                AvailableEntityAnalysisModels = availableModels,
                EntityAnalysisModelInstanceEntryPayload = entityAnalysisModelInstanceEntryPayload,
                Stopwatch = stopwatch,
                Log = log,
                Random = new Random(Environment.TickCount ^ Guid.NewGuid().GetHashCode()),
                Environment = environment,
                Async = false
            };
        }

        private JObject ParseJson(MemoryStream inputStream, EntityAnalysisModel model,
            EntityAnalysisModelInstanceEntryPayload payload)
        {
            if (inputStream.Length == 0)
            {
                log.Info($"Json to Context Extractor: GUID payload {payload.EntityAnalysisModelInstanceEntryGuid} model id is {model.Instance.Id} has zero content length.");
                return null;
            }

            inputStream.Position = 0;
            var json = JObject.Load(new JsonTextReader(new StreamReader(inputStream, Encoding.UTF8))
            {
                DateParseHandling = DateParseHandling.None
            });

            log.Info($"Json to Context Extractor: GUID payload {payload.EntityAnalysisModelInstanceEntryGuid} model id is {model.Instance.Id} JSON parsed successfully.");
            return json;
        }

        private (string entryId, DateTime referenceDate) ExtractEntryAndReferenceDate(
            JObject json,
            EntityAnalysisModel model,
            DictionaryNoBoxing<string> payload)
        {
            var modelEntryValue = "";
            var referenceDateValue = DateTime.UtcNow;
            JToken jToken;

            try
            {
                jToken = json?.SelectToken(model.References.EntryXPath);
                if (jToken != null)
                {
                    modelEntryValue = jToken.ToString();
                }

                log.Info($"Json to Context Extractor: Entity {model.Instance.Id}: extracted entry via {model.References.EntryXPath} value {modelEntryValue}");
            }
            catch (Exception ex)
            {
                log.Error($"Json to Context Extractor: Could not extract entry path {model.References.EntryXPath}: {ex.Message}");
            }

            try
            {
                switch (model.References.ReferenceDatePayloadLocationTypeId)
                {
                    case 3:
                        referenceDateValue = DateTime.UtcNow;
                        break;

                    default:
                        jToken = json?.SelectToken(model.References.ReferenceDateXpath);
                        if (jToken != null)
                        {
                            referenceDateValue = DateTimeOffset.TryParse(jToken.Value<string>(), CultureInfo.InvariantCulture,
                                environment.AppSettings("AssumeLocalDateInPayloadExtraction").Equals("True", StringComparison.CurrentCultureIgnoreCase)
                                    ? DateTimeStyles.AssumeLocal
                                    : DateTimeStyles.AssumeUniversal,
                                out var dto)
                                ? dto.UtcDateTime
                                : DateTime.UtcNow;
                        }
                        break;
                }

                log.Info($"Json to Context Extractor: Entity {model.Instance.Id}: extracted reference date {referenceDateValue}");
            }
            catch (Exception ex)
            {
                log.Error($"Json to Context Extractor: Could not extract reference date: {ex.Message}");
            }

            payload.TryAdd(model.References.ReferenceDateName, referenceDateValue);
            payload.TryAdd(model.References.EntryName, modelEntryValue);

            return (modelEntryValue, referenceDateValue);
        }

        private void ProcessRequestXPaths(
            JObject json,
            EntityAnalysisModelInstanceEntryPayload entityInstanceEntryPayloadStore,
            DictionaryNoBoxing<string> payload,
            List<ArchiveKey> reportDatabaseValues,
            bool isReprocess)
        {
            if (entityAnalysisModel.Collections?.EntityAnalysisModelRequestXPaths == null)
            {
                return;
            }

            if (log.IsInfoEnabled)
            {
                log.Info($"Json to Context Extractor: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id {entityAnalysisModel.Instance.Id} beginning request XPath extraction.");
            }

            foreach (var xPath in entityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths)
            {
                try
                {
                    if (payload.ContainsKey(xPath.Name))
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info($"Json to Context Extractor: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} duplicate key {xPath.Name} detected, skipping extraction.");
                        }
                        continue;
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Json to Context Extractor: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} evaluating {xPath.Name} with path {xPath.XPath}.");
                    }

                    string value = null;
                    var defaultFallback = false;

                    try
                    {
                        value = json.SelectToken(xPath.XPath)?.ToString();

                        if (value == null)
                        {
                            if (!String.IsNullOrEmpty(xPath.DefaultValue))
                            {
                                value = xPath.DefaultValue;
                                defaultFallback = true;
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        if (!String.IsNullOrEmpty(xPath.DefaultValue))
                        {
                            value = xPath.DefaultValue;
                            defaultFallback = true;

                            if (log.IsInfoEnabled)
                            {
                                log.Info($"Json to Context Extractor: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} XPath {xPath.XPath} failed: {ex.Message}. Using default {xPath.DefaultValue}.");
                            }
                        }
                        else
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info($"Json to Context Extractor: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} XPath {xPath.XPath} failed: {ex.Message}. Default value is null and will be skipped.");
                            }
                        }
                    }

                    if (value == null)
                    {
                        continue;
                    }

                    ProcessTypedInsertion(entityInstanceEntryPayloadStore,
                        xPath,
                        value,
                        defaultFallback,
                        payload,
                        reportDatabaseValues,
                        isReprocess);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.Error($"Json to Context Extractor: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} unhandled error processing {xPath.Name}: {ex}");
                }
            }
        }

        private void ProcessTypedInsertion(EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayload,
            EntityAnalysisModelRequestXPath xPath,
            string value,
            bool defaultFallback,
            DictionaryNoBoxing<string> payload,
            List<ArchiveKey> reportDatabaseValues,
            bool isReprocess)
        {
            try
            {
                switch (xPath.DataTypeId)
                {
                    case 1:
                        value = xPath.EncryptionId switch
                        {
                            1 => entityAnalysisModel.Services.AesEncryption.Encrypt(value, IvMode.Deterministic),
                            2 => entityAnalysisModel.Services.AesEncryption.Encrypt(value, IvMode.Random),
                            _ => value
                        };

                        payload.TryAdd(xPath.Name, value);
                        entityAnalysisModel.ResolveDictionaryValueForField(entityAnalysisModelInstanceEntryPayload, log, xPath.Name);
                        reportDatabaseValues.AddArchiveKey(xPath, entityAnalysisModelInstanceEntryPayload, value, isReprocess: isReprocess);

                        log.Info($"Json to Context Extractor: GUID payload {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} XPath {xPath.XPath} value {value} as string {(defaultFallback ? "is default" : String.Empty)}.");

                        break;
                    case 2:
                        if (Int32.TryParse(value, out var intVal))
                        {
                            payload.TryAdd(xPath.Name, intVal);
                            reportDatabaseValues.AddArchiveKey(xPath, entityAnalysisModelInstanceEntryPayload, valueInt: Int32.Parse(value), isReprocess: isReprocess);

                            log.Info($"Json to Context Extractor: GUID payload {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} XPath {xPath.XPath} value {value} as integer {(defaultFallback ? "is default" : String.Empty)}.");
                        }
                        else
                        {
                            log.Info($"Json to Context Extractor: GUID payload {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} XPath {xPath.XPath} value {value} as null integer {(defaultFallback ? "is default" : String.Empty)}.");
                        }

                        break;
                    case 4:
                        DateTime dateValue;
                        if (defaultFallback && Int32.TryParse(xPath.DefaultValue, out var daysBack))
                        {
                            dateValue = DateTime.UtcNow.AddDays(-daysBack);
                        }
                        else
                        {
                            dateValue = DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                                environment.AppSettings("AssumeLocalDateInPayloadExtraction").Equals("True", StringComparison.CurrentCultureIgnoreCase)
                                    ? DateTimeStyles.AssumeLocal
                                    : DateTimeStyles.AssumeUniversal,
                                out var dto)
                                ? dto.UtcDateTime
                                : DateTime.UtcNow;
                        }

                        payload.TryAdd(xPath.Name, dateValue);
                        reportDatabaseValues.AddArchiveKey(xPath, entityAnalysisModelInstanceEntryPayload, valueDate: dateValue, isReprocess: isReprocess);

                        log.Info($"Json to Context Extractor: GUID payload {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} XPath {xPath.XPath} value {value} as datetime {(defaultFallback ? "is default" : String.Empty)}.");

                        break;
                    case 5:
                        var boolVal = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
                        payload.TryAdd(xPath.Name, boolVal);
                        reportDatabaseValues.AddArchiveKey(xPath, entityAnalysisModelInstanceEntryPayload, valueBool: boolVal, isReprocess: isReprocess);

                        log.Info($"Json to Context Extractor: GUID payload {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} XPath {xPath.XPath} value {value} as boolean {(defaultFallback ? "is default" : String.Empty)}.");

                        break;
                    case 6:
                    case 7:
                    case 3:
                        if (Double.TryParse(value, out var dblVal))
                        {
                            payload.TryAdd(xPath.Name, dblVal);
                            reportDatabaseValues.AddArchiveKey(xPath, entityAnalysisModelInstanceEntryPayload, valueFloat: Double.Parse(value), isReprocess: isReprocess);

                            log.Info($"Json to Context Extractor: GUID payload {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} XPath {xPath.XPath} value {value} as double {(defaultFallback ? "is default" : String.Empty)}.");
                        }
                        else
                        {
                            log.Info($"Json to Context Extractor: GUID payload {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} XPath {xPath.XPath} value {value} as null double {(defaultFallback ? "is default" : String.Empty)}.");
                        }
                        break;

                    default:
                        payload.TryAdd(xPath.Name, value);
                        reportDatabaseValues.AddArchiveKey(xPath, entityAnalysisModelInstanceEntryPayload, value, isReprocess: isReprocess);

                        log.Info($"Json to Context Extractor: GUID payload {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} XPath {xPath.XPath} value {value} as string {(defaultFallback ? "is default" : String.Empty)}.");

                        break;
                }

                if (log.IsInfoEnabled)
                {
                    log.Info($"Json to Context Extractor: {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} processed field {xPath.Name} of type {xPath.DataTypeId} with value {value}");
                }
            }
            catch (Exception ex)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info($"Json to Context Extractor: {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} error parsing {xPath.Name}: {ex.Message}. Defaulting to string insert.");
                }
                payload.TryAdd(xPath.Name, value);
            }
        }
    }
}
