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
    using System.Linq;
    using System.Threading.Tasks;
    using AutoMapper.Internal;
    using Data.Repository;

    public static class SyncEntityAnalysisModelListsExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelListsAsync(this Context context)
        {
            try
            {
                context.Services.Parser.EntityAnalysisModelsLists = [];

                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose determining adding Lists.");
                    }

                    var repositoryList = new EntityAnalysisModelListRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelListRepository.GetByEntityAnalysisModelId and entity model key of {key}.");
                    }

                    var recordsList = await repositoryList.GetByEntityAnalysisModelIdOrderByIdAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

                    var listIdName = new Dictionary<int, string>();
                    var shadowEntityAnalysisModelLists = new Dictionary<string, List<string>>();
                    foreach (var recordList in recordsList)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: List ID ID {recordList.Id} returned for model {key}.");
                            }

                            if (recordList.Active != 1)
                            {
                                continue;
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: List ID ID {recordList.Id} returned for model {key} is active.");
                            }

                            if (recordList.Name == null)
                            {
                                continue;
                            }

                            var name = recordList.Name.Replace(" ", "_");
                            listIdName.Add(recordList.Id, recordList.Name);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model  {key} and List {recordList.Id} set Name as {name}.");
                            }

                            context.Services.Parser.EntityAnalysisModelsLists.TryAdd(recordList.Name);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model  {key} and List {recordList.Id} added {name} to parser.");
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: List ID ID {recordList.Id} returned for model {key} is in error with {ex}.");
                        }
                    }


                    foreach (var (i, s) in
                             from listIdNameKvp in listIdName
                             where !shadowEntityAnalysisModelLists.ContainsKey(listIdNameKvp.Value)
                             select listIdNameKvp)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        shadowEntityAnalysisModelLists.Add(s, []);

                        var repositoryListValues = new EntityAnalysisModelListValueRepository(context.Services.DbContext);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Executing EntityAnalysisModelListValueRepository.GetByEntityAnalysisModelListId for entity model key of {key} and list id {i}.");
                        }

                        var recordsListValues = await repositoryListValues.GetByEntityAnalysisModelListIdOrderByIdAsync(i, context.Services.CancellationToken).ConfigureAwait(false);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug("Returned all ListValues from the database.");
                        }

                        foreach (var recordListValues in recordsListValues)
                        {
                            context.Services.CancellationToken.ThrowIfCancellationRequested();

                            try
                            {
                                if (recordListValues.ListValue != null)
                                {
                                    shadowEntityAnalysisModelLists[s].Add(recordListValues.ListValue);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: List Value entity model key of {key} and list id {recordListValues.EntityAnalysisModelListId} has found value of {recordListValues.ListValue} .");
                                    }
                                }
                                else
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: List Value entity model key of {key} and list id {recordListValues.EntityAnalysisModelListId} has found null value.");
                                    }
                                }
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                context.Services.Log.Error(
                                    $"Entity Start: List Value entity model key of {key} and list id {recordListValues.EntityAnalysisModelListId} is in error with {ex}.");
                            }
                        }

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: List Value entity model key of {key} and list id {i} has added all list values.");
                        }
                    }

                    value.Dependencies.EntityAnalysisModelLists = shadowEntityAnalysisModelLists;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug($"Model {key} and List ID shadow copy has been over written");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelListsAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.Lists, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
