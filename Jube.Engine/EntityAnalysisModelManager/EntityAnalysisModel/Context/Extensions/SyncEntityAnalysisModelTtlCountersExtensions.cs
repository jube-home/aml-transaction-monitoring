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

    public static class SyncEntityAnalysisModelTtlCountersExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelTtlCountersAsync(this Context context)
        {
            try
            {
                context.Services.Parser.EntityAnalysisModelsTtlCounters = [];

                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose determining adding TTL Counters.");
                    }

                    var repository = new EntityAnalysisModelTtlCounterRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelTtlCounterRepository.GetByEntityAnalysisModelId and entity model key of {key}.");
                    }

                    var records = await repository.GetByEntityAnalysisModelIdOrderByIdAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

                    var shadowEntityAnalysisModelTtlCounters = new List<EntityAnalysisModelTtlCounter>();
                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: TTL Counter ID {record.Id} returned for model {key}.");
                            }

                            if (record.Active != 1)
                            {
                                continue;
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: TTL Counter ID {record.Id} returned for model {key} is active.");
                            }

                            var entityAnalysisModelTtlCounter = new EntityAnalysisModelTtlCounter
                            {
                                Id = record.Id
                            };

                            if (record.Guid != Guid.Empty)
                            {
                                entityAnalysisModelTtlCounter.Guid = record.Guid;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set Guid as {entityAnalysisModelTtlCounter.Guid}.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Cross Model Abstraction {entityAnalysisModelTtlCounter.Id} has a missing guid.");
                                }
                            }

                            if (record.Name == null)
                            {
                                entityAnalysisModelTtlCounter.Name =
                                    $"TTL_Counter_{entityAnalysisModelTtlCounter.Id}";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set DEFAULT Name as {entityAnalysisModelTtlCounter.Name}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.Name = record.Name.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Cross Model Abstraction {entityAnalysisModelTtlCounter.Id} set Name as {entityAnalysisModelTtlCounter.Name}.");
                                }
                            }

                            if (record.TtlCounterDataName == null)
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set DEFAULT TTL Counter Data Name as {entityAnalysisModelTtlCounter.TtlCounterDataName}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.TtlCounterDataName =
                                    record.TtlCounterDataName.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Cross Model Abstraction {entityAnalysisModelTtlCounter.Id} set TTL Counter Data Name as {entityAnalysisModelTtlCounter.TtlCounterDataName}.");
                                }
                            }

                            if (!record.EnableSum.HasValue)
                            {
                                entityAnalysisModelTtlCounter.EnableSum = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Enable Live Forever {entityAnalysisModelTtlCounter.Id} set DEFAULT Enable Sum as {entityAnalysisModelTtlCounter.EnableSum}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.EnableSum = record.EnableSum.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Enable Live Forever {entityAnalysisModelTtlCounter.Id} set Enable Sum as {entityAnalysisModelTtlCounter.EnableSum}.");
                                }
                            }

                            if (record.TtlCounterDataValue == null)
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set DEFAULT TTL Counter Data Value as {entityAnalysisModelTtlCounter.TtlCounterDataValue}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.TtlCounterDataValue = record.TtlCounterDataValue.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Cross Model Abstraction {entityAnalysisModelTtlCounter.Id} set TTL Counter Data Value as {entityAnalysisModelTtlCounter.TtlCounterDataValue}.");
                                }
                            }

                            if (record.ResolutionInterval == null)
                            {
                                entityAnalysisModelTtlCounter.ResolutionInterval = "d";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set DEFAULT Resolution Interval as {entityAnalysisModelTtlCounter.ResolutionInterval}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.ResolutionInterval = record.ResolutionInterval;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set Resolution Interval as {entityAnalysisModelTtlCounter.ResolutionInterval}.");
                                }
                            }

                            if (record.TtlCounterInterval == null)
                            {
                                entityAnalysisModelTtlCounter.TtlCounterInterval = "d";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set DEFAULT Name as {entityAnalysisModelTtlCounter.TtlCounterInterval}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.TtlCounterInterval = record.TtlCounterInterval;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Model {key} and TTL Counter {entityAnalysisModelTtlCounter.Id} set Name as {entityAnalysisModelTtlCounter.TtlCounterInterval}.");
                                }
                            }

                            if (!record.TtlCounterValue.HasValue)
                            {
                                entityAnalysisModelTtlCounter.TtlCounterValue = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set DEFAULT Name as {entityAnalysisModelTtlCounter.TtlCounterValue}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.TtlCounterValue = record.TtlCounterValue.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set Name as {entityAnalysisModelTtlCounter.TtlCounterValue}.");
                                }
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                entityAnalysisModelTtlCounter.ResponsePayload = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set DEFAULT Response Payload as {entityAnalysisModelTtlCounter.ResponsePayload}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.ResponsePayload = record.ResponsePayload.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set Response Payload as {entityAnalysisModelTtlCounter.ResponsePayload}.");
                                }
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                entityAnalysisModelTtlCounter.ReportTable = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set DEFAULT Promote Report Table as {entityAnalysisModelTtlCounter.ReportTable}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.ReportTable = record.ReportTable == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} set Promote Report Table as {entityAnalysisModelTtlCounter.ReportTable}.");
                                }
                            }

                            if (!record.OnlineAggregation.HasValue)
                            {
                                entityAnalysisModelTtlCounter.OnlineAggregation = true;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Online Aggregation {entityAnalysisModelTtlCounter.Id} set DEFAULT Online Aggregation as {entityAnalysisModelTtlCounter.OnlineAggregation}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.OnlineAggregation = record.OnlineAggregation == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Online Aggregation {entityAnalysisModelTtlCounter.Id} set Online Aggregation as {entityAnalysisModelTtlCounter.OnlineAggregation}.");
                                }
                            }

                            if (!record.EnableLiveForever.HasValue)
                            {
                                entityAnalysisModelTtlCounter.EnableLiveForever = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Enable Live Forever {entityAnalysisModelTtlCounter.Id} set DEFAULT Live Forever as {entityAnalysisModelTtlCounter.EnableLiveForever}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelTtlCounter.EnableLiveForever = record.EnableLiveForever.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Enable Live Forever {entityAnalysisModelTtlCounter.Id} set Live Forever as {entityAnalysisModelTtlCounter.EnableLiveForever}.");
                                }
                            }

                            shadowEntityAnalysisModelTtlCounters.Add(entityAnalysisModelTtlCounter);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} has been added to a shadow collection of TTL Counters.");
                            }

                            context.Services.Parser.EntityAnalysisModelsTtlCounters.TryAdd(entityAnalysisModelTtlCounter.Name);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model {key} and TTL Counter Interval {entityAnalysisModelTtlCounter.Id} has {entityAnalysisModelTtlCounter.Name} to context.Parser.");
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: TTL Counter ID {record.Id} returned for model {key} has created an error as {ex}.");
                        }
                    }

                    value.Collections.ModelTtlCounters = shadowEntityAnalysisModelTtlCounters;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Model {key} and TTL Counter Interval has been added to a shadow collection of TTL Counters.");
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: Completed adding TTL Counters to entity models.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelTtlCountersAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.TtlCounters, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
