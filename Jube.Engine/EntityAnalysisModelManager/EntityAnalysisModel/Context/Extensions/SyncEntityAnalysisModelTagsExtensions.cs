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
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;

    public static class SyncEntityAnalysisModelTagsExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelTagsAsync(this Context context)
        {
            try
            {
                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose determining adding models created and adding Tags.");
                    }

                    var repository = new EntityAnalysisModelTagRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug($"Entity Start: Executing fetch of all Tags for entity model {key}.");
                    }

                    var records = await repository.GetByEntityAnalysisModelIdOrderByIdAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

                    var shadowEntityAnalysisModelTags = new List<EntityAnalysisModelTag>();
                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Tag {record.Id} returned for model {key}.");
                            }

                            if (record.Active != 1)
                            {
                                continue;
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Tag ID {record.Id} returned for model {key} is active.");
                            }

                            var entityAnalysisModelTag = new EntityAnalysisModelTag
                            {
                                Id = record.Id
                            };

                            if (record.Name == null)
                            {
                                entityAnalysisModelTag.Name =
                                    $"Tag_{entityAnalysisModelTag.Id}";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Tag_{entityAnalysisModelTag.Id} set DEFAULT Name as {entityAnalysisModelTag.Name}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelTag.Name = record.Name.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set Name as {entityAnalysisModelTag.Name}.");
                                }
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                entityAnalysisModelTag.ResponsePayload = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set DEFAULT Name as {entityAnalysisModelTag.ResponsePayload}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelTag.ResponsePayload = record.ResponsePayload == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set Name as {entityAnalysisModelTag.ResponsePayload}.");
                                }
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                entityAnalysisModelTag.ReportTable = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set DEFAULT Name as {entityAnalysisModelTag.ReportTable}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelTag.ReportTable = record.ReportTable == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} set Name as {entityAnalysisModelTag.ReportTable}.");
                                }
                            }

                            shadowEntityAnalysisModelTags.Add(entityAnalysisModelTag);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model {key} and Tag {entityAnalysisModelTag.Id} has been added to a shadow list of Tags.");
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Tag ID {record.Id} returned for model {key} as created an error as {ex}.");
                        }
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Model {key} and Tag Model  {key} has completed creating the adaptations into a shadow list of adaptations and it will now be allocated the fields in the order that they appeared in model training.");
                    }

                    value.Collections.EntityAnalysisModelTags = shadowEntityAnalysisModelTags;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Model {key} and Adaptations Model  {key} has completed creating the adaptations into a shadow list of adaptations and it has now be allocated the fields in the order that they appeared in model training from the shadow list of these variables.");
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: Completed adding Adaptations and Exhaustive Neural Networks to entity models.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelTagsAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.Tags, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
