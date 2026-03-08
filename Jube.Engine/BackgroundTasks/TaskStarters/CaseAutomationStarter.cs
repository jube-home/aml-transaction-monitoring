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

namespace Jube.Engine.BackgroundTasks.TaskStarters
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Context;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using EntityAnalysisModelInvoke.Models.CaseManagement;

    public class CaseAutomationStarter(Context context)
    {
        public async Task StartAsync()
        {
            try
            {
                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    if (context.Services.Log.IsInfoEnabled)
                    {
                        context.Services.Log.Info("Start: Is building the database connection.");
                    }

                    var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                            context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);
                    try
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                "Case Automation: Has opened the database connection for the case automation server.");
                        }

                        var expiredCases = await GetExpiredCasesPendingAsync(dbContext).ConfigureAwait(false);

                        foreach (var processExpiredCase in expiredCases)
                        {
                            if (context.Services.Log.IsInfoEnabled)
                            {
                                context.Services.Log.Info(
                                    $"Case Automation: Is about to update case id {processExpiredCase.CaseId} with status of {processExpiredCase.CaseId}.");
                            }

                            try
                            {
                                await UpdateCaseInDatabaseAsync(dbContext, processExpiredCase).ConfigureAwait(false);

                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Info(
                                        $"Case Automation: Has updated case id {processExpiredCase.CaseId} with status of {processExpiredCase.CaseId}.  Will now create an audit event including the old value of {processExpiredCase.OldClosedStatus} and a case key of {processExpiredCase.CaseKey} and Case Key Value of {processExpiredCase.CaseKeyValue}.");
                                }

                                await InsertCaseEventAsync(dbContext, processExpiredCase).ConfigureAwait(false);

                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Info(
                                        $"Case Automation: Has created an audit event including the old value of {processExpiredCase.OldClosedStatus} and a case key of {processExpiredCase.CaseKey} and Case Key Value of {processExpiredCase.CaseKeyValue}.");
                                }
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                context.Services.Log.Error(
                                    $"Case Automation: Has created an error while processing expired cases as {ex} for case ID {processExpiredCase.CaseId} and new closed status {processExpiredCase.NewClosedStatus}.");
                            }
                        }

                        await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug("Case Automation: Is waiting.");
                        }

                        await Task.Delay(Int32.Parse(context.Services.DynamicEnvironment.AppSettings("CasesAutomationWait")), context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                        throw;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                        context.Services.Log.Error($"Case Automation: Has created an error inside the loop {ex}");

                        await Task.Delay(Int32.Parse(context.Services.DynamicEnvironment.AppSettings("CasesAutomationWait")), context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation CasesAutomationAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"CasesAutomationAsync: has produced an error {ex}");
            }
        }

        private async Task<IEnumerable<ExpiredCase>> GetExpiredCasesPendingAsync(DbContext dbContext)
        {
            var expiredCases = new List<ExpiredCase>();
            try
            {
                var repository = new CaseRepository(dbContext);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Case Automation: Has instantiated the command object to return all expired cases.");
                }

                var records = await repository.GetByExpiredAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Case Automation: Has executed a reader to return all expired cases.");
                }

                foreach (var record in records)
                {
                    try
                    {
                        var expiredCase = new ExpiredCase
                        {
                            CaseId = record.Id
                        };

                        if (record.CaseKey != null)
                        {
                            expiredCase.CaseKey = record.CaseKey;
                        }

                        if (record.CaseKeyValue != null)
                        {
                            expiredCase.CaseKeyValue = record.CaseKeyValue;
                        }

                        if (record.ClosedStatusId.HasValue)
                        {
                            expiredCase.OldClosedStatus = record.ClosedStatusId.Value;

                            switch (expiredCase.OldClosedStatus)
                            {
                                case 0:
                                    expiredCase.NewClosedStatus = 0;

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info(
                                            $"Case Automation: Case ID {expiredCase.CaseId} has an open status and will be maintained.");
                                    }

                                    break;
                                case 1:
                                    expiredCase.NewClosedStatus = 0;

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info(
                                            $"Case Automation: Case ID {expiredCase.CaseId} has a Suspend Open status and will be changed to Open.");
                                    }

                                    break;
                                case 2:
                                    expiredCase.NewClosedStatus = 3;

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info(
                                            $"Case Automation: Case ID {expiredCase.CaseId} has a Suspend Close status and will be changed to Closed.");
                                    }

                                    break;
                                case 4:
                                    expiredCase.NewClosedStatus = 3;

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info(
                                            $"Case Automation: Case ID {expiredCase.CaseId} has a Suspend Bypass status and will be changed to Closed.");
                                    }

                                    break;
                            }
                        }
                        else
                        {
                            expiredCase.NewClosedStatus = 0;

                            if (context.Services.Log.IsInfoEnabled)
                            {
                                context.Services.Log.Info(
                                    $"Case Automation: Case ID {expiredCase.CaseId} has a missing Close status and will be changed to Open.");
                            }
                        }

                        expiredCases.Add(expiredCase);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Services.Log.Error(
                            $"Case Automation: Has created an error while processing expired cases as {ex}");
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        "Case Automation: Has closed the reader of expired cases and will now process them.");
                }

            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"GetExpiredCasesPending: has produced an error {ex}");
            }

            return expiredCases;
        }

        private async Task InsertCaseEventAsync(DbContext dbContext, ExpiredCase processExpiredCase)
        {
            try
            {
                var repository = new CaseEventRepository(dbContext);

                var model = new CaseEvent
                {
                    CaseKey = processExpiredCase.CaseKey,
                    CaseKeyValue = processExpiredCase.CaseKeyValue,
                    CreatedUser = "Administrator",
                    CaseEventTypeId = 15,
                    Before = processExpiredCase.OldClosedStatus.ToString(),
                    After = processExpiredCase.NewClosedStatus.ToString(),
                    CaseId = processExpiredCase.CaseId
                };

                await repository.InsertAsync(model, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"InsertCaseEventAsync: has produced an error {ex}");
            }
        }

        private async Task UpdateCaseInDatabaseAsync(DbContext dbContext, ExpiredCase processExpiredCase)
        {
            try
            {
                var repository = new CaseRepository(dbContext);

                await repository.UpdateExpiredCaseDiaryAsync(processExpiredCase.CaseId, processExpiredCase.NewClosedStatus,
                    processExpiredCase.OldClosedStatus, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"UpdateCaseInDatabaseAsync: has produced an error {ex}");
            }
        }
    }
}
