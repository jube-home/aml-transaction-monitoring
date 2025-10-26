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

using CacheTtlCounterEntryRepository=Jube.Cache.Redis.CacheTtlCounterEntryRepository;
using CacheTtlCounterRepository=Jube.Cache.Redis.CacheTtlCounterRepository;
// ReSharper disable MemberCanBePrivate.Global

namespace Jube.Engine.Invoke
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Abstraction;
    using Cache;
    using Cache.Redis.Interfaces;
    using Cache.Redis.Models;
    using Data.Cache.Postgres.Callback;
    using Data.Extension;
    using Data.Messaging;
    using Data.Poco;
    using Dictionary;
    using DynamicEnvironment;
    using Extensions;
    using Helpers;
    using Helpers.TaskHelper;
    using log4net;
    using Model;
    using Model.Processing;
    using Model.Processing.CaseManagement;
    using Model.Processing.Payload;
    using Model.Processing.Payload.Performance;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using RabbitMQ.Client;
    using Reflect;
    using Sanctions;
    using EntityAnalysisModel=Model.EntityAnalysisModel;
    using EntityAnalysisModelAbstractionRule=Model.EntityAnalysisModelAbstractionRule;
    using EntityAnalysisModelActivationRule=Model.EntityAnalysisModelActivationRule;

    public class EntityAnalysisModelInvoke(
        ILog log,
        DynamicEnvironment jubeEnvironment,
        IModel rabbitMqChannel,
        CacheService cacheService,
        ConcurrentQueue<Notification> pendingNotification,
        Random seeded,
        Dictionary<int, EntityAnalysisModel> models)
    {
        private long startBytesUsedForInvokeThread;

        public MemoryStream ResponseJson { get; set; }

        public DictionaryNoBoxing CachePayloadDocumentStore { get; set; }

        public DictionaryNoBoxing CachePayloadDocumentResponse { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        public EntityAnalysisModelInstanceEntryPayload EntityAnalysisModelInstanceEntryPayloadStore { get; set; }

        private Dictionary<int, List<DictionaryNoBoxing>> AbstractionRuleMatches { get; } = new Dictionary<int, List<DictionaryNoBoxing>>();

        // ReSharper disable once CollectionNeverQueried.Global
        public Dictionary<string, Dictionary<string, double>> EntityInstanceEntryAdaptationResponses { get; set; } = new Dictionary<string, Dictionary<string, double>>();

        public Dictionary<string, double> EntityInstanceEntryAbstractionResponse { get; } = new Dictionary<string, double>();

        public Dictionary<string, int> EntityInstanceEntryTtlCountersResponse { get; } = new Dictionary<string, int>();

        private PooledDictionary<string, double> EntityInstanceEntrySanctions { get; } = new PooledDictionary<string, double>();

        // ReSharper disable once CollectionNeverQueried.Global
        public Dictionary<string, double> EntityInstanceEntrySanctionsResponse { get; } = new Dictionary<string, double>();

        private PooledDictionary<string, double> EntityInstanceEntryDictionaryKvPs { get; } = new PooledDictionary<string, double>();

        // ReSharper disable once CollectionNeverQueried.Global
        public Dictionary<string, double> EntityInstanceEntryDictionaryKvPsResponse { get; } = new Dictionary<string, double>();

        // ReSharper disable once CollectionNeverQueried.Global
        public Dictionary<int, EntityAnalysisModelActivationRule> EntityInstanceEntryActivationResponse { get; } = new Dictionary<int, EntityAnalysisModelActivationRule>();

        public Dictionary<string, double> EntityInstanceEntryAbstractionCalculationResponse { get; } = new Dictionary<string, double>();

        public Dictionary<int, EntityAnalysisModel> Models { get; set; } = models;

        private bool Finished { get; set; }

        public Random Seeded { get; set; } = seeded;

        public List<ArchiveKey> ReportDatabaseValues { get; set; } = [];

        public Stopwatch Stopwatch { get; set; } = new Stopwatch();
        public bool Reprocess { get; set; }

        public bool InError { get; set; }

        public string ErrorMessage { get; set; }

        public bool AsyncEnableCallback { get; set; }

        public async Task ParseAndInvoke(EntityAnalysisModel entityAnalysisModel, MemoryStream inputStream,
            bool async, long inputLength,
            ConcurrentQueue<EntityAnalysisModelInvoke> pendingEntityInvoke)
        {
            StartMeasuresForInvokeThread();

            EntityAnalysisModel = entityAnalysisModel;

            var entityInstanceEntryPayloadStore = new EntityAnalysisModelInstanceEntryPayload
            {
                EntityAnalysisModelId = EntityAnalysisModel.Id,
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                EntityAnalysisModelGuid = entityAnalysisModel.Guid,
                Abstraction = new PooledDictionary<string, double>(EntityAnalysisModel.ModelAbstractionRules.Count),
                Activation = new PooledDictionary<string, EntityModelActivationRulePayload>(),
                Tag = new PooledDictionary<string, double>(EntityAnalysisModel.EntityAnalysisModelTags.Count),
                Dictionary = new PooledDictionary<string, double>(EntityAnalysisModel.KvpDictionaries.Count),
                TtlCounter = new PooledDictionary<string, int>(EntityAnalysisModel.ModelTtlCounters.Count),
                Sanction = new PooledDictionary<string, double>(EntityAnalysisModel.EntityAnalysisModelSanctions.Count),
                AbstractionCalculation =
                    new PooledDictionary<string, double>(EntityAnalysisModel.EntityAnalysisModelAbstractionCalculations
                        .Count),
                HttpAdaptation =
                    new PooledDictionary<string, double>(entityAnalysisModel.EntityAnalysisModelAdaptations.Count),
                ExhaustiveAdaptation = new PooledDictionary<string, double>(entityAnalysisModel.ExhaustiveModels.Count),
                InvokeThreadPerformance = new InvokeThreadPerformance
                {
                    ComputeTime = new InvokeThreadComputeTime()
                }
            };

            var cachePayloadDocumentStore = new DictionaryNoBoxing(entityAnalysisModel.DictionaryNoBoxingInitialSize);
            var cachePayloadDocumentResponse = new DictionaryNoBoxing(entityAnalysisModel.DictionaryNoBoxingInitialSize);
            var reportDatabaseValues = new List<ArchiveKey>();

            try
            {
                JObject json = null;
                var modelEntryValue = "";
                var referenceDateValue = default(DateTime);

                JToken jToken = null;
                if (inputLength > 0)
                {
                    json = JObject.Parse(Encoding.UTF8.GetString(inputStream.ToArray()));

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} JSON has been parsed.  The outer JSON is {json}");
                    }
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} is JSON but has zero content length.");
                    }
                }

                try
                {
                    if (json != null)
                    {
                        jToken = json.SelectToken(entityAnalysisModel.EntryXPath);
                    }

                    if (jToken != null)
                    {
                        modelEntryValue = jToken.ToString();
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} will has extracted the JSON Path for ENTRY identifier {entityAnalysisModel.EntryXPath} with value of {modelEntryValue}.");
                    }
                }
                catch (Exception ex)
                {
                    InError = true;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} could not extract the JSON Path or Querystring for ENTRY identifier {entityAnalysisModel.EntryXPath} with exception message of {ex.Message}.");
                    }

                    ErrorMessage = "Could not locate model entry value.";
                }

                try
                {
                    switch (entityAnalysisModel.ReferenceDatePayloadLocationTypeId)
                    {
                        case 3:
                            referenceDateValue = DateTime.Now;

                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} will has extracted the Reference Date {entityAnalysisModel.EntryXPath} with value of {modelEntryValue} with the system time.");
                            }

                            break;
                        default:
                        {
                            if (json != null)
                            {
                                jToken = json.SelectToken(entityAnalysisModel.ReferenceDateXpath);
                            }

                            if (jToken is { Type: JTokenType.Date })
                            {
                                referenceDateValue = Convert.ToDateTime(jToken);
                            }
                            else
                            {
                                if (jToken != null && !DateTime.TryParse(jToken.ToString(),
                                        out referenceDateValue))
                                {
                                    referenceDateValue = DateTime.Now;

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} will has extracted the JSON Path for Reference Date {entityAnalysisModel.EntryXPath} with value of {modelEntryValue} with a promiscuous parse, but it failed,  so it has been set to the system time.");
                                    }
                                }
                                else
                                {
                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} will has extracted the JSON Path for Reference Date {entityAnalysisModel.EntryXPath} with value of {modelEntryValue} with a promiscuous parse.");
                                    }
                                }
                            }

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    InError = true;

                    log.Error($"Could not locate model date value {ex}.");

                    ErrorMessage = "Could not locate model date value.";
                }

                if (!InError)
                {
                    entityInstanceEntryPayloadStore.CreatedDate = DateTime.Now;
                    entityInstanceEntryPayloadStore.R = Seeded.NextDouble();
                    entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceName = Dns.GetHostName();
                    entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceGuid =
                        entityAnalysisModel.EntityAnalysisInstanceGuid;
                    entityInstanceEntryPayloadStore.EntityAnalysisModelName =
                        entityAnalysisModel.Name;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} had added the cache db GUID _id to the entry as {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} which is the same as the main GUID.");
                    }

                    entityInstanceEntryPayloadStore.ReferenceDate = referenceDateValue;
                    if (!cachePayloadDocumentStore.ContainsKey(entityAnalysisModel.ReferenceDateName))
                    {
                        cachePayloadDocumentStore.TryAdd(entityAnalysisModel.ReferenceDateName,
                            entityInstanceEntryPayloadStore.ReferenceDate);
                        cachePayloadDocumentResponse.TryAdd(entityAnalysisModel.ReferenceDateName,
                            entityInstanceEntryPayloadStore.ReferenceDate);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} added reference date with the field name {entityAnalysisModel.ReferenceDateName} and value {referenceDateValue}.");
                        }
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} already contains a reference date with the field name {entityAnalysisModel.ReferenceDateName}. This is not ideal,  try and make distinct.");
                        }
                    }

                    entityInstanceEntryPayloadStore.EntityInstanceEntryId = modelEntryValue;
                    if (!cachePayloadDocumentStore.ContainsKey(entityAnalysisModel.EntryName))
                    {
                        cachePayloadDocumentStore.TryAdd(entityAnalysisModel.EntryName,
                            entityInstanceEntryPayloadStore.EntityInstanceEntryId);
                        cachePayloadDocumentResponse.TryAdd(entityAnalysisModel.EntryName,
                            entityInstanceEntryPayloadStore.EntityInstanceEntryId);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} added field with the name {entityAnalysisModel.EntryName} and value {modelEntryValue} when adding the Entry.");
                        }
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} already contains a field with the name {entityAnalysisModel.EntryName} when adding the Entry. This is not ideal,  try and make distinct.");
                        }
                    }

                    if (!InError)
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} is now going to loop around all of the XPath Requests specified to perform extractions of data.");
                        }

                        foreach (var xPath in entityAnalysisModel.EntityAnalysisModelRequestXPaths)
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} evaluating {xPath.Name}.");
                            }

                            if (!cachePayloadDocumentStore.ContainsKey(xPath.Name))
                            {
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} no duplication on {xPath.Name}.");
                                }

                                string value;
                                var defaultFallback = false;
                                try
                                {
                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name} is in the body of the POST.");
                                    }

                                    value = json.SelectToken(xPath.XPath).ToString();

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}JSON Path {xPath.XPath} has extracted value {value}.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    value = xPath.DefaultValue;
                                    defaultFallback = true;

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name} Querystring {xPath.XPath} has caused an error of {ex.Message} and has fallen back to default value of {xPath.DefaultValue}.");
                                    }
                                }

                                try
                                {
                                    if (value != null)
                                    {
                                        switch (xPath.DataTypeId)
                                        {
                                            case 1:
                                            {
                                                cachePayloadDocumentStore.TryAdd(xPath.Name, value);

                                                if (log.IsInfoEnabled)
                                                {
                                                    log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and been typed in the BSON document as a string.");
                                                }

                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.TryAdd(
                                                        xPath.Name, value);

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                                    }
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueString = value,
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            entityInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                                    }
                                                }

                                                break;
                                            }

                                            case 2:
                                            {
                                                cachePayloadDocumentStore.TryAdd(xPath.Name, Int32.Parse(value));

                                                if (log.IsInfoEnabled)
                                                {
                                                    log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and been typed in the BSON document as an integer.");
                                                }

                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.TryAdd(
                                                        xPath.Name, Int32.Parse(value));

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                                    }
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueInteger = Int32.Parse(value),
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });
                                                }

                                                if (log.IsInfoEnabled)
                                                {
                                                    log.Info(
                                                        $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                                }

                                                break;
                                            }

                                            case 3:
                                            {
                                                cachePayloadDocumentStore.TryAdd(xPath.Name, Double.Parse(value));

                                                if (log.IsInfoEnabled)
                                                {
                                                    log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and been typed in the BSON document as Float.");
                                                }

                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.TryAdd(
                                                        xPath.Name, Double.Parse(value));

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                                    }
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueFloat = Double.Parse(value),
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                                    }
                                                }

                                                break;
                                            }

                                            case 4 when defaultFallback:
                                            {
                                                var fallbackDate =
                                                    DateTime.Now.AddDays(Int32.Parse(xPath.DefaultValue) * -1);

                                                cachePayloadDocumentStore.TryAdd(xPath.Name,
                                                    fallbackDate);

                                                if (log.IsInfoEnabled)
                                                {
                                                    log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and been typed in the BSON document as an Date.");
                                                }

                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.TryAdd(
                                                        xPath.Name, fallbackDate);

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                                    }
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueDate = fallbackDate,
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                                    }
                                                }

                                                break;
                                            }

                                            case 4 when DateTime.TryParse(value, out var dateValue):
                                            {
                                                cachePayloadDocumentStore.TryAdd(xPath.Name, dateValue);

                                                if (log.IsInfoEnabled)
                                                {
                                                    log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and been typed in the BSON document as an Date.");
                                                }

                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.TryAdd(
                                                        xPath.Name, dateValue);

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                                    }
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueDate = dateValue,
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                                    }
                                                }

                                                break;
                                            }
                                            case 4:
                                            {
                                                cachePayloadDocumentStore.TryAdd(xPath.Name, DateTime.Now);
                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.TryAdd(
                                                        xPath.Name, DateTime.Now);

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                                    }
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueDate = DateTime.Now,
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                                    }
                                                }

                                                break;
                                            }

                                            case 5:
                                            {
                                                var valueBoolean =
                                                    value.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                                                    value == "1";
                                                cachePayloadDocumentStore.TryAdd(xPath.Name, valueBoolean);

                                                if (log.IsInfoEnabled)
                                                {
                                                    log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {valueBoolean} and been typed in the BSON document as an Boolean.");
                                                }

                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.TryAdd(
                                                        xPath.Name, valueBoolean);
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueBoolean = (byte)(valueBoolean ? 1 : 0),
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {valueBoolean} has been added to the report payload with the name of {xPath.Name}.");
                                                    }
                                                }

                                                break;
                                            }
                                            case 6 or 7:
                                            {
                                                cachePayloadDocumentStore.TryAdd(xPath.Name, Double.Parse(value));

                                                if (log.IsInfoEnabled)
                                                {
                                                    log.Info(
                                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and been typed in the BSON document as Lat or Long.");
                                                }

                                                if (xPath.ResponsePayload)
                                                {
                                                    cachePayloadDocumentResponse.TryAdd(
                                                        xPath.Name, Double.Parse(value));

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                                    }
                                                }

                                                if (xPath.ReportTable && !Reprocess)
                                                {
                                                    reportDatabaseValues.Add(new ArchiveKey
                                                    {
                                                        ProcessingTypeId = 1,
                                                        Key = xPath.Name,
                                                        KeyValueFloat = Double.Parse(value),
                                                        EntityAnalysisModelInstanceEntryGuid =
                                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                                .EntityAnalysisModelInstanceEntryGuid
                                                    });

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                                    }
                                                }

                                                if (xPath.DataTypeId == 6)
                                                {
                                                    Latitude = Double.Parse(value);

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and has set this as the prevailing Latitude.");
                                                    }
                                                }
                                                else
                                                {
                                                    Longitude = Double.Parse(value);

                                                    if (log.IsInfoEnabled)
                                                    {
                                                        log.Info(
                                                            $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and has set this as the prevailing Longitude.");
                                                    }
                                                }

                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (cachePayloadDocumentStore.TryAdd(xPath.Name, value))
                                    {
                                        if (log.IsInfoEnabled)
                                        {
                                            log.Info(
                                                $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} and been typed in the BSON document as DEFAULT string on error as {ex}.");
                                        }

                                        if (xPath.ResponsePayload)
                                        {
                                            cachePayloadDocumentResponse.TryAdd(xPath.Name, value);

                                            if (log.IsInfoEnabled)
                                            {
                                                log.Info(
                                                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} {xPath.Name}XPath Path {xPath.XPath} has extracted value {value} is available to the response payload.");
                                            }
                                        }

                                        if (xPath.ReportTable && !Reprocess)
                                        {
                                            reportDatabaseValues.Add(new ArchiveKey
                                            {
                                                ProcessingTypeId = 1,
                                                Key = xPath.Name,
                                                KeyValueString = value,
                                                EntityAnalysisModelInstanceEntryGuid =
                                                    entityInstanceEntryPayloadStore
                                                        .EntityAnalysisModelInstanceEntryGuid
                                            });

                                            if (log.IsInfoEnabled)
                                            {
                                                log.Info(
                                                    $"Entity Invoke: GUID {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Id} matching XPath has been located and the value {value} has been added to the report payload with the name of {xPath.Name}.");
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} duplication on {xPath.Name}, stepped over.");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InError = true;
                ErrorMessage =
                    "A fatal error has occured in processing.  Please check the logs for more information.";

                log.Error(
                    $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} has yielded an error in the XPath and Model parsing as {ex}.");
            }

            if (!InError)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} activation promotion is set to {entityAnalysisModel.EnableActivationArchive}.");
                }

                CachePayloadDocumentStore = cachePayloadDocumentStore;
                entityInstanceEntryPayloadStore.Payload = cachePayloadDocumentStore;

                CachePayloadDocumentResponse = cachePayloadDocumentResponse;
                ReportDatabaseValues = reportDatabaseValues;
                EntityAnalysisModelInstanceEntryPayloadStore = entityInstanceEntryPayloadStore;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} has instantiated a model invocation object and is launching it.");
                }

                EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.Parse = (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

                if (async)
                {
                    if (pendingEntityInvoke.Count >=
                        Int32.Parse(jubeEnvironment.AppSettings("MaximumModelInvokeAsyncQueue")))
                    {
                        InError = true;
                        ErrorMessage =
                            "Maximum Queue threshold has been reached.  Please wait and retry.";
                    }
                    else
                    {
                        AsyncEnableCallback = jubeEnvironment.AppSettings("EnableCallback")
                            .Equals("True", StringComparison.OrdinalIgnoreCase);
                        pendingEntityInvoke.Enqueue(this);

                        var payloadJsonResponse = new EntityAnalysisModelInstanceEntryPayloadJson();
                        ResponseJson = payloadJsonResponse.BuildJson(EntityAnalysisModelInstanceEntryPayloadStore,
                            entityAnalysisModel.ContractResolver);
                    }
                }
                else
                {
                    await Start().ConfigureAwait(false);
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"HTTP Handler Entity: GUID payload {entityInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {entityAnalysisModel.Id} has finished model invocation.");
                }
            }
        }
        private void StartMeasuresForInvokeThread()
        {
            Stopwatch = new Stopwatch();
            Stopwatch.Start();

            startBytesUsedForInvokeThread = GC.GetAllocatedBytesForCurrentThread();
        }

        public async Task Start()
        {
            try
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} has started invocation timer.  Will now update the reference date.");
                }

                await cacheService.CacheReferenceDate.UpsertReferenceDate(EntityAnalysisModel.TenantRegistryId,
                    EntityAnalysisModel.Guid, EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                        $"has updated reference date for model {EntityAnalysisModel.Id} to {EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate}.");
                }

                var matchedGateway = false;
                double maxGatewayResponseElevation = 0;
                EntityAnalysisModel.ModelInvokeCounter += 1;
                EntityAnalysisModelInstanceEntryPayloadStore.Reprocess = Reprocess;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                        $"has configured startup options.  The model invocation counter is {EntityAnalysisModel.ModelInvokeCounter} and " +
                        $"reprocessing is set to {EntityAnalysisModelInstanceEntryPayloadStore.Reprocess}.  Will now proceed" +
                        " execute inline functions.");
                }

                ExecuteInlineFunctions();
                ExecuteInlineScripts();
                ExecuteGatewayRules(ref maxGatewayResponseElevation, ref matchedGateway);

                var pendingReadTasks = new List<Task<TimedTaskResult>>();
                var pendingWriteTasks = new List<Task<TimedTaskResult>>();

                var activationRuleCount = 0;
                int? prevailingActivationRuleId = null;
                CreateCase createCase = null;
                if (matchedGateway)
                {
                    var cacheTtlCounterRepository = BuildCacheTtlCounterRepository();
                    var cacheTtlCounterEntryRepository = BuildCacheTtlCounterEntryRepository();

                    ExecuteCacheDbStorage(EntityAnalysisModelInstanceEntryPayloadStore,
                        pendingWriteTasks, EntityAnalysisModel.DistinctSearchKeys);

                    pendingReadTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.SanctionsAsync, async () => await ExecuteSanctionsAsync(pendingWriteTasks)));

                    pendingReadTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.DictionaryKvPsAsync, async () => await ExecuteDictionaryKvPsAsync()));

                    pendingReadTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.TtlCountersAsync, async () => await ExecuteTtlCountersAsync(cacheTtlCounterRepository, cacheTtlCounterEntryRepository)));

                    pendingReadTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.AbstractionRulesWithSearchKeysAsync, async () => await ExecuteAbstractionRulesWithSearchKeysAsync(pendingWriteTasks)));

                    await WaitReadTasks(pendingReadTasks).ConfigureAwait(false);

                    ExecuteAbstractionRulesWithoutSearchKeys();
                    ExecuteAbstractionCalculations();
                    ExecuteExhaustiveAdaptation();
                    ExecuteHttpAdaptations();

                    var activationSummary = ExecuteActivations(cacheTtlCounterRepository, cacheTtlCounterEntryRepository,
                        maxGatewayResponseElevation, pendingWriteTasks);

                    createCase = activationSummary.createCase;
                    prevailingActivationRuleId = activationSummary.prevailingActivationRuleId;
                    activationRuleCount = activationSummary.activationRuleCount;
                }

                await WaitWriteTasks(pendingWriteTasks).ConfigureAwait(false);

                CalculateMemoryUsedInInvokeThreadAndUpdateExecutionMeasure();

                await WriteResponseJsonAndQueueAsynchronousResponseMessage();

                ActivationRuleBuildArchivePayload(activationRuleCount, prevailingActivationRuleId, createCase);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} all model invocation processing has completed.");
                }
            }
            catch (Exception ex)
            {
                log.Error(
                    $"Entity Invoke: {EntityAnalysisModel.Id} has created a general error as {ex}.");
            }
            finally
            {
                Finished = true;
            }
        }
        private void CalculateMemoryUsedInInvokeThreadAndUpdateExecutionMeasure()
        {

            EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.Memory = startBytesUsedForInvokeThread - GC.GetAllocatedBytesForCurrentThread();
        }

        private async Task WaitWriteTasks(List<Task<TimedTaskResult>> pendingWriteTasks)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                    $" is waiting for {pendingWriteTasks.Count} write tasks of which {pendingWriteTasks.Count(c => c.IsCompleted)} are completed.");
            }

            await Task.WhenAll(pendingWriteTasks.ToArray()).ConfigureAwait(false);

            EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.WriteTasks = new WriteTasks();
            var pendingReadTasksResults = await Task.WhenAll(pendingWriteTasks).ConfigureAwait(false);

            foreach (var pendingWriteTasksResult in pendingReadTasksResults)
            {
                switch (pendingWriteTasksResult.TaskType)
                {
                    case TaskType.CachePayloadLatestUpsertAsync:
                        EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.WriteTasks.CachePayloadLatestUpsertAsync = new ThreadPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.CachePayloadUpsertAsync:
                        EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.WriteTasks.CachePayloadUpsertAsync = new ThreadPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.CachePayloadInsertAsync:
                        EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.WriteTasks.CachePayloadInsertAsync = new ThreadPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.CachePayloadLatestInsertAsync:
                        EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.WriteTasks.CachePayloadLatestInsertAsync = new ThreadPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.CacheTtlCounterEntryUpsertAsync:
                        EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.WriteTasks.CacheTtlCounterEntryUpsertAsync = new ThreadPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.CacheTtlCounterEntryIncrementAsync:
                        EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.WriteTasks.CacheTtlCounterEntryIncrementAsync = new ThreadPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.CacheSanctionInsertAsync:
                        EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.WriteTasks.CacheSanctionInsertAsync = new ThreadPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.CacheSanctionUpdateAsync:
                        EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.WriteTasks.CacheSanctionUpdateAsync = new ThreadPerformance(pendingWriteTasksResult.ThreadMemory, pendingWriteTasksResult.ComputeTime);
                        break;
                    case TaskType.SanctionsAsync:
                    case TaskType.DictionaryKvPsAsync:
                    case TaskType.TtlCountersAsync:
                    case TaskType.AbstractionRulesWithSearchKeysAsync:
                    case TaskType.OnlineAggregationOfTtlCountersAsync:
                    case TaskType.ExecuteOutOfProcessAggregationOfTtlCountersAsync:
                    case TaskType.ExecuteTimeToLiveCounterIterationAsync:
                    case TaskType.ExecuteAbstractionRulesWithSearchKeyAsync:
                    default:
                        break;
                }
            }

            EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.JoinWriteTasks = (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                    $" completed {pendingWriteTasks.Count} write tasks.");
            }
        }

        private async Task WaitReadTasks(List<Task<TimedTaskResult>> pendingReadTasks)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                    $" is waiting for {pendingReadTasks.Count} read tasks of which {pendingReadTasks.Count(c => c.IsCompleted)} are completed.");
            }

            EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.ReadTasks = new ReadTasks();
            var pendingReadTasksResults = await Task.WhenAll(pendingReadTasks).ConfigureAwait(false);
            foreach (var pendingReadTasksResult in pendingReadTasksResults)
            {
                switch (pendingReadTasksResult.TaskType)
                {
                    case TaskType.SanctionsAsync:
                        EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.ReadTasks.SanctionsAsync = new ThreadPerformance(pendingReadTasksResult.ThreadMemory, pendingReadTasksResult.ComputeTime);
                        break;
                    case TaskType.DictionaryKvPsAsync:
                        EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.ReadTasks.DictionaryKvPsAsync = new ThreadPerformance(pendingReadTasksResult.ThreadMemory, pendingReadTasksResult.ComputeTime);
                        break;
                    case TaskType.TtlCountersAsync:
                        EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.ReadTasks.TtlCountersAsync = new ThreadPerformance(pendingReadTasksResult.ThreadMemory, pendingReadTasksResult.ComputeTime);
                        break;
                    case TaskType.AbstractionRulesWithSearchKeysAsync:
                        EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.ReadTasks.AbstractionRulesWithSearchKeysAsync = new ThreadPerformance(pendingReadTasksResult.ThreadMemory, pendingReadTasksResult.ComputeTime);
                        break;
                    case TaskType.CachePayloadLatestUpsertAsync:
                    case TaskType.CachePayloadUpsertAsync:
                    case TaskType.CachePayloadInsertAsync:
                    case TaskType.CacheTtlCounterEntryUpsertAsync:
                    case TaskType.CacheTtlCounterEntryIncrementAsync:
                    case TaskType.CacheSanctionUpdateAsync:
                    case TaskType.OnlineAggregationOfTtlCountersAsync:
                    case TaskType.ExecuteOutOfProcessAggregationOfTtlCountersAsync:
                    case TaskType.ExecuteTimeToLiveCounterIterationAsync:
                    case TaskType.CachePayloadLatestInsertAsync:
                    case TaskType.CacheSanctionInsertAsync:
                    case TaskType.ExecuteAbstractionRulesWithSearchKeyAsync:
                    default:
                        break;
                }
            }

            EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.JoinReadTasks =
                (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                    $" completed {pendingReadTasks.Count}.");
            }
        }

        private async Task WriteResponseJsonAndQueueAsynchronousResponseMessage()
        {
            if (EntityAnalysisModel.OutputTransform)
            {
                var activationsForTransform = new Dictionary<int, string>();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"HTTP Handler Entity: GUID payload {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {EntityAnalysisModel.Id} has an outbound transformation routine and the transformation will now begin.");
                }

                ResponseJson = EntityAnalysisModel.OutputTransformDelegate(
                    EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.ForeColor,
                    EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.BackColor,
                    EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value,
                    EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Content,
                    EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Redirect,
                    CachePayloadDocumentResponse,
                    EntityInstanceEntryAbstractionResponse,
                    EntityInstanceEntryTtlCountersResponse, activationsForTransform,
                    EntityInstanceEntryAbstractionCalculationResponse, null, log);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"HTTP Handler Entity: GUID payload {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {EntityAnalysisModel.Id} has completed the outbound transformation and the memory stream has {ResponseJson.Length} bytes.");
                }
            }
            else
            {
                var payloadJsonResponse = new EntityAnalysisModelInstanceEntryPayloadJson();
                ResponseJson = payloadJsonResponse.BuildJson(EntityAnalysisModelInstanceEntryPayloadStore,
                    EntityAnalysisModel.ContractResolver);
            }

            if (jubeEnvironment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase) && !Reprocess)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"HTTP Handler Entity: GUID payload {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {EntityAnalysisModel.Id} is about to publish the response to the Outbound Exchange.");
                }

                var props = rabbitMqChannel.CreateBasicProperties();
                props.Headers = new Dictionary<string, object>();

                ResponseJson.Position = 0;
                rabbitMqChannel.BasicPublish("jubeOutbound", "", props, ResponseJson.ToArray());

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"HTTP Handler Entity: GUID payload {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {EntityAnalysisModel.Id} has published the response to the Outbound Exchange.");
                }
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"HTTP Handler Entity: GUID payload {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {EntityAnalysisModel.Id} does not have AMQP configured to dispatch messages to an exchange.");
                }
            }

            if (AsyncEnableCallback)
            {
                var cacheCallbackRepository =
                    new CacheCallbackRepository(jubeEnvironment.AppSettings("ConnectionString"), log);

                await cacheCallbackRepository.InsertAsync(ResponseJson.ToArray(),
                    EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"HTTP Handler Entity: GUID payload {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} model id is {EntityAnalysisModel.Id} will store the callback in the database.");
                }
            }
        }

        private void ExecuteCacheDbStorage(
            EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayload,
            List<Task<TimedTaskResult>> pendingWriteTasks, Dictionary<string, DistinctSearchKey> distinctSearchKeys)
        {
            InsertOrReplaceCacheEntries(entityAnalysisModelInstanceEntryPayload, pendingWriteTasks);
            UpsertCachePayloadLatest(entityAnalysisModelInstanceEntryPayload, pendingWriteTasks,
                distinctSearchKeys);
        }

        private void UpsertCachePayloadLatest(EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayload,
            List<Task<TimedTaskResult>> pendingWriteTasks, Dictionary<string, DistinctSearchKey> distinctSearchKeys)
        {
            foreach (var (key, _) in distinctSearchKeys)
            {
                entityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(key, out var searchKeyValue);

                pendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.CachePayloadLatestUpsertAsync, async () => await cacheService.CachePayloadLatestRepository.UpsertAsync(
                    EntityAnalysisModel.TenantRegistryId,
                    EntityAnalysisModel.Guid,
                    entityAnalysisModelInstanceEntryPayload.ReferenceDate,
                    entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid,
                    key, searchKeyValue.AsString())));
            }
        }

        private void InsertOrReplaceCacheEntries(EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayload,
            List<Task<TimedTaskResult>> pendingWriteTasks)
        {
            if (EntityAnalysisModel.EnableCache)
            {
                if (Reprocess)
                {
                    pendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.CachePayloadUpsertAsync, async () => await cacheService.CachePayloadRepository.UpsertAsync(
                        EntityAnalysisModel.TenantRegistryId,
                        EntityAnalysisModel.Guid,
                        entityAnalysisModelInstanceEntryPayload.Payload,
                        entityAnalysisModelInstanceEntryPayload.ReferenceDate,
                        entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid)));

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has replaced the entity into the cache db serially.");
                    }
                }
                else
                {
                    pendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.CachePayloadInsertAsync, async () => await cacheService.CachePayloadRepository.InsertAsync(
                        EntityAnalysisModel.TenantRegistryId,
                        EntityAnalysisModel.Guid,
                        entityAnalysisModelInstanceEntryPayload.Payload,
                        entityAnalysisModelInstanceEntryPayload.ReferenceDate,
                        entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid)));

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has inserted the entity into the cache db serially.");
                    }
                }
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} does not allow entity storage in the cache.");
                }
            }
        }

        private bool CheckSuppressedResponseElevation()
        {
            if (EntityAnalysisModel.BillingResponseElevationBalanceEntries.Count >
                EntityAnalysisModel.ResponseElevationFrequencyLimitCounter)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has an activation balance of {EntityAnalysisModel.BillingResponseElevationBalanceEntries.Count} and has exceeded threshold.");
                }

                return true;
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has an activation balance of {EntityAnalysisModel.BillingResponseElevationBalanceEntries.Count} and has not exceeded threshold.");
            }

            return false;
        }

        private (int activationRuleCount, int? prevailingActivationRuleId, CreateCase createCase) ExecuteActivations(
            ICacheTtlCounterRepository cacheTtlCounterRepository,
            ICacheTtlCounterEntryRepository cacheTtlCounterEntryRepository,
            double maxGatewayResponseElevation,
            List<Task<TimedTaskResult>> pendingWriteTasks)
        {
            var modelInstanceGuid = EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid;
            var modelId = EntityAnalysisModel.Id;
            var activationRules = EntityAnalysisModel.ModelActivationRules;
            var suppressedActivationRules = new List<string>(activationRules.Count);
            CreateCase createCase = null;
            int? prevailingActivationRuleId = null;
            string prevailingActivationRuleName = null;
            var activationRuleCount = 0;
            double responseElevationHighWaterMark = 0;
            const double responseElevationNotAdjustedHighWaterMark = 0;

            var suppressedModel = ActivationRuleGetSuppressedModel(ref suppressedActivationRules);
            var suppressedResponseElevation = CheckSuppressedResponseElevation();

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId} will now process {activationRules.Count} Activation Rules.");
            }

            var rulesCount = activationRules.Count;
            var instanceActivation = EntityAnalysisModelInstanceEntryPayloadStore.Activation;
            var instanceEntryPayloadStore = EntityAnalysisModelInstanceEntryPayloadStore;
            var entityInstanceEntryActivationResponse = EntityInstanceEntryActivationResponse;
            var reportDatabaseValues = ReportDatabaseValues;

            var activationLock = new object();
            var responseLock = new object();
            var reportLock = new object();
            var countLock = new object();
            var createCaseSet = 0;
            var rand = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

            Parallel.For(0, rulesCount, iActivationRule =>
            {
                var evaluateActivationRule = activationRules[iActivationRule];
                try
                {
                    var suppressed = false;
                    if (suppressedModel || suppressedResponseElevation)
                    {
                        suppressed = true;

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId} activation rule {evaluateActivationRule.Id} is suppressed at the model level or has exceeded response elevation counter at {EntityAnalysisModel.BillingResponseElevationBalanceEntries.Count} or {EntityAnalysisModel.BillingResponseElevationBalance}.");
                        }
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId} activation rule {evaluateActivationRule.Id} is not suppressed at the model level, will test at rule level.");
                        }

                        if (!evaluateActivationRule.EnableReprocessing && Reprocess)
                        {
                            suppressed = true;

                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId} activation rule {evaluateActivationRule.Id} is suppressed at the activation rule level because of reprocessing.");
                            }
                        }
                        else if (suppressedActivationRules is { Count: > 0 })
                        {
                            suppressed = suppressedActivationRules.Contains(evaluateActivationRule.Name);

                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    suppressed
                                        ? $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId} activation rule {evaluateActivationRule.Id} is suppressed at the activation rule level."
                                        : $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId} activation rule {evaluateActivationRule.Id} is not suppressed at the activation rule level.");
                            }
                        }
                    }

                    var activationSample = evaluateActivationRule.ActivationSample >= rand.Value.NextDouble();
                    if (!activationSample)
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId}  has failed in sampling so certain activations will not take place even if there is a match on the activation rule.");
                        }

                        return;
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId}  has passed sampling and is eligible for activation.");
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId}  is starting to test the activation rule match.");
                    }

                    var matched = ReflectRule.Execute(
                        evaluateActivationRule,
                        EntityAnalysisModel,
                        instanceEntryPayloadStore,
                        EntityInstanceEntryDictionaryKvPs,
                        log);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId}  has finished testing the activation rule and it has a matched status of {matched}.");
                    }

                    if (!matched)
                    {
                        return;
                    }

                    lock (activationLock)
                    {
                        if (instanceActivation.ContainsKey(evaluateActivationRule.Name))
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId} and has already added the activation rule {evaluateActivationRule.Id} on {evaluateActivationRule.Name} for processing.");
                            }

                            return;
                        }
                        instanceActivation.Add(
                            evaluateActivationRule.Name,
                            new EntityModelActivationRulePayload
                            {
                                Visible = evaluateActivationRule.Visible
                            });

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId} and has added the activation rule {evaluateActivationRule.Id} flag on {evaluateActivationRule.Name} to the activation buffer for processing.");
                        }
                    }

                    if (evaluateActivationRule.ResponsePayload)
                    {
                        lock (responseLock)
                        {
                            entityInstanceEntryActivationResponse.Add(evaluateActivationRule.Id, evaluateActivationRule);
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId} and has added the activation rule {evaluateActivationRule.Id} flag to the response payload also.");
                        }
                    }

                    if (evaluateActivationRule.ReportTable && !Reprocess)
                    {
                        lock (reportLock)
                        {
                            reportDatabaseValues.Add(new ArchiveKey
                            {
                                ProcessingTypeId = 11,
                                Key = evaluateActivationRule.Name,
                                KeyValueBoolean = 1,
                                EntityAnalysisModelInstanceEntryGuid = modelInstanceGuid
                            });
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId} and has added the activation rule {evaluateActivationRule.Id} flag to the response payload also.");
                        }
                    }

                    lock (countLock)
                    {
                        ActivationRuleGetResponseElevationHighWaterMark(evaluateActivationRule,
                            ref responseElevationHighWaterMark);
                        ActivationRuleResponseElevationHighest(
                            evaluateActivationRule, responseElevationHighWaterMark, suppressed,
                            maxGatewayResponseElevation);
                        ActivationRuleNotification(evaluateActivationRule, suppressed);
                        ActivationRuleCountsAndArchiveHighWatermark(
                            iActivationRule, evaluateActivationRule, suppressed, ref activationRuleCount,
                            ref prevailingActivationRuleId, ref prevailingActivationRuleName);
                        ActivationRuleActivationWatcher(
                            evaluateActivationRule, suppressed, responseElevationNotAdjustedHighWaterMark);
                        if (createCase == null)
                        {
                            if (Interlocked.CompareExchange(ref createCaseSet, 1, 0) == 0)
                            {
                                createCase = ActivationRuleCreateCaseObject(evaluateActivationRule, suppressed);
                            }
                        }
                        ActivationRuleTtlCounter(cacheTtlCounterRepository, cacheTtlCounterEntryRepository,
                            evaluateActivationRule, pendingWriteTasks);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(
                        $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId} error in TTL Counter processing as {ex} .");
                }
            });

            instanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.ExecuteActivation = (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {modelInstanceGuid} and model {modelId} has added the response elevation for use in bidding against other models if called by model inheritance.");
            }

            ActivationRuleFinishResponseElevation(instanceEntryPayloadStore.ResponseElevation.Value);
            ActivationRuleResponseElevationAddToCounters();

            return (activationRuleCount, prevailingActivationRuleId, createCase);
        }

        private void ActivationRuleResponseElevationAddToCounters()
        {
            if (!(EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value > 0))
            {
                return;
            }

            EntityAnalysisModel.BillingResponseElevationCount += 1;
            EntityAnalysisModel.BillingResponseElevationJournal.Enqueue(DateTime.Now);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} the response elevation is greater than 0 and has incremented counters for throttling.  The Billing Response Elevation Count is {EntityAnalysisModel.BillingResponseElevationCount}.");
            }
        }

        private void ActivationRuleBuildArchivePayload(int activationRuleCount, int? prevailingActivationRuleId,
            CreateCase createCase)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has been selected for sampling or case creation is been specified. Is building the XML payload from the payload created.");
            }

            EntityAnalysisModelInstanceEntryPayloadStore.TenantRegistryId = EntityAnalysisModel.TenantRegistryId;
            EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelName = EntityAnalysisModel.Name;
            EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelActivationRuleCount = activationRuleCount;
            EntityAnalysisModelInstanceEntryPayloadStore.PrevailingEntityAnalysisModelActivationRuleId =
                prevailingActivationRuleId;
            EntityAnalysisModelInstanceEntryPayloadStore.ReportDatabaseValue = ReportDatabaseValues;
            EntityAnalysisModelInstanceEntryPayloadStore.Payload = CachePayloadDocumentStore;
            EntityAnalysisModelInstanceEntryPayloadStore.ArchiveEnqueueDate = DateTime.Now;
            EntityAnalysisModelInstanceEntryPayloadStore.CreateCasePayload = createCase;
            EntityAnalysisModelInstanceEntryPayloadStore.StoreInRdbms = EntityAnalysisModel.EnableRdbmsArchive;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} a payload has been created for archive.");
            }

            var localStopwatch = new Stopwatch();
            localStopwatch.Start();

            if (Reprocess)
            {
                EntityAnalysisModel.CaseCreationAndArchiver(EntityAnalysisModelInstanceEntryPayloadStore,
                    null);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} a payload has been added for archive synchronously as it is set for reprocessing.");
                }
            }
            else
            {
                EntityAnalysisModel.PersistToDatabaseAsync.Enqueue(EntityAnalysisModelInstanceEntryPayloadStore);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} a payload has been added for archive asynchronously.");
                }
            }
        }

        private void ActivationRuleFinishResponseElevation(double responseElevation)
        {
            if (!(responseElevation > 0))
            {
                return;
            }

            EntityAnalysisModel.ModelResponseElevationCounter += 1;
            EntityAnalysisModel.ModelResponseElevationSum += responseElevation;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} response elevation is greater than zero and has incremented Response Elevation Counter which has a value of {EntityAnalysisModel.ModelResponseElevationCounter} and Model Response Elevation Sum which has a value of {EntityAnalysisModel.ModelResponseElevationSum}.");
            }
        }

        private void ActivationRuleTtlCounter(ICacheTtlCounterRepository cacheTtlCounterRepository,
            ICacheTtlCounterEntryRepository cacheTtlCounterEntryRepository,
            EntityAnalysisModelActivationRule evaluateActivationRule,
            List<Task<TimedTaskResult>> pendingWriteTasks)
        {
            if (!evaluateActivationRule.EnableTtlCounter || Reprocess)
            {
                return;
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is incrementing TTL counter {evaluateActivationRule.EntityAnalysisModelTtlCounterGuid} as this is enabled in the activation rule.");
            }

            var found = false;
            foreach (var (_, value) in
                     from targetTtlCounterModelKvp in Models
                     where evaluateActivationRule.EntityAnalysisModelGuidTtlCounter ==
                           targetTtlCounterModelKvp.Value.Guid
                     select targetTtlCounterModelKvp)
            {
                foreach (var foundTtlCounter in value.ModelTtlCounters)
                {
                    if (evaluateActivationRule.EntityAnalysisModelTtlCounterGuid == foundTtlCounter.Guid)
                    {
                        try
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has matched the name in the activation rule to the TTL counters loaded for {EntityAnalysisModel.Name} in model id {value.Id}.");
                            }

                            if (CachePayloadDocumentStore.ContainsKey(foundTtlCounter.TtlCounterDataName))
                            {
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} found a value a value for TTL counter name {foundTtlCounter.Name} as {CachePayloadDocumentStore[foundTtlCounter.TtlCounterDataName]}.");
                                }

                                if (value.EnableTtlCounter)
                                {
                                    if (evaluateActivationRule.EntityAnalysisModelGuidTtlCounter == value.Guid)
                                    {
                                        if (log.IsInfoEnabled)
                                        {
                                            log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {CachePayloadDocumentStore[foundTtlCounter.TtlCounterDataName]}.  Is about to insert the entry.");
                                        }

                                        if (!foundTtlCounter.EnableLiveForever)
                                        {
                                            var resolution = EntityAnalysisModelInstanceEntryPayloadStore
                                                .ReferenceDate.Floor(TimeSpan.FromMinutes(1));

                                            pendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.CacheTtlCounterEntryUpsertAsync, async () => await cacheTtlCounterEntryRepository.UpsertAsync(
                                                EntityAnalysisModel.TenantRegistryId, EntityAnalysisModel.Guid,
                                                foundTtlCounter.TtlCounterDataName,
                                                CachePayloadDocumentStore[foundTtlCounter.TtlCounterDataName]
                                                    .AsString(),
                                                foundTtlCounter.Guid,
                                                resolution, 1)));
                                        }
                                        else
                                        {
                                            if (log.IsInfoEnabled)
                                            {
                                                log.Info(
                                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {CachePayloadDocumentStore[foundTtlCounter.TtlCounterDataName]} is set to live forever so no entry has been made to wind back counters.");
                                            }
                                        }

                                        pendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.CacheTtlCounterEntryIncrementAsync, async () => await cacheTtlCounterRepository
                                            .IncrementTtlCounterCacheAsync(EntityAnalysisModel.TenantRegistryId,
                                                EntityAnalysisModel.Guid,
                                                foundTtlCounter.TtlCounterDataName,
                                                CachePayloadDocumentStore[foundTtlCounter.TtlCounterDataName]
                                                    .AsString(),
                                                foundTtlCounter.Guid, 1,
                                                EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate
                                            )));
                                    }
                                }
                                else
                                {
                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} cannot create a TTL counter for name {value.Name} as TTL Counter Storage is disabled for the model id {value.Id}.");
                                    }
                                }
                            }
                            else
                            {
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} could not find a value for TTL counter name {foundTtlCounter.Name}.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} error performing insertion on match for a TTL Counter by name of {foundTtlCounter.Name} and id of {foundTtlCounter.Id} with exception message of {ex.Message}.");
                            }
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has matched the name in the activation rule to the TTL counters loaded for {EntityAnalysisModel.Name} and has finished processing.");
                        }

                        found = true;
                    }

                    if (found)
                    {
                        break;
                    }
                }

                if (found)
                {
                    break;
                }
            }
        }

        private ICacheTtlCounterRepository BuildCacheTtlCounterRepository()
        {
            ICacheTtlCounterRepository cacheTtlCounterRepository = new CacheTtlCounterRepository(
                cacheService.RedisDatabase, log);
            return cacheTtlCounterRepository;
        }

        private void ActivationRuleResponseElevationHighest(
            EntityAnalysisModelActivationRule evaluateActivationRule, double responseElevationHighWaterMark,
            bool suppressed, double maxGatewayResponseElevation)
        {
            if (!(responseElevationHighWaterMark >
                  EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value) ||
                suppressed || evaluateActivationRule.EnableResponseElevation)
            {
                return;
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} response elevation for activation rule {evaluateActivationRule.Id} is the current largest Response Elevation {EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation} but is less than the new value of {responseElevationHighWaterMark} so it will be elevated.  Some integrity checks will also be performed.");
            }

            if (responseElevationHighWaterMark > EntityAnalysisModel.MaxResponseElevation)
            {
                EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value =
                    EntityAnalysisModel.MaxResponseElevation;
                EntityAnalysisModel.ResponseElevationValueLimitCounter += 1;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} the response elevation exceeds the maximum allowed in the model of {EntityAnalysisModel.MaxResponseElevation}, so has been truncated to {EntityAnalysisModel.MaxResponseElevation} and the Response Elevation Value Limit Counter incremented.");
                }
            }
            else
            {
                if (responseElevationHighWaterMark > maxGatewayResponseElevation)
                {
                    EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value =
                        maxGatewayResponseElevation;
                    EntityAnalysisModel.ResponseElevationValueGatewayLimitCounter += 1;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} the response elevation exceeds the maximum allowed in the gateway rule of {maxGatewayResponseElevation}, so has been truncated to {maxGatewayResponseElevation} and the Response Elevation Value Gateway Limit counter incremented.");
                    }
                }
                else
                {
                    EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value =
                        responseElevationHighWaterMark;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} the response elevation has tested the limits and the response elevation is being carried forward as {responseElevationHighWaterMark}.");
                    }
                }
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} response elevation for activation rule {evaluateActivationRule.Id} is being tested against the current limits and cap to zero if exceeded.");
            }

            EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Content =
                evaluateActivationRule.ResponseElevationContent;
            EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Redirect =
                evaluateActivationRule.ResponseElevationRedirect;
            EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.BackColor =
                evaluateActivationRule.ResponseElevationBackColor;
            EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.ForeColor =
                evaluateActivationRule.ResponseElevationForeColor;
            EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Content =
                evaluateActivationRule.ResponseElevationContent;
            EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Redirect =
                evaluateActivationRule.ResponseElevationRedirect;
            EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.ForeColor =
                evaluateActivationRule.ResponseElevationForeColor;
            EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.BackColor =
                evaluateActivationRule.ResponseElevationBackColor;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} response elevation for activation rule {evaluateActivationRule.Id} updated the response elevation to {EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation}.");
            }

            if (EntityAnalysisModel.EnableResponseElevationLimit)
            {
                EntityAnalysisModel.BillingResponseElevationBalanceEntries.Enqueue(new ResponseElevation
                {
                    CreatedDate = DateTime.Now,
                    Value = EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation.Value
                });

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has noted the response elevation date on the counter queue. There are {EntityAnalysisModel.ActivationWatcherCountJournal.Count} in queue.");
                }
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} does not have response elevation limit enabled,  so has not noted the response elevation date.");
                }
            }
        }

        private void ActivationRuleNotification(EntityAnalysisModelActivationRule evaluateActivationRule,
            bool suppressed)
        {
            if (jubeEnvironment.AppSettings("EnableNotification").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (suppressed || !evaluateActivationRule.EnableNotification || Reprocess)
                {
                    return;
                }

                var notification = new Notification
                {
                    NotificationBody = ReplaceTokens(evaluateActivationRule.NotificationBody),
                    NotificationDestination = ReplaceTokens(evaluateActivationRule.NotificationDestination),
                    NotificationSubject = ReplaceTokens(evaluateActivationRule.NotificationSubject),
                    NotificationTypeId = evaluateActivationRule.NotificationTypeId
                };

                if (jubeEnvironment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    var jsonString = JsonConvert.SerializeObject(notification);
                    var bodyBytes = Encoding.UTF8.GetBytes(jsonString);
                    rabbitMqChannel.BasicPublish("", "jubeNotifications", null, bodyBytes);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has sent a message to the notification dispatcher as {jsonString}.");
                    }
                }
                else
                {
                    pendingNotification.Enqueue(notification);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has not sent a message to the internal notification dispatcher because AMQP is not enabled.");
                    }
                }
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has not sent a message as notification disabled.");
                }
            }
        }

        private string ReplaceTokens(string message)
        {
            var notificationTokenizationList = NotificationTokenization.ReturnTokens(message);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has found {notificationTokenizationList.Count} tokens in message {message}.");
            }

            foreach (var notificationToken in notificationTokenizationList)
            {
                var notificationTokenValue = "";
                if (CachePayloadDocumentStore.TryGetValue(notificationToken, out var valuePayload))
                {
                    notificationTokenValue = valuePayload.ToString();
                }
                else if (EntityAnalysisModelInstanceEntryPayloadStore.Abstraction.TryGetValue(notificationToken,
                             out var valueAbstraction))
                {
                    notificationTokenValue = valueAbstraction.ToString(CultureInfo.InvariantCulture);
                }
                else if (EntityAnalysisModelInstanceEntryPayloadStore.TtlCounter.TryGetValue(notificationToken,
                             out var valueTtlCounter))
                {
                    notificationTokenValue = valueTtlCounter.ToString();
                }
                else if (
                    EntityAnalysisModelInstanceEntryPayloadStore.AbstractionCalculation.TryGetValue(notificationToken,
                        out var valueAbstractionCalculation))
                {
                    notificationTokenValue = valueAbstractionCalculation
                        .ToString(CultureInfo.InvariantCulture);
                }

                var notificationReplaceToken = $"[@{notificationToken}@]";
                message = message.Replace(notificationReplaceToken, notificationTokenValue);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has finalized notification message {message}.");
                }
            }

            return message;
        }

        private void ActivationRuleGetResponseElevationHighWaterMark(
            EntityAnalysisModelActivationRule evaluateActivationRule,
            ref double responseElevationHighWaterMark)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                    $"and model {EntityAnalysisModel.Id} will begin processing of response elevation for activation " +
                    $"rule {evaluateActivationRule.Id}. " +
                    $"Current high water mark on response elevation is {responseElevationHighWaterMark}.");
            }

            responseElevationHighWaterMark = evaluateActivationRule.ResponseElevation;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} response elevation for activation rule {evaluateActivationRule.Id} is the current largest Response Elevation {EntityAnalysisModelInstanceEntryPayloadStore.ResponseElevation} and will be tested against the new one of {responseElevationHighWaterMark} .");
            }
        }

        private bool ActivationRuleGetSuppressedModel(ref List<string> suppressedActivationRules)
        {
            var suppressedModelValue = false;
            foreach (var xpath in EntityAnalysisModel.EntityAnalysisModelRequestXPaths.Where(w => w.EnableSuppression)
                         .ToList())
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Suppression key is {xpath.Name}.  Will now check to see if has a suppressed value.");
                }

                if (CachePayloadDocumentStore.ContainsKey(xpath.Name))
                {
                    if (EntityAnalysisModel.EntityAnalysisModelSuppressionModels.ContainsKey(xpath.Name))
                    {
                        suppressedModelValue =
                            EntityAnalysisModel.EntityAnalysisModelSuppressionModels[xpath.Name].Contains(
                                CachePayloadDocumentStore[xpath.Name].AsString());
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Suppression key is {xpath.Name} but it has no keys.");
                        }
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Suppression status is {suppressedModelValue}.");
                    }

                    if (EntityAnalysisModel.EntityAnalysisModelSuppressionRules.ContainsKey(xpath.Name))
                    {
                        if (EntityAnalysisModel.EntityAnalysisModelSuppressionRules[xpath.Name].ContainsKey(
                                CachePayloadDocumentStore[xpath.Name].AsString()))
                        {
                            suppressedActivationRules =
                                EntityAnalysisModel.EntityAnalysisModelSuppressionRules[xpath.Name][
                                    CachePayloadDocumentStore[xpath.Name].AsString()];

                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Suppression status is {suppressedModelValue}.");
                            }
                        }
                        else
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Suppression status is {suppressedModelValue}.");
                            }
                        }
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Suppression key is {xpath.Name} but it has no keys.");
                        }
                    }
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Suppression key is {xpath.Name} but could not locate the value in the data payload.");
                    }
                }
            }

            return suppressedModelValue;
        }

        private CreateCase ActivationRuleCreateCaseObject(EntityAnalysisModelActivationRule evaluateActivationRule,
            bool suppressed)
        {
            if (!evaluateActivationRule.EnableCaseWorkflow || suppressed)
            {
                return null;
            }

            var createCase = new CreateCase
            {
                CaseEntryGuid = EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid,
                CaseWorkflowGuid = evaluateActivationRule.CaseWorkflowGuid,
                CaseWorkflowStatusGuid = evaluateActivationRule.CaseWorkflowStatusGuid
            };

            if (evaluateActivationRule.BypassSuspendSample > Seeded.NextDouble())
            {
                createCase.SuspendBypass = true;
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has been selected for bypass.");
                }

                switch (evaluateActivationRule.BypassSuspendInterval)
                {
                    case 'n':
                        createCase.SuspendBypassDate =
                            DateTime.Now.AddMinutes(evaluateActivationRule.BypassSuspendValue);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has a bypass interval of n to create a date of {createCase.SuspendBypassDate}.");
                        }

                        break;
                    case 'h':
                        createCase.SuspendBypassDate =
                            DateTime.Now.AddHours(evaluateActivationRule.BypassSuspendValue);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has a bypass interval of h to create a date of {createCase.SuspendBypassDate}.");
                        }

                        break;
                    case 'd':
                        createCase.SuspendBypassDate =
                            DateTime.Now.AddDays(evaluateActivationRule.BypassSuspendValue);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has a bypass interval of d to create a date of {createCase.SuspendBypassDate}.");
                        }

                        break;
                    case 'm':
                        createCase.SuspendBypassDate =
                            DateTime.Now.AddMonths(evaluateActivationRule.BypassSuspendValue);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has a bypass interval of m to create a date of {createCase.SuspendBypassDate}.");
                        }

                        break;
                }
            }
            else
            {
                createCase.SuspendBypass = false;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} has been selected for open.");
                }

                createCase.SuspendBypassDate = DateTime.Now;
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    String.IsNullOrEmpty(evaluateActivationRule.CaseKey)
                        ? $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} which is an entry foreign key."
                        : $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} which is not a entry foreign key.");
            }

            if (evaluateActivationRule.CaseKey != null &&
                CachePayloadDocumentStore.ContainsKey(evaluateActivationRule.CaseKey))
            {
                createCase.CaseKey = evaluateActivationRule.CaseKey;
                createCase.CaseKeyValue = CachePayloadDocumentStore[evaluateActivationRule.CaseKey].ToString();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} and case key value is {CachePayloadDocumentStore[evaluateActivationRule.CaseKey]}.");
                }
            }
            else
            {
                createCase.CaseKeyValue = EntityAnalysisModelInstanceEntryPayloadStore.EntityInstanceEntryId;
                createCase.CaseKey = null;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and case key is {evaluateActivationRule.CaseKey} does not have a value,  has fallen back to the entity id of {EntityAnalysisModelInstanceEntryPayloadStore.EntityInstanceEntryId}.");
                }
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has flagged that a case needs to be created for case workflow id {createCase.CaseWorkflowGuid} and case status id {createCase.CaseWorkflowStatusGuid}.  The case will be queued later after the archive XML has been created.");
            }

            return createCase;
        }

        private void ActivationRuleActivationWatcher(EntityAnalysisModelActivationRule evaluateActivationRule,
            bool suppressed, double responseElevationNotAdjustedHighWaterMark)
        {
            if (!evaluateActivationRule.SendToActivationWatcher || suppressed || Reprocess ||
                !EntityAnalysisModel.EnableActivationWatcher)
            {
                return;
            }

            try
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} the current activation watch count is {EntityAnalysisModel.ActivationWatcherCount} which will be tested against the threshold {EntityAnalysisModel.MaxActivationWatcherThreshold}.");
                }

                if (!(EntityAnalysisModel.ActivationWatcherCount <
                      EntityAnalysisModel.MaxActivationWatcherThreshold) ||
                    !(EntityAnalysisModel.ActivationWatcherSample >= Seeded.NextDouble()))
                {
                    return;
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} the current activation watch count is {EntityAnalysisModel.ActivationWatcherCount} which will be tested against the threshold {EntityAnalysisModel.MaxActivationWatcherThreshold} and selected via random sampling.");
                }

                var activationWatcher = new ActivationWatcher
                {
                    BackColor = evaluateActivationRule.ResponseElevationBackColor,
                    ForeColor = evaluateActivationRule.ResponseElevationForeColor,
                    ResponseElevation = responseElevationNotAdjustedHighWaterMark,
                    ResponseElevationContent = evaluateActivationRule.ResponseElevationContent,
                    ActivationRuleSummary = evaluateActivationRule.Name,
                    TenantRegistryId = EntityAnalysisModel.TenantRegistryId,
                    CreatedDate = DateTime.Now,
                    Longitude = Longitude,
                    Latitude = Latitude,
                    Key = "",
                    KeyValue = ""
                };

                if (CachePayloadDocumentStore.ContainsKey(evaluateActivationRule
                        .ResponseElevationKey))
                {
                    activationWatcher.Key = evaluateActivationRule.ResponseElevationKey;
                    activationWatcher.KeyValue =
                        CachePayloadDocumentStore[evaluateActivationRule
                            .ResponseElevationKey].AsString();

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} found key of {activationWatcher.Key} and key value of {activationWatcher.KeyValue}.");
                    }
                }
                else
                {
                    activationWatcher.Key = evaluateActivationRule.ResponseElevationKey;
                    activationWatcher.KeyValue = "Missing";

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} fallen back to key of {activationWatcher.Key} and key value of {activationWatcher.KeyValue}.");
                    }
                }

                var jsonString = JsonConvert.SerializeObject(activationWatcher, new JsonSerializerSettings
                {
                    ContractResolver = EntityAnalysisModel.ContractResolver
                });

                var bodyBytes = Encoding.UTF8.GetBytes(jsonString);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has serialized the Activation Watcher Object to be dispatched.");
                }

                if (jubeEnvironment.AppSettings("ActivationWatcherAllowPersist")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    EntityAnalysisModel.PersistToActivationWatcherAsync.Enqueue(activationWatcher);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} replay is  allowed so it has been sent to the database. {EntityAnalysisModel.ActivationWatcherCount}.");
                    }
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} replay is not allowed so it has not been sent to the database. {EntityAnalysisModel.ActivationWatcherCount}.");
                    }
                }

                if (jubeEnvironment.AppSettings("StreamingActivationWatcher")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    var messaging = new Messaging(jubeEnvironment.AppSettings("ConnectionString"), log);

                    messaging.SendActivation(bodyBytes);

                    EntityAnalysisModel.ActivationWatcherCount += 1;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} streaming is allowed so it has been sent to the database as a notification in the activation channel. {EntityAnalysisModel.ActivationWatcherCount}.");
                    }
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} streaming is not allowed so it has not been sent to the database as a notification in the activation channel. {EntityAnalysisModel.ActivationWatcherCount}.");
                    }
                }

                if (jubeEnvironment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    var properties = rabbitMqChannel.CreateBasicProperties();

                    rabbitMqChannel.BasicPublish("jubeActivations", "", properties, bodyBytes);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} AMQP is  allowed so it has been published to the RabbitMQ.");
                    }
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} AMQP is not allowed, so publish has been stepped over.");
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has sent a message to the watcher as {jsonString} the activation watcher counter has been incremented and is currently {EntityAnalysisModel.ActivationWatcherCount}.");
                }
            }
            catch (Exception ex)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} there has been an error in Activation Watcher processing {ex}.");
                }
            }
        }

        private void ActivationRuleCountsAndArchiveHighWatermark(int iActivationRule,
            EntityAnalysisModelActivationRule evaluateActivationRule, bool suppressed,
            ref int activationRuleCount, ref int? prevailingActivationRuleId, ref string prevailingActivationRuleName)
        {
            if (suppressed)
            {
                return;
            }

            EntityAnalysisModel.ModelActivationRules[iActivationRule].Counter += 1;

            if (EntityAnalysisModel.ModelActivationRules[iActivationRule].Visible)
            {
                prevailingActivationRuleId = EntityAnalysisModel.ModelActivationRules[iActivationRule].Id;
                prevailingActivationRuleName = EntityAnalysisModel.ModelActivationRules[iActivationRule].Name;

                activationRuleCount += 1;
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} response elevation for activation rule {evaluateActivationRule.Id} has not been included in the local count as the rule is not set to visible{prevailingActivationRuleId}.  This activation rule count is {activationRuleCount}.");
                }
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} response elevation for activation rule {evaluateActivationRule.Id} activation counter has been incremented and the prevailing activation rule has been set to {prevailingActivationRuleId}.  This activation rule count is {activationRuleCount}.");
            }
        }

        private async Task ExecuteSanctionsAsync(List<Task<TimedTaskResult>> pendingWriteTasks)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is starting Sanctions processing.");
            }

            double sumLevenshteinDistance = 0;
            foreach (var entityAnalysisModelSanction in EntityAnalysisModel.EntityAnalysisModelSanctions)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name}.");
                }

                try
                {
                    if (CachePayloadDocumentStore.ContainsKey(entityAnalysisModelSanction.MultipartStringDataName))
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is about to look for Sanctions Match in the Cache.");
                        }

                        if (CachePayloadDocumentStore[entityAnalysisModelSanction.MultipartStringDataName].AsString() ==
                            null)
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has an entry but is a null value.");
                            }
                        }
                        else
                        {
                            var multiPartStringValue = CachePayloadDocumentStore
                                [entityAnalysisModelSanction.MultipartStringDataName].AsString();

                            var sanction = await cacheService.CacheSanctionRepository
                                .GetByMultiPartStringDistanceThresholdAsync(
                                    EntityAnalysisModel.TenantRegistryId,
                                    EntityAnalysisModel.Guid, multiPartStringValue,
                                    entityAnalysisModelSanction.Distance
                                ).ConfigureAwait(false);

                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} and has found sanction as {sanction != null}.");
                            }

                            var foundCacheSanctions = false;
                            if (sanction != null)
                            {
                                var deleteLineCacheKeys = sanction.CreatedDate;

                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} has a cache interval of {entityAnalysisModelSanction.CacheInterval} and value of {entityAnalysisModelSanction.CacheValue}.");
                                }

                                deleteLineCacheKeys = entityAnalysisModelSanction.CacheInterval switch
                                {
                                    's' => deleteLineCacheKeys.AddSeconds(
                                        entityAnalysisModelSanction.CacheValue),
                                    'n' => deleteLineCacheKeys.AddMinutes(
                                        entityAnalysisModelSanction.CacheValue),
                                    'h' => deleteLineCacheKeys.AddHours(entityAnalysisModelSanction.CacheValue),
                                    _ => deleteLineCacheKeys.AddDays(entityAnalysisModelSanction.CacheValue)
                                };

                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} has an expiry date of {deleteLineCacheKeys}");
                                }

                                if (deleteLineCacheKeys <= DateTime.Now)
                                {
                                    foundCacheSanctions = false;

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} cache is not available because of expiration.");
                                    }
                                }
                                else
                                {
                                    foundCacheSanctions = true;

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} cache is available.");
                                    }
                                }
                            }
                            else
                            {
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} cache is not available.");
                                }
                            }

                            if (foundCacheSanctions)
                            {
                                if (!EntityInstanceEntrySanctions.ContainsKey(entityAnalysisModelSanction.Name))
                                {
                                    if (sanction.Value.HasValue)
                                    {
                                        EntityInstanceEntrySanctions.Add(entityAnalysisModelSanction.Name,
                                            sanction.Value.Value);

                                        if (log.IsInfoEnabled)
                                        {
                                            log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} is adding cache value of {sanction.Value} to processing. Reprocessing will not take place.");
                                        }

                                        if (entityAnalysisModelSanction.ResponsePayload)
                                        {
                                            EntityInstanceEntrySanctionsResponse.Add(entityAnalysisModelSanction.Name,
                                                sanction.Value.Value);

                                            if (log.IsInfoEnabled)
                                            {
                                                log.Info(
                                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has added the average of {sanction.Value} to the response payload.");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has extracted multi part string name value as {multiPartStringValue} is not adding cache value of {sanction.Value} to processing as is a duplicate. Reprocessing will not take place.");
                                    }
                                }
                            }
                            else
                            {
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and is about to execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance}.");
                                }

                                var sanctionEntryReturns = LevenshteinDistance.CheckMultipartString(
                                    multiPartStringValue,
                                    entityAnalysisModelSanction.Distance, EntityAnalysisModel.SanctionsEntries);

                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} and found {sanctionEntryReturns.Count} matches.");
                                }

                                double? averageLevenshteinDistance = null;
                                if (sanctionEntryReturns.Count == 0)
                                {
                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} found no matches average distance set to 5.");
                                    }
                                }
                                else
                                {
                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} is about to calculate the average.");
                                    }

                                    sumLevenshteinDistance = sanctionEntryReturns.Aggregate(sumLevenshteinDistance,
                                        (current, sanctionEntryReturn) =>
                                            current + sanctionEntryReturn.LevenshteinDistance);

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a sum of {sumLevenshteinDistance}.");
                                    }

                                    if (!(sumLevenshteinDistance == 0 | Double.IsNaN(sumLevenshteinDistance)))
                                    {
                                        averageLevenshteinDistance =
                                            sumLevenshteinDistance / sanctionEntryReturns.Count;

                                        if (log.IsInfoEnabled)
                                        {
                                            log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a sum of {sumLevenshteinDistance} and calculated average as {averageLevenshteinDistance ?? null}.");
                                        }
                                    }
                                    else
                                    {
                                        averageLevenshteinDistance = 0;

                                        if (log.IsInfoEnabled)
                                        {
                                            log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a sum of {sumLevenshteinDistance} but is an invalid number.");
                                        }
                                    }

                                    if (!EntityInstanceEntrySanctions.ContainsKey(entityAnalysisModelSanction.Name))
                                    {
                                        if (averageLevenshteinDistance.HasValue)
                                        {
                                            EntityInstanceEntrySanctions.Add(entityAnalysisModelSanction.Name,
                                                averageLevenshteinDistance.Value);

                                            if (log.IsInfoEnabled)
                                            {
                                                log.Info(
                                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has added the average of {averageLevenshteinDistance ?? null} to the payload.");
                                            }

                                            if (entityAnalysisModelSanction.ResponsePayload)
                                            {
                                                EntityInstanceEntrySanctionsResponse.Add(
                                                    entityAnalysisModelSanction.Name,
                                                    averageLevenshteinDistance.Value);

                                                if (log.IsInfoEnabled)
                                                {
                                                    log.Info(
                                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has added the average of {averageLevenshteinDistance ?? null} to the response payload.");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (log.IsInfoEnabled)
                                        {
                                            log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has added the average of {averageLevenshteinDistance ?? null} but has not been added to payload as it is a duplicate.");
                                        }
                                    }
                                }

                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has constructed a cache payload as Distance of {averageLevenshteinDistance ?? null}, MultiPartString of {multiPartStringValue} and a created date of now.");
                                }

                                if (sanction == null)
                                {
                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is about to insert cache payload.");
                                    }

                                    pendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.CacheSanctionInsertAsync, async () => await cacheService.CacheSanctionRepository.InsertAsync(
                                        EntityAnalysisModel.TenantRegistryId,
                                        EntityAnalysisModel.Guid,
                                        multiPartStringValue,
                                        entityAnalysisModelSanction.Distance, averageLevenshteinDistance)));

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has inserted cache payload.");
                                    }
                                }
                                else
                                {
                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is about to update cache payload " +
                                            $"for Multi Part String Value {multiPartStringValue} and distance {entityAnalysisModelSanction.Distance}.");
                                    }

                                    pendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.CacheSanctionUpdateAsync, async () => await cacheService.CacheSanctionRepository.UpdateAsync(
                                        EntityAnalysisModel.TenantRegistryId,
                                        EntityAnalysisModel.Guid,
                                        multiPartStringValue,
                                        entityAnalysisModelSanction.Distance,
                                        averageLevenshteinDistance)));

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has updated cache payload.");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} but could not find it in the payload.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} has seen an error in sanctions checking as {ex}.");
                }
            }

            EntityAnalysisModelInstanceEntryPayloadStore.Sanction = EntityInstanceEntrySanctions;

            EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.SanctionsAsync =
                (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has finished sanctions processing.");
            }
        }

        private void ExecuteHttpAdaptations()
        {
            var payloadStore = EntityAnalysisModelInstanceEntryPayloadStore;
            var model = EntityAnalysisModel;
            var modelGuid = payloadStore.EntityAnalysisModelInstanceEntryGuid;
            var modelId = model.Id;
            var abstractions = payloadStore.Abstraction;
            var ttlCounters = payloadStore.TtlCounter;
            var abstractionCalcs = payloadStore.AbstractionCalculation;
            var httpAdaptation = payloadStore.HttpAdaptation;

            if (log.IsInfoEnabled)
            {
                log.Info($"Entity Invoke: GUID {modelGuid} and model {modelId} will begin processing adaptations.");
            }

            foreach (var (adaptationKey, modelAdaptation) in model.EntityAnalysisModelAdaptations)
            {
                try
                {
                    var totalKeys = abstractions.Count + ttlCounters.Count + abstractionCalcs.Count;
                    var jsonForPlumber = new Dictionary<string, object>(totalKeys);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelGuid} and model {modelId} is evaluating {adaptationKey} has finished allocating the Data collection for R Plumber POST.");
                    }

                    foreach (var kvp in abstractions)
                    {
                        jsonForPlumber[kvp.Key] = kvp.Value;
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelGuid} and model {modelId} is evaluating {adaptationKey} has finished allocating the Abstractions for R Plumber POST.");
                    }

                    foreach (var kvp in ttlCounters)
                    {
                        if (!jsonForPlumber.ContainsKey(kvp.Key))
                        {
                            jsonForPlumber[kvp.Key] = kvp.Value;
                        }
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelGuid} and model {modelId} is evaluating {adaptationKey} has finished allocating the TTL Counters for R Plumber POST.");
                    }

                    foreach (var kvp in abstractionCalcs)
                    {
                        if (!jsonForPlumber.ContainsKey(kvp.Key))
                        {
                            jsonForPlumber[kvp.Key] = kvp.Value;
                        }
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelGuid} and model {modelId} is evaluating {adaptationKey} has finished allocating the Abstraction Calculations for R Plumber POST.");
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelGuid} and model {modelId} is evaluating {adaptationKey} has finished allocating and created JSON for R Plumber:{jsonForPlumber}.");
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelGuid} and model {modelId} is evaluating {adaptationKey} is about to post to R Plumber.");
                    }

                    var adaptationSimulation = modelAdaptation.Post(jsonForPlumber, log);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelGuid} and model {modelId} is evaluating {adaptationKey} has called R Plumber with a response of {adaptationSimulation}.");
                    }

                    httpAdaptation[modelAdaptation.Name] = adaptationSimulation.Result;

                    if (modelAdaptation.ResponsePayload)
                    {
                        var simulations = new Dictionary<string, double>
                        {
                            {
                                "1", adaptationSimulation.Result
                            }
                        };
                        EntityInstanceEntryAdaptationResponses[modelAdaptation.Name] = simulations;

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {modelGuid} and model {modelId} is evaluating {adaptationKey} has called R Plumber with a response of {adaptationSimulation} and added it to the response payload.");
                        }
                    }

                    if (!modelAdaptation.ReportTable || Reprocess)
                    {
                        continue;
                    }
                    
                    ReportDatabaseValues.Add(new ArchiveKey
                    {
                        ProcessingTypeId = 9,
                        Key = modelAdaptation.Name,
                        KeyValueFloat = adaptationSimulation.Result,
                        EntityAnalysisModelInstanceEntryGuid = modelGuid
                    });

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelGuid} and model {modelId} is evaluating {adaptationKey} has called R Plumber with a response of {adaptationSimulation} and has added it to the SQL report payload.");
                    }
                }
                catch (Exception ex)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelGuid} and model {modelId} is evaluating {adaptationKey} produced an error {ex}.");
                    }
                }
            }

            payloadStore.InvokeThreadPerformance.ComputeTime.ExecuteHttpAdaptation = (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (log.IsInfoEnabled)
            {
                log.Info($"Entity Invoke: GUID {modelGuid} and model {modelId} Adaptations have concluded.");
            }
        }

        private void ExecuteExhaustiveAdaptation()
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} will now perform Exhaustive and will loop through each.");
            }

            foreach (var exhaustive in EntityAnalysisModel.ExhaustiveModels)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}.");
                }

                try
                {
                    var data = new double[exhaustive.NetworkVariablesInOrder.Count];
                    for (var i = 0; i < exhaustive.NetworkVariablesInOrder.Count; i++)
                    {
                        var cleanName = exhaustive.NetworkVariablesInOrder[i].Name.Contains('.')
                            ? exhaustive.NetworkVariablesInOrder[i].Name.Split(".")[1]
                            : exhaustive.NetworkVariablesInOrder[i].Name;

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                $"  Will look up {cleanName} for processing type id {exhaustive.NetworkVariablesInOrder[i].ProcessingTypeId}.");
                        }

                        switch (exhaustive.NetworkVariablesInOrder[i].ProcessingTypeId)
                        {
                            case 1:
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" will look up {cleanName} for payload.");
                                }

                                if (EntityAnalysisModelInstanceEntryPayloadStore
                                    .Payload.ContainsKey(cleanName))
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        EntityAnalysisModelInstanceEntryPayloadStore.Payload[cleanName].AsDouble());

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                            $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                            $" {cleanName} found value {data[i]}.");
                                    }
                                }
                                else
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        exhaustive.NetworkVariablesInOrder[i].Mean);

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                            $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                            $" {cleanName} fall back value {data[i]}.");
                                    }
                                }

                                break;
                            case 2:
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" will look up {cleanName} for KVP.");
                                }

                                if (EntityAnalysisModelInstanceEntryPayloadStore
                                    .Dictionary.TryGetValue(cleanName, out var valueKvp))
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        valueKvp);

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                            $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                            $" {cleanName} found value {data[i]}.");
                                    }
                                }
                                else
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        exhaustive.NetworkVariablesInOrder[i].Mean);

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                            $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                            $" {cleanName} fall back value {data[i]}.");
                                    }
                                }

                                break;
                            case 3:
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" will look up {cleanName} for Ttl Counter.");
                                }

                                if (EntityAnalysisModelInstanceEntryPayloadStore
                                    .TtlCounter.TryGetValue(cleanName, out var valueTtl))
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        valueTtl);

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                            $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                            $" {cleanName} found value {data[i]}.");
                                    }
                                }
                                else
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        exhaustive.NetworkVariablesInOrder[i].Mean);

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                            $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                            $" {cleanName} fall back value {data[i]}.");
                                    }
                                }

                                break;
                            case 4:
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" will look up {cleanName} for Ttl Counter.");
                                }

                                if (EntityAnalysisModelInstanceEntryPayloadStore
                                    .Sanction.TryGetValue(cleanName, out var valueSanction))
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        valueSanction);

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                            $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                            $" {cleanName} found value {data[i]}.");
                                    }
                                }
                                else
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        exhaustive.NetworkVariablesInOrder[i].Mean);

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                            $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                            $" {cleanName} fall back value {data[i]}.");
                                    }
                                }

                                break;
                            case 5:
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" will look up {cleanName} for Abstraction.");
                                }

                                if (EntityAnalysisModelInstanceEntryPayloadStore
                                    .Abstraction.TryGetValue(cleanName, out var valueAbstraction))
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        valueAbstraction);

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                            $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                            $" {cleanName} found value {data[i]}.");
                                    }
                                }
                                else
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        exhaustive.NetworkVariablesInOrder[i].Mean);

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                            $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                            $" {cleanName} fall back value {data[i]}.");
                                    }
                                }

                                break;
                            case 6:
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" will look up {cleanName} for Abstraction Calculation.");
                                }

                                if (EntityAnalysisModelInstanceEntryPayloadStore
                                    .AbstractionCalculation.TryGetValue(cleanName, out var valueAbstractionCalculation))
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        valueAbstractionCalculation);

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                            $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                            $" {cleanName} found value {data[i]}.");
                                    }
                                }
                                else
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        exhaustive.NetworkVariablesInOrder[i].Mean);

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                            $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                            $" {cleanName} fall back value {data[i]}.");
                                    }
                                }

                                break;
                            default:
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                        $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                        $" will look up {cleanName} for Abstraction as the default.");
                                }

                                if (EntityAnalysisModelInstanceEntryPayloadStore
                                    .Abstraction.TryGetValue(cleanName, out var valueAbstractionDefault))
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        valueAbstractionDefault);

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                            $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                            $" {cleanName} found value {data[i]}.");
                                    }
                                }
                                else
                                {
                                    data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                                        exhaustive.NetworkVariablesInOrder[i].Mean);

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} " +
                                            $" and model {EntityAnalysisModel.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                                            $" {cleanName} fall back value {data[i]}.");
                                    }
                                }

                                break;
                        }
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} is about to recall model with {data.Length} variables.");
                    }

                    var value = exhaustive.TopologyNetwork.Compute(data)[0];

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} has recalled a score of {value}.  Will proceed to add the value to payload collection.");
                    }

                    EntityAnalysisModelInstanceEntryPayloadStore.ExhaustiveAdaptation.Add(exhaustive.Name, value);
                }
                catch (Exception ex)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has exception {ex}.");
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} has concluded exhaustive recall.");
                }
            }

            EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.ExecuteExhaustiveAdaptation = (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);
        }

        private void ExecuteAbstractionCalculations()
        {
            double calculationDouble = 0;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} will now perform entity analysis abstractions calculations and will loop through each.");
            }

            foreach (var entityAnalysisModelAbstractionCalculation in EntityAnalysisModel
                         .EntityAnalysisModelAbstractionCalculations)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id}.");
                }

                try
                {
                    if (entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId == 5)
                    {
                        calculationDouble = ReflectRule.Execute(entityAnalysisModelAbstractionCalculation,
                            EntityAnalysisModel,
                            EntityAnalysisModelInstanceEntryPayloadStore, EntityInstanceEntryDictionaryKvPs, log);
                    }
                    else
                    {
                        try
                        {
                            double leftDouble = 0;
                            double rightDouble = 0;

                            var cleanAbstractionNameLeft = entityAnalysisModelAbstractionCalculation
                                .EntityAnalysisModelAbstractionNameLeft.Replace(" ", "_");
                            var cleanAbstractionNameRight = entityAnalysisModelAbstractionCalculation
                                .EntityAnalysisModelAbstractionNameRight.Replace(" ", "_");

                            if (EntityAnalysisModelInstanceEntryPayloadStore.Abstraction.TryGetValue(
                                    cleanAbstractionNameLeft, out var valueLeft))
                            {
                                leftDouble = valueLeft;

                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} and has extracted left value of {leftDouble}.");
                                }
                            }
                            else
                            {
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} but it does not contain a left value.");
                                }
                            }

                            if (EntityAnalysisModelInstanceEntryPayloadStore.Abstraction.TryGetValue(
                                    cleanAbstractionNameRight, out var valueRight))
                            {
                                rightDouble = valueRight;

                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} and extracted right value of {rightDouble}.");
                                }
                            }
                            else
                            {
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} but it does not contain a right value.");
                                }
                            }

                            switch (entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId)
                            {
                                case 1:
                                    calculationDouble = leftDouble + rightDouble;

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} addition, produces value of {calculationDouble}.");
                                    }

                                    break;
                                case 2:
                                    calculationDouble = leftDouble - rightDouble;

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} subtraction, produces value of {calculationDouble}.");
                                    }

                                    break;
                                case 3:
                                    calculationDouble = leftDouble / rightDouble;

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} divide, produces value of {calculationDouble}.");
                                    }

                                    break;
                                case 4:
                                    calculationDouble = leftDouble * rightDouble;

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} multiply, produces value of {calculationDouble}.");
                                    }

                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            calculationDouble = 0;

                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} has produced an error in calculation and has been set to zero with exception message of {ex.Message}.");
                            }
                        }

                        if (Double.IsNaN(calculationDouble) | Double.IsInfinity(calculationDouble))
                        {
                            calculationDouble = 0;

                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} has produced IsNaN or IsInfinity and has been set to zero.");
                            }
                        }
                    }

                    EntityAnalysisModelInstanceEntryPayloadStore.AbstractionCalculation.Add(
                        entityAnalysisModelAbstractionCalculation.Name, calculationDouble);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} and has added the name {entityAnalysisModelAbstractionCalculation.Name} with the value {calculationDouble} to abstractions for processing.");
                    }

                    if (entityAnalysisModelAbstractionCalculation.ResponsePayload)
                    {
                        EntityInstanceEntryAbstractionCalculationResponse.Add(
                            entityAnalysisModelAbstractionCalculation.Name, calculationDouble);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} and has added the name {entityAnalysisModelAbstractionCalculation.Name} with the value {calculationDouble} to response payload also.");
                        }
                    }

                    if (entityAnalysisModelAbstractionCalculation.ReportTable && !Reprocess)
                    {
                        ReportDatabaseValues.Add(new ArchiveKey
                        {
                            ProcessingTypeId = 6,
                            Key = entityAnalysisModelAbstractionCalculation.Name,
                            KeyValueFloat = calculationDouble,
                            EntityAnalysisModelInstanceEntryGuid =
                                EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid
                        });

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} and has added the name {entityAnalysisModelAbstractionCalculation.Name} with the value {calculationDouble} to report payload also with a column name of {entityAnalysisModelAbstractionCalculation.Name}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} evaluating abstraction calculation {entityAnalysisModelAbstractionCalculation.Id} has produced an error as {ex}.");
                }
            }

            EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.ExecuteAbstractionCalculation = (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Abstraction Calculations have concluded in {Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency} ns.");
            }
        }

        private void ExecuteAbstractionRulesWithoutSearchKeys()
        {
            foreach (var evaluateAbstractionRule in
                     from evaluateAbstractionRuleLinq in EntityAnalysisModel.ModelAbstractionRules
                     where !evaluateAbstractionRuleLinq.Search
                     select evaluateAbstractionRuleLinq)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} abstraction rule {evaluateAbstractionRule.Id} is being processed as a basic rule.");
                }

                double abstractionValue;

                if (ReflectRule.Execute(evaluateAbstractionRule, EntityAnalysisModel, CachePayloadDocumentStore,
                        EntityInstanceEntryTtlCountersResponse, EntityInstanceEntryDictionaryKvPs, log))
                {
                    abstractionValue = 1;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} abstraction rule {evaluateAbstractionRule.Id} has returned true and set abstraction value to {abstractionValue}.");
                    }
                }
                else
                {
                    abstractionValue = 0;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} abstraction rule {evaluateAbstractionRule.Id} has returned false and set abstraction value to {abstractionValue}.");
                    }
                }

                EntityAnalysisModelInstanceEntryPayloadStore.Abstraction.Add(evaluateAbstractionRule.Name,
                    abstractionValue);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is a basic abstraction rule {evaluateAbstractionRule.Id} added value {abstractionValue} to processing.");
                }

                if (evaluateAbstractionRule.ResponsePayload)
                {
                    EntityInstanceEntryAbstractionResponse.Add(evaluateAbstractionRule.Name, abstractionValue);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is a basic abstraction rule {evaluateAbstractionRule.Id} added value {abstractionValue} to response payload.");
                    }
                }

                if (evaluateAbstractionRule.ReportTable && !Reprocess)
                {
                    ReportDatabaseValues.Add(new ArchiveKey
                    {
                        ProcessingTypeId = 5,
                        Key = evaluateAbstractionRule.Name,
                        KeyValueFloat = abstractionValue,
                        EntityAnalysisModelInstanceEntryGuid =
                            EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid
                    });

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is a basic abstraction rule {evaluateAbstractionRule.Id} added value {abstractionValue} to report payload with a column name of {evaluateAbstractionRule.Name}.");
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} finished basic abstraction rule {evaluateAbstractionRule.Id}.");
                }
            }

            EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.ExecuteAbstractionRulesWithoutSearchKey = (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Abstraction has concluded in {Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency} ns.");
            }
        }

        private async Task ExecuteAbstractionRulesWithSearchKeysAsync(List<Task<TimedTaskResult>> pendingWriteTasks)
        {
            var pendingExecutionThreads = new List<Task>();
            if (EntityAnalysisModel.EnableCache)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Entity cache storage is enabled so will now proceed to loop through the distinct grouping keys for this model.");
                }

                foreach (var (key, value) in EntityAnalysisModel.DistinctSearchKeys)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating grouping key {key}.");
                    }

                    try
                    {
                        if (value.SearchKeyCache)
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} grouping key {key} is a search key,  so the values will be fetched from the cache later on.");
                            }
                        }
                        else
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} checking if grouping key {key} exists in the current payload data.");
                            }

                            if (CachePayloadDocumentStore.ContainsKey(key))
                            {
                                var execute = new Execute
                                {
                                    EntityInstanceEntryDictionaryKvPs = EntityInstanceEntryDictionaryKvPs,
                                    AbstractionRuleGroupingKey = key,
                                    DistinctSearchKey = value,
                                    CachePayloadDocument = CachePayloadDocumentStore,
                                    EntityAnalysisModelInstanceEntryPayload =
                                        EntityAnalysisModelInstanceEntryPayloadStore,
                                    AbstractionRuleMatches = AbstractionRuleMatches,
                                    EntityAnalysisModel = EntityAnalysisModel,
                                    Log = log,
                                    DynamicEnvironment = jubeEnvironment,
                                    CacheService = cacheService,
                                    PendingWritesTasks = pendingWriteTasks
                                };

                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has created a execute object to run all the abstraction rules rolling up to the grouping key.  It has been added to a collection to track it when multi threaded abstraction rules are enabled.");
                                }

                                pendingExecutionThreads.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.ExecuteAbstractionRulesWithSearchKeyAsync, async () => await execute.StartAsync()));
                            }
                            else
                            {
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} grouping key {key} does not exist in the current transaction data being processed.");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} checking if grouping key {key} has created an error as {ex}.");
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} will now loop around all of the Abstraction rules for the purposes of performing the aggregations.");
                }
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Entity cache storage is not enabled so it cannot fetch anything relating to Abstraction Rules.");
                }
            }

            await Task.WhenAll(pendingExecutionThreads).ConfigureAwait(false);

            await CalculateAbstractionRuleValuesOrLookupFromTheCache().ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} all abstraction aggregation has finished, basic rules will now be processed.");
            }

            EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.AbstractionRulesWithSearchKeysAsync = (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);
        }

        private async Task CalculateAbstractionRuleValuesOrLookupFromTheCache()
        {
            var listEntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest =
                new List<EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue>();
            foreach (var abstractionRule in EntityAnalysisModel.ModelAbstractionRules)
            {
                try
                {
                    if (abstractionRule.Search)
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating abstraction rule {abstractionRule.Id}.");
                        }

                        if (EntityAnalysisModel.DistinctSearchKeys.FirstOrDefault(x =>
                                x.Key == abstractionRule.SearchKey && x.Value.SearchKeyCache).Value != null)
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} abstraction rule {abstractionRule.Id} has its values in the cache.");
                            }

                            listEntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest.Add(
                                new EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue
                                {
                                    AbstractionRuleName = abstractionRule.Name,
                                    SearchKey = abstractionRule.SearchKey,
                                    SearchValue = CachePayloadDocumentStore[abstractionRule.SearchKey].AsString()
                                });

                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} abstraction rule {abstractionRule.Id} has added " +
                                    $"EntityAnalysisModelId:{EntityAnalysisModel.Id}, AbstractionRuleName:{abstractionRule.Name},SearchKey:{abstractionRule.SearchKey} " +
                                    $"and SearchValue:{CachePayloadDocumentStore[abstractionRule.SearchKey].AsString()} to he bulk select list.");
                            }
                        }
                        else
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is aggregating abstraction rule {abstractionRule.Id} using documents in the entities collection of the cache.");
                            }

                            AddComputedValuesToAbstractionRulePayload(
                                EntityAnalysisModelAbstractionRuleAggregator.Aggregate(
                                    EntityAnalysisModelInstanceEntryPayloadStore,
                                    AbstractionRuleMatches, abstractionRule, log), abstractionRule);
                        }
                    }

                    if (!listEntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest.Any())
                    {
                        continue;
                    }

                    foreach (var abstractionRuleNameValue in await cacheService.CacheAbstractionRepository
                                 .Get(
                                     EntityAnalysisModel.TenantRegistryId, EntityAnalysisModel.Guid,
                                     listEntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest)
                                 .ConfigureAwait(false))
                    {
                        AddComputedValuesToAbstractionRulePayload(abstractionRuleNameValue.Value, abstractionRule);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is aggregating abstraction rule {abstractionRule.Id} but has created an error as {ex}.");
                }
            }
        }

        private void AddComputedValuesToAbstractionRulePayload(double value,
            EntityAnalysisModelAbstractionRule abstractionRule)
        {
            EntityAnalysisModelInstanceEntryPayloadStore.Abstraction
                .Add(abstractionRule.Name, value);

            if (abstractionRule.ResponsePayload)
            {
                var added = EntityInstanceEntryAbstractionResponse.TryAdd(abstractionRule.Name, value);
                if (log.IsInfoEnabled)
                {
                    log.Info(added
                        ? $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is aggregating abstraction rule {abstractionRule.Id} added value {value} to response payload."
                        : $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is aggregating abstraction rule {abstractionRule.Id} added value {value} but not added to the response payload as it already exists.");
                }
            }

            if (abstractionRule.ReportTable && !Reprocess)
            {
                ReportDatabaseValues.Add(new ArchiveKey
                {
                    ProcessingTypeId = 5,
                    Key = abstractionRule.Name,
                    KeyValueFloat = value,
                    EntityAnalysisModelInstanceEntryGuid = EntityAnalysisModelInstanceEntryPayloadStore
                        .EntityAnalysisModelInstanceEntryGuid
                });

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is aggregating abstraction rule {abstractionRule.Id} added value {value} to report payload with a column name of {abstractionRule.Name}.");
                }
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} finished aggregating abstraction rule {abstractionRule.Id}.");
            }
        }

        private async Task ExecuteTtlCountersAsync(ICacheTtlCounterRepository cacheTtlCounterRepository,
            ICacheTtlCounterEntryRepository cacheTtlCounterEntryRepository)
        {
            try
            {
                if (EntityAnalysisModel.EnableTtlCounter)
                {
                    var tasks = new List<Task>
                    {
                        TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.OnlineAggregationOfTtlCountersAsync, async () => await OnlineAggregationOfTtlCountersAsync(cacheTtlCounterEntryRepository)),
                        TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.ExecuteOutOfProcessAggregationOfTtlCountersAsync, async () => await OutOfProcessAggregationOfTtlCountersAsync(cacheTtlCounterRepository))
                    };

                    await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter cache storage is not enabled so it cannot fetch TTL Counter Aggregation.");
                    }
                }

                EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.TtlCountersAsync =
                    (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);
            }
            catch (Exception ex)
            {
                log.Error(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has caused an error in TTL Counters as {ex}.");
            }
        }

        private async Task OutOfProcessAggregationOfTtlCountersAsync(ICacheTtlCounterRepository cacheTtlCounterRepository)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} will now look for TTL Counters from the cache.");
            }

            var ttlCounters = EntityAnalysisModel.ModelTtlCounters.FindAll(x => !x.OnlineAggregation);
            var reportDatabaseValuesBag = new ConcurrentBag<ArchiveKey>();

            var tasks = ttlCounters.Select(ttlCounter =>
            {
                return TaskHelper.MeasureTaskTimeAndMemoryAllocated(TaskType.ExecuteTimeToLiveCounterIterationAsync, async () =>
                {
                    try
                    {
                        var dataValue = CachePayloadDocumentStore[ttlCounter.TtlCounterDataName].AsString();
                        var ttlCounterValue = await cacheTtlCounterRepository
                            .GetByNameDataNameDataValueAsync(EntityAnalysisModel.TenantRegistryId,
                                EntityAnalysisModel.Guid,
                                ttlCounter.Guid,
                                ttlCounter.TtlCounterDataName,
                                dataValue).ConfigureAwait(false);

                        if (EntityAnalysisModelInstanceEntryPayloadStore.TtlCounter.TryAdd(ttlCounter.Name, ttlCounterValue))
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} is missing,  so will add this as name {ttlCounter.Name} with value of {ttlCounterValue}.");
                            }

                            if (ttlCounter.ResponsePayload)
                            {
                                lock (EntityInstanceEntryTtlCountersResponse)
                                {
                                    EntityInstanceEntryTtlCountersResponse[ttlCounter.Name] = ttlCounterValue;
                                }

                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} is missing,  added this as name {ttlCounter.Name} with value of {ttlCounterValue} to the response payload also.");
                                }
                            }

                            if (ttlCounter.ReportTable && !Reprocess)
                            {
                                reportDatabaseValuesBag.Add(new ArchiveKey
                                {
                                    ProcessingTypeId = 5,
                                    Key = ttlCounter.Name,
                                    KeyValueInteger = ttlCounterValue,
                                    EntityAnalysisModelInstanceEntryGuid =
                                        EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid
                                });

                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} is missing,  added this as name {ttlCounter.Name} with value of {ttlCounterValue} to the report payload also.");
                                }
                            }
                        }
                        else
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} is missing,  so will add this as name {ttlCounter.Name} with value of {ttlCounterValue}.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} has thrown an error as {ex}.");
                        }
                    }
                });
            }).ToList();

            await Task.WhenAll(tasks);

            if (reportDatabaseValuesBag.Count > 0)
            {
                lock (ReportDatabaseValues)
                {
                    ReportDatabaseValues.AddRange(reportDatabaseValuesBag);
                }
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counters have concluded in {Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency} ns.");
            }
        }

        private async Task OnlineAggregationOfTtlCountersAsync(
            ICacheTtlCounterEntryRepository cacheTtlCounterEntryRepository)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter cache storage is enabled so it will now proceed to return the TTL Counters with online aggregation.");
            }

            var onlineTtlCounters = EntityAnalysisModel.ModelTtlCounters
                .Where(x => x.OnlineAggregation)
                .ToList();

            if (onlineTtlCounters.Count > 0)
            {
                var tasks = onlineTtlCounters.Select(async ttlCounter =>
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} creating predication for TTL Counter {ttlCounter.Id} is online aggregation.");
                    }

                    if (CachePayloadDocumentStore.ContainsKey(ttlCounter.TtlCounterDataName))
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} creating predication for TTL Counter {ttlCounter.Id} which has an interval type of {ttlCounter.TtlCounterInterval} and interval value of {ttlCounter.TtlCounterValue}.");
                        }

                        var adjustedTtlCounterDate = ttlCounter.TtlCounterInterval switch
                        {
                            "d" => EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate.AddDays(
                                ttlCounter.TtlCounterValue * -1),
                            "h" => EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate.AddHours(
                                ttlCounter.TtlCounterValue * -1),
                            "n" => EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate.AddMinutes(
                                ttlCounter.TtlCounterValue * -1),
                            "s" => EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate.AddSeconds(
                                ttlCounter.TtlCounterValue * -1),
                            "m" => EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate.AddMonths(
                                ttlCounter.TtlCounterValue * -1),
                            "y" => EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate.AddYears(
                                ttlCounter.TtlCounterValue * -1),
                            _ => default(DateTime)
                        };

                        var count = await cacheTtlCounterEntryRepository.GetAsync(
                            EntityAnalysisModel.TenantRegistryId,
                            EntityAnalysisModel.Guid,
                            ttlCounter.Guid,
                            ttlCounter.TtlCounterDataName,
                            CachePayloadDocumentStore[ttlCounter.TtlCounterDataName].AsString(),
                            adjustedTtlCounterDate,
                            EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate
                        ).ConfigureAwait(false);

                        try
                        {
                            lock (EntityInstanceEntryTtlCountersResponse)
                            {
                                if (!EntityInstanceEntryTtlCountersResponse.ContainsKey(ttlCounter.Name))
                                {
                                    EntityAnalysisModelInstanceEntryPayloadStore.TtlCounter.Add(ttlCounter.Name, count);
                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} is missing, so will add this as name {ttlCounter.Name} with value of zero.");
                                    }

                                    if (ttlCounter.ResponsePayload)
                                    {
                                        EntityInstanceEntryTtlCountersResponse.Add(ttlCounter.Name, count);

                                        if (log.IsInfoEnabled)
                                        {
                                            log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} is missing, added this as name {ttlCounter.Name} with value of zero to the response payload also.");
                                        }
                                    }
                                    if (ttlCounter.ReportTable && !Reprocess)
                                    {
                                        ReportDatabaseValues.Add(new ArchiveKey
                                        {
                                            ProcessingTypeId = 5,
                                            Key = ttlCounter.Name,
                                            KeyValueInteger = count,
                                            EntityAnalysisModelInstanceEntryGuid =
                                                EntityAnalysisModelInstanceEntryPayloadStore
                                                    .EntityAnalysisModelInstanceEntryGuid
                                        });

                                        if (log.IsInfoEnabled)
                                        {
                                            log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} is missing, added this as name {ttlCounter.Name} with value of zero to the report payload also.");
                                        }
                                    }
                                }
                                else
                                {
                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} exists already, so nothing more added.");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} TTL Counter {ttlCounter.Id} has created an error as {ex}.");
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} creating predication for TTL Counter {ttlCounter.Id} the date threshold from is {adjustedTtlCounterDate} to {EntityAnalysisModelInstanceEntryPayloadStore.ReferenceDate}, the TTL Counter Name is {ttlCounter.Name}, the TTL Counter Data Name is {ttlCounter.TtlCounterDataName} and the TTL Counter Data Name Value is {CachePayloadDocumentStore[ttlCounter.TtlCounterDataName]}.");
                        }
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} was unable to fine a value for TTL Counter Data Name {ttlCounter.TtlCounterDataName} and TTL Counter Name {ttlCounter.Name}.");
                        }
                    }
                }).ToList();

                await Task.WhenAll(tasks);
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Does not have any online TTL Counters.");
                }
            }
        }
        private ICacheTtlCounterEntryRepository BuildCacheTtlCounterEntryRepository()
        {
            ICacheTtlCounterEntryRepository cacheTtlCounterEntryRepository = new CacheTtlCounterEntryRepository(
                cacheService.RedisDatabase, log, 0);

            return cacheTtlCounterEntryRepository;
        }

        private Task ExecuteDictionaryKvPsAsync()
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} Will now look for KVP Dictionary Values.");
            }

            foreach (var (i, kvpDictionary) in EntityAnalysisModel.KvpDictionaries)
            {
                try
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i}.");
                    }

                    double value;
                    if (CachePayloadDocumentStore.TryGetValue(kvpDictionary.DataName, out var valueCache))
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has been found in the data payload.");
                        }

                        var key = valueCache.AsString();

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has been found in the data payload and has returned a value of {key}, which will be used for the lookup.");
                        }

                        if (kvpDictionary.KvPs.TryGetValue(key, out var p))
                        {
                            value = p;

                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has been found in the data payload and has returned a value of {key}, found a lookup value.  The dictionary value has been set to {value}.");
                            }
                        }
                        else
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has been found in the data payload and has returned a value of {key}, does not contain a lookup value.  The dictionary value has been set to zero.");
                            }

                            value = 0;
                        }
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} but the payload does not have such a value,  so have set the value to zero.");
                        }

                        value = 0;
                    }

                    if (EntityInstanceEntryDictionaryKvPs.TryAdd(kvpDictionary.Name, value))
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has added the name of {kvpDictionary.Name} and value of {value} for processing.");
                        }

                        if (kvpDictionary.ResponsePayload)
                        {
                            EntityInstanceEntryDictionaryKvPsResponse.Add(kvpDictionary.Name, value);

                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has added the name of {kvpDictionary.Name} and value of {value} to response payload.");
                            }
                        }
                        else
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has not added the name of {kvpDictionary.Name} and value of {value} to response payload.");
                            }
                        }
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is evaluating dictionary kvp key of {i} has already added the name of {kvpDictionary.Name}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has caused an error in Dictionary KVP as {ex}.");
                    }
                }
            }

            EntityAnalysisModelInstanceEntryPayloadStore.Dictionary = EntityInstanceEntryDictionaryKvPs;
            EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.DictionaryKvPsAsync = (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has finished looking for Dictionary KVP values.");
            }

            return Task.CompletedTask;
        }

        private void ExecuteInlineFunctions()
        {
            try
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is going to check for inline functions.");
                }

                foreach (var inlineFunction in EntityAnalysisModel.EntityAnalysisModelInlineFunctions)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is going to invoke inline function {inlineFunction.Id}.");
                    }

                    try
                    {
                        var output = ReflectRule.Execute(inlineFunction, EntityAnalysisModel,
                            EntityAnalysisModelInstanceEntryPayloadStore,
                            EntityInstanceEntryDictionaryKvPs, log);

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} and returned a value of {output}.");
                        }

                        if (!CachePayloadDocumentStore.ContainsKey(inlineFunction.Name))
                        {
                            switch (inlineFunction.ReturnDataTypeId)
                            {
                                case 1:
                                    CachePayloadDocumentStore.TryAdd(inlineFunction.Name, output.ToString());

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as string.");
                                    }

                                    break;
                                case 2:
                                    CachePayloadDocumentStore.TryAdd(inlineFunction.Name, Convert.ToInt32(output));

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as integer.");
                                    }

                                    break;
                                case 3:
                                    CachePayloadDocumentStore.TryAdd(inlineFunction.Name, Convert.ToDouble(output));

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as double.");
                                    }

                                    break;
                                case 4:
                                    CachePayloadDocumentStore.TryAdd(inlineFunction.Name, Convert.ToDateTime(output));

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as date.");
                                    }

                                    break;
                                case 5:
                                    CachePayloadDocumentStore.TryAdd(inlineFunction.Name, Convert.ToBoolean(output));

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as boolean.");
                                    }

                                    break;
                            }
                        }
                        else
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has not added to payload as name {inlineFunction.Name} already exists.");
                            }
                        }


                        if (inlineFunction.ResponsePayload)
                        {
                            if (!CachePayloadDocumentResponse.ContainsKey(inlineFunction.Name))
                            {
                                switch (inlineFunction.ReturnDataTypeId)
                                {
                                    case 1:
                                        CachePayloadDocumentResponse.TryAdd(inlineFunction.Name, output.AsString());

                                        if (log.IsInfoEnabled)
                                        {
                                            log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to response payload as name {inlineFunction.Name} with value of {output} as string.");
                                        }

                                        break;
                                    case 2:
                                        CachePayloadDocumentResponse.TryAdd(inlineFunction.Name, Convert.ToInt32(output));

                                        if (log.IsInfoEnabled)
                                        {
                                            log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to response payload as name {inlineFunction.Name} with value of {output} as integer.");
                                        }

                                        break;
                                    case 3:
                                        CachePayloadDocumentResponse.TryAdd(inlineFunction.Name, Convert.ToDouble(output));

                                        if (log.IsInfoEnabled)
                                        {
                                            log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to response payload as name {inlineFunction.Name} with value of {output} as double.");
                                        }

                                        break;
                                    case 4:
                                        CachePayloadDocumentResponse.TryAdd(inlineFunction.Name,
                                            Convert.ToDateTime(output));

                                        if (log.IsInfoEnabled)
                                        {
                                            log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to response payload as name {inlineFunction.Name} with value of {output} as date.");
                                        }

                                        break;
                                    case 5:
                                        CachePayloadDocumentResponse.TryAdd(inlineFunction.Name,
                                            Convert.ToBoolean(output));

                                        if (log.IsInfoEnabled)
                                        {
                                            log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to response payload as name {inlineFunction.Name} with value of {output} as boolean.");
                                        }

                                        break;
                                }
                            }
                        }
                        else
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has not added to response payload as name {inlineFunction.Name} already exists.");
                            }
                        }

                        if (inlineFunction.ReportTable && !Reprocess)
                        {
                            switch (inlineFunction.ReturnDataTypeId)
                            {
                                case 1:
                                    ReportDatabaseValues.Add(new ArchiveKey
                                    {
                                        ProcessingTypeId = 3,
                                        Key = inlineFunction.Name,
                                        KeyValueString = output == null ? null : Convert.ToString(output),
                                        EntityAnalysisModelInstanceEntryGuid =
                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                .EntityAnalysisModelInstanceEntryGuid
                                    });

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as string.");
                                    }

                                    break;
                                case 2:
                                    if (output != null)
                                    {
                                        ReportDatabaseValues.Add(new ArchiveKey
                                        {
                                            ProcessingTypeId = 3,
                                            Key = inlineFunction.Name,
                                            KeyValueInteger = (int)output,
                                            EntityAnalysisModelInstanceEntryGuid =
                                                EntityAnalysisModelInstanceEntryPayloadStore
                                                    .EntityAnalysisModelInstanceEntryGuid
                                        });

                                        if (log.IsInfoEnabled)
                                        {
                                            log.Info(
                                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as integer.");
                                        }
                                    }

                                    break;
                                case 3:
                                    ReportDatabaseValues.Add(new ArchiveKey
                                    {
                                        ProcessingTypeId = 3,
                                        Key = inlineFunction.Name,
                                        KeyValueFloat = Convert.ToDouble(output),
                                        EntityAnalysisModelInstanceEntryGuid =
                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                .EntityAnalysisModelInstanceEntryGuid
                                    });

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as double.");
                                    }

                                    break;
                                case 4:
                                    ReportDatabaseValues.Add(new ArchiveKey
                                    {
                                        ProcessingTypeId = 3,
                                        Key = inlineFunction.Name,
                                        KeyValueDate = Convert.ToDateTime(output),
                                        EntityAnalysisModelInstanceEntryGuid =
                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                .EntityAnalysisModelInstanceEntryGuid
                                    });

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as date.");
                                    }

                                    break;
                                case 5:
                                    ReportDatabaseValues.Add(new ArchiveKey
                                    {
                                        ProcessingTypeId = 3,
                                        Key = inlineFunction.Name,
                                        KeyValueBoolean = Convert.ToByte(output),
                                        EntityAnalysisModelInstanceEntryGuid =
                                            EntityAnalysisModelInstanceEntryPayloadStore
                                                .EntityAnalysisModelInstanceEntryGuid
                                    });

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as boolean.");
                                    }

                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} is has invoked inline function {inlineFunction.Id} has created an error as {ex}.");
                        }
                    }
                }

                EntityAnalysisModelInstanceEntryPayloadStore.InvokeThreadPerformance.ComputeTime.InlineFunction = (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has passed inline functions.");
                }
            }
            catch (Exception ex)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {EntityAnalysisModelInstanceEntryPayloadStore.EntityAnalysisModelInstanceEntryGuid} and model {EntityAnalysisModel.Id} has experienced an error invoking inline functions as {ex}.");
                }
            }
        }

        private void ExecuteGatewayRules(ref double maxGatewayResponseElevation, ref bool matchedGateway)
        {
            var payloadStore = EntityAnalysisModelInstanceEntryPayloadStore;
            var model = EntityAnalysisModel;
            var modelGuid = payloadStore.EntityAnalysisModelInstanceEntryGuid;
            var modelId = model.Id;
            var gatewayRules = model.ModelGatewayRules;

            var gatewaySample = Seeded.NextDouble();

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {modelGuid} and model {modelId} is going to invoke Gateway Rules with a gateway sample of {gatewaySample}.");
            }

            var rulesCount = gatewayRules.Count;
            for (var i = 0; i < rulesCount; i++)
            {
                var gatewayRule = gatewayRules[i];
                try
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelGuid} and model {modelId} is going to invoke Gateway Rule {gatewayRule.EntityAnalysisModelGatewayRuleId} with a gateway sample of {gatewaySample}.  The model's Gateway Sample is {gatewayRule.GatewaySample} to be tested against {gatewaySample} .");
                    }

                    if (gatewaySample >= gatewayRule.GatewaySample)
                    {
                        continue;
                    }

                    if (!gatewayRule.GatewayRuleCompileDelegate(CachePayloadDocumentStore,
                            model.EntityAnalysisModelLists,
                            EntityInstanceEntryDictionaryKvPs,
                            log))
                    {
                        continue;
                    }

                    matchedGateway = true;
                    maxGatewayResponseElevation = gatewayRule.MaxResponseElevation;
                    model.ModelInvokeGatewayCounter++;
                    gatewayRule.Counter++;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelGuid} and model {modelId} is going to invoke Gateway Rule {gatewayRule.EntityAnalysisModelGatewayRuleId} as it has matched. The max response elevation has been set to {maxGatewayResponseElevation} and Model Invoke Gateway Counter has been set to {model.ModelInvokeGatewayCounter}. The Entity Model Gateway Rule Counter has been set to {gatewayRule.Counter}.");
                    }

                    break;
                }
                catch (Exception ex)
                {
                    log.Error(
                        $"Entity Invoke: GUID {modelGuid} and model {modelId} has tried to invoke Gateway Rule {gatewayRule.EntityAnalysisModelGatewayRuleId} but it has caused an error as {ex}.");
                }
            }

            payloadStore.InvokeThreadPerformance.ComputeTime.Gateway = (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {modelGuid} and model {modelId} Gateway Rules have concluded {Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency} ns and has returned {matchedGateway} to continue processing.");
            }
        }

        private void ExecuteInlineScripts()
        {
            var payloadStore = EntityAnalysisModelInstanceEntryPayloadStore;
            var model = EntityAnalysisModel;
            var modelGuid = payloadStore.EntityAnalysisModelInstanceEntryGuid;
            var modelId = model.Id;
            var inlineScripts = model.EntityAnalysisModelInlineScripts;
            var inlineScriptCount = inlineScripts.Count;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {modelGuid} and model {modelId}. Model Invocation Counter is now {model.ModelInvokeCounter}.");
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {modelGuid} and model {modelId} is going to search for inline scripts to be invoked.");
            }

            for (var i = 0; i < inlineScriptCount; i++)
            {
                var inlineScript = inlineScripts[i];
                try
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelGuid} and model {modelId} is going to invoke {inlineScript.InlineScriptCode}.");
                    }

                    var tempPayloadStore = CachePayloadDocumentStore;
                    var tempPayloadResponse = CachePayloadDocumentResponse;
                    var tempReportDatabaseValues = ReportDatabaseValues;

                    ReflectInlineScript.Execute(
                        inlineScript,
                        ref tempPayloadStore,
                        ref tempPayloadResponse,
                        log);

                    ReportDatabaseValues = tempReportDatabaseValues;
                    CachePayloadDocumentResponse = tempPayloadResponse;
                    CachePayloadDocumentStore = tempPayloadStore;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {modelGuid} and model {modelId} has invoked {inlineScript.InlineScriptCode}.");
                    }
                }
                catch (Exception ex)
                {
                    log.Error(
                        $"Entity Invoke: GUID {modelGuid} and model {modelId} has tried to invoke inline script {inlineScript.InlineScriptCode} but it has produced an error as {ex}.");
                }
            }

            payloadStore.InvokeThreadPerformance.ComputeTime.InlineScript = (int)(Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Entity Invoke: GUID {modelGuid} and model {modelId} inline script invocation has concluded {Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency} ns.");
            }
        }
    }
}
