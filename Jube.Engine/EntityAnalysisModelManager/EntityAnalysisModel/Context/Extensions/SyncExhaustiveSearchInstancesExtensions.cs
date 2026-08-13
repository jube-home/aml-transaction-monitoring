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
    using Accord.Neuro;
    using AutoMapper.Internal;
    using Data.Query;
    using Data.Repository;
    using Exhaustive.Models;
    using Newtonsoft.Json;

    public static class SyncExhaustiveSearchInstancesExtensions
    {
        public static async Task<Context> SyncExhaustiveSearchInstancesAsync(this Context context)
        {
            try
            {
                context.Services.Parser.EntityAnalysisModelsExhaustiveAdaptations = [];

                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose determining adding models created and promoted by the Exhaustive Algorithm.");
                    }

                    var repository = new ExhaustiveSearchInstanceRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing select from Exhaustive Search Instance Repository and entity model key of {key}.");
                    }

                    var records = await repository.GetByEntityAnalysisModelIdOrderByIdAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

                    var shadowEntityAnalysisModelExhaustive = new List<ExhaustiveSearchInstance>();

                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug($"Entity Start: Exhaustive ID {record.Id} returned for model {key}.");
                            }

                            if (record.Active != 1)
                            {
                                continue;
                            }

                            var exhaustive = new ExhaustiveSearchInstance
                            {
                                Id = record.Id,
                                Guid = record.Guid
                            };

                            if (record.Name == null)
                            {
                                exhaustive.Name =
                                    $"Exhaustive_{exhaustive.Id}";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Exhaustive_{exhaustive.Id} set DEFAULT Name as {exhaustive.Name}.");
                                }
                            }
                            else
                            {
                                exhaustive.Name = record.Name.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set Name as {exhaustive.Name}.");
                                }
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                exhaustive.ResponsePayload = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set DEFAULT Name as {exhaustive.ResponsePayload}.");
                                }
                            }
                            else
                            {
                                exhaustive.ResponsePayload = record.ResponsePayload == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set Name as {exhaustive.ResponsePayload}.");
                                }
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                exhaustive.ReportTable = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set DEFAULT Name as {exhaustive.ReportTable}.");
                                }
                            }
                            else
                            {
                                exhaustive.ReportTable = record.ReportTable == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Adaptation {exhaustive.Id} set Name as {exhaustive.ReportTable}.");
                                }
                            }

                            var getExhaustiveSearchInstancePromotedTrialInstanceQuery
                                = await new GetExhaustiveSearchInstancePromotedTrialInstanceByLastActiveQuery(context.Services.DbContext)
                                    .ExecuteAsync(exhaustive.Id, context.Services.CancellationToken).ConfigureAwait(false);

                            if (getExhaustiveSearchInstancePromotedTrialInstanceQuery != null)
                            {
                                try
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Exhaustive, Balance and Currencies: Exhaustive GUID {exhaustive.Id} has received " +
                                            $"the json as {getExhaustiveSearchInstancePromotedTrialInstanceQuery.Json} from the database and will now start to load to Accord.");
                                    }

                                    exhaustive.TopologyNetwork =
                                        JsonConvert.DeserializeObject<ActivationNetwork>
                                        (getExhaustiveSearchInstancePromotedTrialInstanceQuery.Json,
                                            context.JsonSerializationHelper.DeserializeTopologyNetworkJsonSerializerSettings);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Exhaustive, Balance and Currencies: Exhaustive GUID {exhaustive.Id} has deserialized " +
                                            $"json and will add to the Exhaustive");
                                    }

                                    var getExhaustiveSearchInstancePromotedTrialInstanceVariableQuery
                                        = new GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(context.Services.DbContext);

                                    var trialInstanceDto = await getExhaustiveSearchInstancePromotedTrialInstanceVariableQuery
                                        .ExecuteByExhaustiveSearchInstanceTrialInstanceIdAsync(
                                            getExhaustiveSearchInstancePromotedTrialInstanceQuery
                                                .ExhaustiveSearchInstanceTrialInstanceId, context.Services.CancellationToken).ConfigureAwait(false);

                                    foreach (var exhaustiveVariable in
                                             trialInstanceDto.Select(variable =>
                                                 new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                                                 {
                                                     Name = variable.Name,
                                                     ProcessingTypeId = variable.ProcessingTypeId,
                                                     Mean = variable.Mean,
                                                     Sd = variable.StandardDeviation,
                                                     NormalisationTypeId = variable.NormalisationTypeId
                                                 }))
                                    {
                                        exhaustive.NetworkVariablesInOrder.Add(exhaustiveVariable);
                                    }

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Exhaustive GUID {exhaustive.Id} has loaded the byte array to Accord.");
                                    }
                                }
                                catch (Exception ex) when (ex is not OperationCanceledException)
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Exhaustive GUID {exhaustive.Id} has created an error during loading as {ex}.");
                                    }
                                }

                                shadowEntityAnalysisModelExhaustive.Add(exhaustive);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Exhaustive GUID {exhaustive.Id} has added {exhaustive.Name} to shadow collection.");
                                }

                                context.Services.Parser.EntityAnalysisModelsExhaustiveAdaptations.TryAdd(exhaustive.Name);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Exhaustive GUID {exhaustive.Id} has added {exhaustive.Name} to parser.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Exhaustive GUID {exhaustive.Id} has empty json indicating training not concluded.");
                                }
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Exhaustive Id {record.Id} returned for model {key} as created an error as {ex}.");
                        }
                    }

                    value.Collections.ExhaustiveModels = shadowEntityAnalysisModelExhaustive;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncExhaustiveSearchInstancesAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.ExhaustiveSearchInstances, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
