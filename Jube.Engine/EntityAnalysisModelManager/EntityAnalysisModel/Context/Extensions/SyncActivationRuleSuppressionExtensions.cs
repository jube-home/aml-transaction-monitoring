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

    public static class SyncActivationRuleSuppressionExtensions
    {
        public static async Task<Context> SyncActivationRuleSuppressionAsync(this Context context)
        {
            try
            {
                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose determining adding Suppression Activation Rules.");
                    }

                    var repository = new EntityAnalysisModelActivationRuleSuppressionRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelActivationRuleSuppressionRepository.GetByEntityAnalysisModelId for and entity model key of {key}.");
                    }

                    var records = await repository.GetByEntityAnalysisModelGuidOrderByIdAsync(value.Instance.Guid, context.Services.CancellationToken).ConfigureAwait(false);

                    var shadowEntityAnalysisModelSuppressionDictionary =
                        new Dictionary<string, Dictionary<string, List<string>>>();

                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Suppression ID {record.Id} returned for model {key}.");

                            if (record.SuppressionKey == null)
                            {
                                continue;
                            }

                            var suppressionDictionary = new Dictionary<string, List<string>>();
                            if (record.SuppressionKeyValue == null)
                            {
                                continue;
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Checking to see if there is a shadow collection for suppression value {record.Id} for Entity Model ID {key}.");
                            }

                            if (!suppressionDictionary.ContainsKey(record.SuppressionKeyValue ?? String.Empty))
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: No shadow collection for suppression value {record.Id} for Entity Model ID {key} so it is being created.");
                                }

                                suppressionDictionary.Add(record.SuppressionKeyValue ?? String.Empty,
                                    []);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: checking for collection for suppression value {record.Id} and activation rule name {record.EntityAnalysisModelActivationRuleName} for Entity Model ID {key} so it is being created.");
                                }

                                if (!suppressionDictionary[
                                            record.SuppressionKeyValue ?? String.Empty]
                                        .Contains(record.EntityAnalysisModelActivationRuleName))
                                {
                                    suppressionDictionary[
                                            record.SuppressionKeyValue ?? String.Empty]
                                        .Add(record.EntityAnalysisModelActivationRuleName);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: added to collection for suppression value {record.Id} and activation rule name {record.EntityAnalysisModelActivationRuleName} for Entity Model ID {key} so it is being created.");
                                    }
                                }
                                else
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: was not added to collection for suppression value {record.Id} and activation rule name {record.EntityAnalysisModelActivationRuleName} for Entity Model ID {key} as it already exists.");
                                    }
                                }
                            }
                            else
                            {
                                if (!suppressionDictionary[
                                            record.SuppressionKeyValue ?? String.Empty]
                                        .Contains(record.EntityAnalysisModelActivationRuleName))
                                {
                                    suppressionDictionary[
                                            record.SuppressionKeyValue ?? String.Empty]
                                        .Add(record.EntityAnalysisModelActivationRuleName);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: added to collection for suppression value {record.Id} and activation rule name {record.EntityAnalysisModelActivationRuleName} for Entity Model ID {key} so it is being created.");
                                    }
                                }
                            }

                            if (!shadowEntityAnalysisModelSuppressionDictionary.TryAdd(record.SuppressionKey,
                                    suppressionDictionary))
                            {
                                shadowEntityAnalysisModelSuppressionDictionary[record.SuppressionKey] =
                                    suppressionDictionary;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Suppression Key Value as {record.SuppressionKey} and already exists in collection,  added to key.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Suppression Activation Rule ID  {record.Id} set Suppression Key Value as {record.SuppressionKey} and already exists in collection,  added to key.");
                                }
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Suppression ID {record.Id} returned for model {key} is in error with {ex}.");
                        }
                    }

                    value.Dependencies.EntityAnalysisModelSuppressionRules = shadowEntityAnalysisModelSuppressionDictionary;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Model {key} and suppression Model  {key} added to collection.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncActivationRuleSuppressionAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.ActivationRuleSuppression, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
