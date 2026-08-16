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
    using Data.Repository;

    public static class SyncSuppressionExtensions
    {
        public static async Task<Context> SyncSuppressionAsync(this Context context)
        {
            try
            {
                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose adding suppression.");
                    }

                    var repository = new EntityAnalysisModelSuppressionRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelSuppressionRepository.GetByEntityAnalysisModelId and entity model key of {key}.");
                    }

                    var records = await repository.GetByEntityAnalysisModelIdAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

                    var shadowEntityAnalysisModelSuppressionList = new Dictionary<string, List<string>>();
                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Entity Analysis Model Activation Rule Suppression ID {record.Id} returned for model {key}.");
                            }

                            var suppressionDictionary = new List<string>();

                            if (record.SuppressionKeyValue == null)
                            {
                                continue;
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Value as {record.SuppressionKeyValue} also checking to see if it is already added.");
                            }

                            if (!suppressionDictionary.Contains(record.SuppressionKeyValue))
                            {
                                suppressionDictionary.Add(record.SuppressionKeyValue);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Suppression ID  {record.Id} set Value as {record.SuppressionKeyValue} has been added to a shadow list of suppression.");
                                }
                            }

                            if (!shadowEntityAnalysisModelSuppressionList.TryAdd(record.SuppressionKey,
                                    suppressionDictionary))
                            {
                                shadowEntityAnalysisModelSuppressionList[record.SuppressionKey] =
                                    suppressionDictionary;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Suppression Key Value as {record.SuppressionKey} and already exists in collection,  added to key.");
                                }
                            }
                            else
                            {
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Suppression Key Value as {record.SuppressionKey} and does not exist in collection,  created key.");
                                    }
                                }
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Entity Analysis Model Activation Rule Suppression ID {record.Id} returned for model {key} is in error with {ex}.");
                        }
                    }

                    value.Dependencies.EntityAnalysisModelSuppressionModels = shadowEntityAnalysisModelSuppressionList;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Model {key} and Activation Rule Suppression ID added to collection.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncSuppressionAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.Suppression, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
