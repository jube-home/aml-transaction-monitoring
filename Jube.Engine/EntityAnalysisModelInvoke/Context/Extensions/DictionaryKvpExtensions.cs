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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    using System;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using log4net;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using EntityAnalysisModel=EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

    public static class DictionaryKvpExtensions
    {
        public static void ResolveDictionaryValueForField(
            this EntityAnalysisModel entityAnalysisModel,
            EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayload,
            ILog log,
            string fieldName)
        {
            foreach (var (i, kvpDictionary) in entityAnalysisModel.Dependencies.KvpDictionaries)
            {
                if (!String.Equals(kvpDictionary.DataName, fieldName, StringComparison.Ordinal))
                {
                    continue;
                }

                try
                {
                    if (entityAnalysisModelInstanceEntryPayload.Dictionary.ContainsKey(kvpDictionary.Name))
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Entity Invoke: GUID {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Instance.Id} is skipping dictionary kvp key of {i} as {kvpDictionary.Name} has already been resolved.");
                        }

                        continue;
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} for field {fieldName}.");
                    }

                    var value = LookupFromTheLocalCacheOfDictionaryKeyValuePairs(entityAnalysisModel, entityAnalysisModelInstanceEntryPayload, log, kvpDictionary, i);
                    AddToResponsesIfNotAdded(entityAnalysisModel, entityAnalysisModelInstanceEntryPayload, log, kvpDictionary, value, i);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Instance.Id} has caused an error in Dictionary KVP as {ex}.");
                    }
                }
            }
        }

        private static void AddToResponsesIfNotAdded(
            EntityAnalysisModel entityAnalysisModel,
            EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayload,
            ILog log,
            EntityAnalysisModelDictionary kvpDictionary, double value, int i)
        {
            if (entityAnalysisModelInstanceEntryPayload.Dictionary.TryAdd(kvpDictionary.Name, value))
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} has added the name of {kvpDictionary.Name} and value of {value} for processing.");
                }
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} has already added the name of {kvpDictionary.Name}.");
                }
            }
        }

        private static double LookupFromTheLocalCacheOfDictionaryKeyValuePairs(
            EntityAnalysisModel entityAnalysisModel,
            EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayload,
            ILog log,
            EntityAnalysisModelDictionary kvpDictionary, int i)
        {
            double value;
            if (entityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(kvpDictionary.DataName, out var valueCache))
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} has been found in the data payload.");
                }

                var key = valueCache.AsString();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} has been found in the data payload and has returned a value of {key}, which will be used for the lookup.");
                }

                if (kvpDictionary.KvPs.TryGetValue(key, out var p))
                {
                    value = p;

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} has been found in the data payload and has returned a value of {key}, found a lookup value.  The dictionary value has been set to {value}.");
                    }
                }
                else
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Entity Invoke: GUID {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} has been found in the data payload and has returned a value of {key}, does not contain a lookup value.  The dictionary value has been set to zero.");
                    }

                    value = 0;
                }
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Entity Invoke: GUID {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {entityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} but the payload does not have such a value,  so have set the value to zero.");
                }

                value = 0;
            }

            return value;
        }
    }
}
