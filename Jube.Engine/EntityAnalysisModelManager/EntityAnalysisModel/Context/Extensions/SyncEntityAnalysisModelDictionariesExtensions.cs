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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AutoMapper.Internal;
    using Data.Repository;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;

    public static class SyncEntityAnalysisModelDictionariesExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelDictionariesAsync(this Context context)
        {
            try
            {
                context.Services.Parser.EntityAnalysisModelsDictionaries = [];

                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose determining adding Dictionary.");
                    }

                    var repositoryDictionary = new EntityAnalysisModelDictionaryRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelDictionaryRepository.GetByEntityAnalysisModelId and entity model key of {key}.");
                    }

                    var recordsDictionary = await repositoryDictionary.GetByEntityAnalysisModelIdOrderByIdAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

                    var shadowKvpDictionary = new Dictionary<int, EntityAnalysisModelDictionary>();
                    foreach (var recordDictionary in recordsDictionary)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Dictionary ID {recordDictionary.Id} returned for model {key}.");
                            }

                            if (recordDictionary.Active.Value != 1)
                            {
                                continue;
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Dictionary ID {recordDictionary.Id} returned for model {key} is active.");
                            }

                            var kvpDictionary = new EntityAnalysisModelDictionary();

                            if (recordDictionary.DataName != null)
                            {
                                kvpDictionary.DataName = recordDictionary.DataName.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} set Data Name as {kvpDictionary.DataName}.");
                                }
                            }

                            if (recordDictionary.ResponsePayload.HasValue)
                            {
                                kvpDictionary.ResponsePayload = recordDictionary.ResponsePayload == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} set Response Payload as {kvpDictionary.ResponsePayload}.");
                                }
                            }
                            else
                            {
                                kvpDictionary.ResponsePayload = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} found empty and set Response Payload as {kvpDictionary.ResponsePayload}.");
                                }
                            }

                            if (recordDictionary.Name == null)
                            {
                                continue;
                            }

                            kvpDictionary.Name = recordDictionary.Name.Replace(" ", "_");

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} set Name as {kvpDictionary.Name}.");
                            }

                            shadowKvpDictionary.Add(recordDictionary.Id, kvpDictionary);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} added {kvpDictionary.Name} to shadow copy of dictionary.");
                            }

                            context.Services.Parser.EntityAnalysisModelsDictionaries.TryAdd(recordDictionary.Name);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model  {key} and Dictionary {recordDictionary.Id} added {kvpDictionary.Name} to context.Parser.");
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Dictionary ID {recordDictionary.Id} returned for model {key} is in error with {ex}.");
                        }
                    }

                    foreach (var (i, kvpDictionary) in shadowKvpDictionary)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        var repositoryDictionaryKvp = new EntityAnalysisModelDictionaryKvpRepository(context.Services.DbContext);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Executing EntityAnalysisModelDictionaryKvpRepository.GetByEntityAnalysisModelDictionaryId for entity model key of {key} and Dictionary id {i}.");
                        }

                        var recordsDictionaryKvp = await repositoryDictionaryKvp.GetByEntityAnalysisModelDictionaryIdOrderByIdAsync(i, context.Services.CancellationToken).ConfigureAwait(false);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug("Returned all Dictionary KVP from the database.");
                        }

                        foreach (var recordDictionaryKvp in recordsDictionaryKvp)
                        {
                            context.Services.CancellationToken.ThrowIfCancellationRequested();

                            try
                            {
                                if (recordDictionaryKvp.KvpKey != null)
                                {
                                    var kvpKey = recordDictionaryKvp.KvpKey;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Dictionary KVP entity model key of {key} and Dictionary KVP id {recordDictionaryKvp.KvpKey} has found value of {kvpKey} .");
                                    }

                                    if (recordDictionaryKvp.KvpValue.HasValue)
                                    {
                                        var kvpValue = recordDictionaryKvp.KvpValue.Value;

                                        if (kvpDictionary.KvPs.TryAdd(kvpKey, kvpValue))
                                        {
                                            continue;
                                        }

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {key} has already added the KVP value.");
                                        }
                                    }
                                    else
                                    {
                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {key} has found null value.");
                                        }
                                    }
                                }
                                else
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {key} has found null value.");
                                    }
                                }
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                context.Services.Log.Error(
                                    $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {key} is in error with {ex}.");
                            }
                        }

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Dictionary Value entity model key of {key} and Dictionary id {i} has added all Dictionary values.");
                        }
                    }

                    value.Dependencies.KvpDictionaries = shadowKvpDictionary;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelDictionariesAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.Dictionaries, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
