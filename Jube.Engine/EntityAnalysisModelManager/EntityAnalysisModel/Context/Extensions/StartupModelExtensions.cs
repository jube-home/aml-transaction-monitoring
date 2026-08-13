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
    using System.Threading.Tasks;
    using Data.Poco;
    using Data.Repository;

    public static class StartupModelExtensions
    {
        public static async Task<Context> StartupModelAsync(this Context context)
        {
            try
            {
                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose of starting the thread.");
                    }

                    if (context.EntityAnalysisModels.ActiveEntityAnalysisModels[key].Started)
                    {
                        continue;
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug($"Entity Start: Checking if model {key} is not started.");
                    }

                    value.Instance.EntityAnalysisModelInstanceGuid = Guid.NewGuid();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug($"Entity Start: Checking if model {key} has now been started.");
                    }

                    var repository = new EntityAnalysisModelInstanceRepository(context.Services.DbContext);

                    var model = new EntityAnalysisModelInstance
                    {
                        CreatedDate = DateTime.UtcNow,
                        EntityAnalysisInstanceGuid = context.EntityAnalysisModels.EntityAnalysisInstanceGuid,
                        EntityAnalysisModelInstanceGuid = value.Instance.EntityAnalysisModelInstanceGuid,
                        EntityAnalysisModelId = value.Instance.Id
                    };

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Recording that model {key} with Created Date {model.CreatedDate}, Entity_Analysis_Model_GUID {context.EntityAnalysisModels.ActiveEntityAnalysisModels[key].Instance.Guid}, Entity_Analysis_Model_Instance_GUID {context.EntityAnalysisModels.ActiveEntityAnalysisModels[key].Instance.EntityAnalysisModelInstanceGuid}, Entity_Analysis_Instance_GUID {model.EntityAnalysisInstanceGuid} has now been started.");
                    }

                    await repository.InsertAsync(model, context.Services.CancellationToken).ConfigureAwait(false);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Started model {key} with Started_Date {model.CreatedDate}, Entity_Analysis_Model_GUID {context.EntityAnalysisModels.ActiveEntityAnalysisModels[key].Instance.Guid}, Entity_Analysis_Model_Instance_GUID {context.EntityAnalysisModels.ActiveEntityAnalysisModels[key].Instance.EntityAnalysisModelInstanceGuid}, Entity_Analysis_Instance_GUID {model.EntityAnalysisInstanceGuid} has now been started.");
                    }

                    value.Started = true;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug($"Entity Start: has started {value.Instance.Id}.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"StartupModelAsync: has produced an error {ex}");
            }

            return context;
        }
    }
}
