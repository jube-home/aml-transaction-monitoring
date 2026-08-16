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

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Context;
    using Data.Context;
    using Data.Query;

    public class AbstractionRuleCachingTaskStarter(Context context)
    {
        public async Task StartAsync()
        {
            try
            {
                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        foreach (var (key, value) in
                                 from modelEntityKvp in context.EntityAnalysisModels.ActiveEntityAnalysisModels
                                 where modelEntityKvp.Value.Started
                                 where AddSearchKeyCacheServerTime(modelEntityKvp.Value.Cache.LastModelSearchKeyCacheWritten) <
                                       DateTime.UtcNow
                                 select modelEntityKvp)
                        {
                            context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                            if (!value.Cache.HasCheckedDatabaseForLastSearchKeyCacheDates)
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Abstraction Rule Caching: The startup routine has not run for model {key} and the last dates will be fetched from the database.");
                                }

                                var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                                    context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);
                                try
                                {
                                    var query =
                                        new GetEntityAnalysisModelsSearchKeyCalculationInstancesLastSearchKeyDates(
                                            dbContext);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Abstraction Rule Caching: Is about to lookup the last date the search keys were run for model {key}.");
                                    }

                                    var records = await query.ExecuteAsync(value.Instance.Guid, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Abstraction Rule Caching: Has executed {key} to look up search keys last executed date.");
                                    }

                                    foreach (var record in records)
                                    {
                                        context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                                        if (record.SearchKey != null)
                                        {
                                            if (record.DistinctFetchToDate.HasValue)
                                            {
                                                value.Dependencies.LastAbstractionRuleCache.Add(
                                                    record.SearchKey ?? String.Empty, record.DistinctFetchToDate.Value);

                                                if (context.Services.Log.IsDebugEnabled)
                                                {
                                                    context.Services.Log.Debug(
                                                        $"Entity Abstraction Rule Caching: Search Key last date for {record.SearchKey} has been added with a Distinct_Fetch_To_Date of {record.DistinctFetchToDate.Value}.");
                                                }
                                            }
                                            else
                                            {
                                                if (context.Services.Log.IsDebugEnabled)
                                                {
                                                    context.Services.Log.Debug(
                                                        $"Entity Abstraction Rule Caching: Search Key last date for {record.SearchKey} is null and being stepped over during last distinct fetch dates.");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (context.Services.Log.IsDebugEnabled)
                                            {
                                                context.Services.Log.Debug(
                                                    "Entity Abstraction Rule Caching: Search Key grouped null and is being stepped over during last distinct fetch dates.");
                                            }
                                        }
                                    }

                                    await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                                    await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Abstraction Rule Caching: Has finished searching for last date the search keys were run for model {key}.");
                                    }

                                    value.Cache.HasCheckedDatabaseForLastSearchKeyCacheDates = true;
                                }
                                catch (OperationCanceledException)
                                {
                                    await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                                    await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                                    await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                    context.Services.Log.Error(
                                        $"Entity Abstraction Rule Caching: Error while fetching the last search key cache dates as {ex}.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Abstraction Rule Caching: The startup of has run before for model {key} and the database is not being checked for the last date on search key cache.");
                                }
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Abstraction Rule Caching: Entity Model {key} is being started.");
                            }

                            await AbstractionRuleCaching.AbstractionRuleCaching.StartAsync(value,
                                context.Services.DynamicEnvironment,
                                context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug($"Entity Abstraction Rule Caching: Entity Model {key} has finished.");
                            }
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        context.Services.Log.Info($"Graceful Cancellation AbstractionRuleCachingAsync: has produced an error {ex}");
                    }
                    catch (Exception ex)
                    {
                        context.Services.Log.Error($"AbstractionRuleCachingAsync: Error outside of loop {ex}");
                    }
                    finally
                    {
                        await Task.Delay(10000, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"AbstractionRuleCachingAsync: has produced an error {ex}");
            }
        }

        private DateTime AddSearchKeyCacheServerTime(DateTime currentDate)
        {
            var value = context.Services.DynamicEnvironment.AppSettings("SearchKeyCacheServerIntervalType") switch
            {
                "n" => currentDate.AddMinutes(
                    Int32.Parse(context.Services.DynamicEnvironment.AppSettings("SearchKeyCacheServerIntervalValue"))),
                "h" => currentDate.AddHours(
                    Int32.Parse(context.Services.DynamicEnvironment.AppSettings("SearchKeyCacheServerIntervalValue"))),
                "d" => currentDate.AddDays(Int32.Parse(context.Services.DynamicEnvironment.AppSettings("SearchKeyCacheServerIntervalValue"))),
                "m" => currentDate.AddMonths(
                    Int32.Parse(context.Services.DynamicEnvironment.AppSettings("SearchKeyCacheServerIntervalValue"))),
                _ => currentDate.AddHours(1)
            };

            return value;
        }
    }
}
