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
    using System.Net;
    using System.Threading.Tasks;
    using BackgroundTasks.Context;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;

    public static class LogStartInstanceAsyncExtensions
    {
        public static async Task<Context> LogStartInstanceAsync(this Context context)
        {
            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);

            try
            {
                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: Trying to establish a connection to the Database database.");
                }

                var repository = new EntityAnalysisInstanceRepository(dbContext);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: Established a connection to the Database database.");
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Entity Start: A GUID for this instance has been created and is {context.EntityAnalysisModels.EntityAnalysisInstanceGuid}.");
                }

                var model = new EntityAnalysisInstance
                {
                    Guid = context.EntityAnalysisModels.EntityAnalysisInstanceGuid,
                    Instance = Dns.GetHostName(),
                    CreatedDate = DateTime.Now
                };

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Entity Start: Passing values to record the entity instance starting Entity_Analysis_Instance_GUID {context.EntityAnalysisModels.EntityAnalysisInstanceGuid}; Node {model.Instance};.");
                }

                await repository.InsertAsync(model, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Entity Start: Recorded the entity instance starting in the Database database with a GUID of {context.EntityAnalysisModels.EntityAnalysisInstanceGuid}.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"Entity Start: {ex}");
            }
            finally
            {
                await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: Closed the Database Connection Finally.");
                }
            }

            return context;
        }
    }
}
