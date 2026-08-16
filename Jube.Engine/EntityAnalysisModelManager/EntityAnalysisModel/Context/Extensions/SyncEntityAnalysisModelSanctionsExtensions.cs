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

    public static class SyncEntityAnalysisModelSanctionsExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelSanctionsAsync(this Context context)
        {
            try
            {
                context.Services.Parser.EntityAnalysisModelsSanctions = [];

                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose determining adding Sanctions.");
                    }

                    var repository = new EntityAnalysisModelSanctionRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelSanctionRepository.GetByEntityAnalysisModelId and entity model key of {key}.");
                    }

                    var records = await repository.GetByEntityAnalysisModelIdOrderByIdAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug("Returned all Sanctions from the database.");
                    }

                    var shadowEntityAnalysisModelSanctions = new List<EntityAnalysisModelSanction>();
                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Sanctions ID {record.Id} returned for model {key}.");
                            }

                            if (record.Active != 1)
                            {
                                continue;
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Sanctions ID {record.Id} returned for model {key} is active.");
                            }

                            var entityAnalysisModelSanctions = new EntityAnalysisModelSanction
                            {
                                EntityAnalysisModelSanctionsId = record.Id
                            };

                            if (record.Name == null)
                            {
                                entityAnalysisModelSanctions.Name =
                                    $"Sanctions_Name_{entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId}";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {record.Id} set DEFAULT Name as {entityAnalysisModelSanctions.Name}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelSanctions.Name =
                                    record.Name.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Name as {entityAnalysisModelSanctions.Name}.");
                                }
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                entityAnalysisModelSanctions.ResponsePayload = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Response Payload as {entityAnalysisModelSanctions.ResponsePayload}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelSanctions.ResponsePayload =
                                    record.ResponsePayload.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Response Payload as {entityAnalysisModelSanctions.ResponsePayload}.");
                                }
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                entityAnalysisModelSanctions.ReportTable = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Promote Report Table as {entityAnalysisModelSanctions.ReportTable}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelSanctions.ReportTable = record.ReportTable.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Promote Report Table as {entityAnalysisModelSanctions.ReportTable}.");
                                }
                            }

                            if (!record.CacheValue.HasValue)
                            {
                                entityAnalysisModelSanctions.CacheValue = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Cache Interval Value as {entityAnalysisModelSanctions.CacheValue}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelSanctions.CacheValue = record.CacheValue.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Cache Interval Value as {entityAnalysisModelSanctions.CacheValue}.");
                                }
                            }

                            if (!record.Distance.HasValue)
                            {
                                entityAnalysisModelSanctions.Distance = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Distance as {entityAnalysisModelSanctions.Distance}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelSanctions.Distance = record.Distance.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Distance as {entityAnalysisModelSanctions.Distance}.");
                                }
                            }

                            if (!record.AggregationTypeId.HasValue)
                            {
                                entityAnalysisModelSanctions.AggregationTypeId = 2;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT AggregationTypeId as {entityAnalysisModelSanctions.AggregationTypeId}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelSanctions.AggregationTypeId = record.AggregationTypeId.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set AggregationTypeId as {entityAnalysisModelSanctions.AggregationTypeId}.");
                                }
                            }

                            if (!record.MaxDistanceRatio.HasValue)
                            {
                                entityAnalysisModelSanctions.MaxDistanceRatio = 0.3;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT MaxDistanceRatio as {entityAnalysisModelSanctions.MaxDistanceRatio}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelSanctions.MaxDistanceRatio = record.MaxDistanceRatio.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set MaxDistanceRatio as {entityAnalysisModelSanctions.MaxDistanceRatio}.");
                                }
                            }

                            if (!record.MaxCoverageRatio.HasValue)
                            {
                                entityAnalysisModelSanctions.MaxCoverageRatio = 2;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT AggregationTypeId as {entityAnalysisModelSanctions.MaxCoverageRatio}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelSanctions.MaxCoverageRatio = record.MaxCoverageRatio.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set AggregationTypeId as {entityAnalysisModelSanctions.MaxCoverageRatio}.");
                                }
                            }

                            if (record.CacheInterval == null)
                            {
                                entityAnalysisModelSanctions.CacheInterval = 'd';

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set DEFAULT Distance as {entityAnalysisModelSanctions.CacheInterval}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelSanctions.CacheInterval = record.CacheInterval.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Distance as {entityAnalysisModelSanctions.CacheInterval}.");
                                }
                            }

                            if (record.MultipartStringDataName != null)
                            {
                                entityAnalysisModelSanctions.MultipartStringDataName =
                                    record.MultipartStringDataName.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} set Multipart String Data Name as {entityAnalysisModelSanctions.MultipartStringDataName}.");
                                }
                            }

                            shadowEntityAnalysisModelSanctions.Add(entityAnalysisModelSanctions);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} has been added to a shadow list of Sanctions.");
                            }

                            context.Services.Parser.EntityAnalysisModelsSanctions.TryAdd(entityAnalysisModelSanctions.Name);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model {key} and Sanctions {entityAnalysisModelSanctions.EntityAnalysisModelSanctionsId} has added {entityAnalysisModelSanctions.Name} to context.Parser.");
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Sanctions ID {record.Id} returned for model {key} has created an error as {ex}.");
                        }
                    }


                    value.Collections.EntityAnalysisModelSanctions = shadowEntityAnalysisModelSanctions;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Model {key} has overwritten the current Sanctions with the shadow list of Sanctions.");
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: Completed adding Sanctions to entity models.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelSanctionsAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.Sanctions, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
