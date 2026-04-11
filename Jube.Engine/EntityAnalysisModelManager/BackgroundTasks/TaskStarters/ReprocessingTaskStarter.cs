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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Data.Context;
    using Data.Query;
    using Data.Reporting;
    using Data.Repository;
    using Data.SyntaxTree;
    using Dictionary;
    using EntityAnalysisModel;
    using EntityAnalysisModelInvoke;
    using Helpers;
    using Parser;
    using Parser.Compiler;
    using Reprocessing;

    public class ReprocessingTaskStarter(Context context)
    {
        public async Task StartAsync()
        {
            try
            {
                var lastUpdated = default(DateTime);
                var random = new Random(Environment.TickCount ^ Guid.NewGuid().GetHashCode());

                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                        context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);
                    try
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug("Entity Reprocessing:  About to make a database connection.");
                        }

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                "Entity Reprocessing:  Has made a database connection.  Will now proceed to loop around the models and see if there are any reprocessing requests.");
                        }

                        foreach (var modelKvp in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                        {
                            context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                            try
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Reprocessing:  Has found model id {modelKvp.Key}.  Will now check to see if the model has been started.");
                                }

                                if (!modelKvp.Value.Started)
                                {
                                    continue;
                                }

                                var entityAnalysisModelRuleReprocessing = await GetEntityAnalysisModelRuleReprocessingInstanceAsync(dbContext, modelKvp, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                if (!entityAnalysisModelRuleReprocessing.FoundInstance)
                                {
                                    continue;
                                }

                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Info(
                                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} using created date.");
                                }

                                var documentsInitialCounts =
                                    await GetInitialCountsAsync(dbContext, modelKvp.Value.Instance.Guid, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                if (documentsInitialCounts != null)
                                {
                                    var dateRangeAndCount = EstablishProcessingDateRange(
                                        entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance,
                                        documentsInitialCounts);

                                    await UpdateEntityAnalysisModelsReprocessingRuleInstanceReferenceDateCountAsync(dbContext,
                                        entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance, modelKvp.Value.Instance.Guid, dateRangeAndCount.adjustedStartDate, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                    var limit = Int32.Parse(context.Services.DynamicEnvironment.AppSettings("ReprocessingBulkLimit"));

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info(
                                            $"Entity Reprocessing:  Is about to build up the cache filter for instance {entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} reprocessing bulk limit has been set to {limit}.");
                                    }

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info(
                                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has created a filter between {dateRangeAndCount.adjustedStartDate} and {dateRangeAndCount.lastReferenceDate}.");
                                    }

                                    var sampled = 0;
                                    var matched = 0;
                                    var processed = 0;
                                    var errors = 0;
                                    // ReSharper disable once RedundantAssignment
                                    var deleted = false;

                                    using var archiveDatabase = new Postgres(context.Services.ReportConnectionString ?? dbContext.Connection.ConnectionString, context.Services.Log);
                                    do
                                    {
                                        if (context.Services.Log.IsInfoEnabled)
                                        {
                                            context.Services.Log.Info(
                                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is about to run a query on cache to bring back all document for filter,  skipping {processed} and limiting {limit}.");
                                        }

                                        var archivePayloadSelectAndBody = modelKvp.Value.References.ArchivePayloadSqlSelect + " " + modelKvp.Value.References.ArchivePayloadSqlBody;

                                        var documents =
                                            await archiveDatabase.ExecuteReturnPayloadFromArchiveWithSkipLimitAsync(
                                                archivePayloadSelectAndBody, dateRangeAndCount.adjustedStartDate,
                                                processed,
                                                limit, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                        if (documents.Count == 0)
                                        {
                                            break;
                                        }

                                        foreach (var entry in documents)
                                        {
                                            context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                                            try
                                            {
                                                if (context.Services.Log.IsInfoEnabled)
                                                {
                                                    context.Services.Log.Info(
                                                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is about to process document {processed}.");
                                                }

                                                if (entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance
                                                        .ReprocessingSample >= random.NextDouble())
                                                {
                                                    if (context.Services.Log.IsInfoEnabled)
                                                    {
                                                        context.Services.Log.Info(
                                                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} and it has passed a random sample.  Will now test the rule.");
                                                    }

                                                    sampled += 1;

                                                    var entityInstanceEntryDictionaryKvPs =
                                                        new PooledDictionary<string, DictionaryNoBoxing<string>>();

                                                    if (entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance
                                                        .ReprocessingRuleCompileDelegate(entry,
                                                            modelKvp.Value.Dependencies.EntityAnalysisModelLists,
                                                            entityInstanceEntryDictionaryKvPs, context.Services.Log))
                                                    {
                                                        dateRangeAndCount.lastReferenceDate =
                                                            entry[modelKvp.Value.References.ReferenceDateName];

                                                        await InvokeReprocessingForDocumentAsync(modelKvp.Value,
                                                            entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance, processed,
                                                            entry).ConfigureAwait(false);

                                                        matched += 1;
                                                    }
                                                    else
                                                    {
                                                        if (context.Services.Log.IsInfoEnabled)
                                                        {
                                                            context.Services.Log.Info(
                                                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} but it has not passed the rule.");
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (context.Services.Log.IsInfoEnabled)
                                                    {
                                                        context.Services.Log.Info(
                                                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} but it has failed to obtain a random digit and as been sampled out.");
                                                    }
                                                }
                                            }
                                            catch (Exception ex) when (ex is not OperationCanceledException)
                                            {
                                                errors += 1;

                                                if (context.Services.Log.IsInfoEnabled)
                                                {
                                                    context.Services.Log.Info(
                                                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} and has had an error {ex}.");
                                                }
                                            }
                                            finally
                                            {
                                                if (context.Services.Log.IsInfoEnabled)
                                                {
                                                    context.Services.Log.Info(
                                                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has finished processing {processed}.");
                                                }

                                                processed += 1;
                                            }

                                            if (lastUpdated <= DateTime.Now.AddSeconds(-10))
                                            {
                                                if (await LogAndGetTerminateAsync(dbContext,
                                                        entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance,
                                                        processed, sampled, matched, errors,
                                                        dateRangeAndCount.lastReferenceDate, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false))
                                                {
                                                    if (context.Services.Log.IsInfoEnabled)
                                                    {
                                                        context.Services.Log.Info(
                                                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been removed, stopping process.");
                                                    }

                                                    // ReSharper disable once RedundantAssignment
                                                    deleted = true;

                                                    break;
                                                }

                                                lastUpdated = DateTime.Now;
                                            }
                                            else
                                            {
                                                if (context.Services.Log.IsInfoEnabled)
                                                {
                                                    context.Services.Log.Info(
                                                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} not updated database as time interval not passed.");
                                                }
                                            }
                                        }

                                        deleted = await LogAndGetTerminateAsync(dbContext,
                                            entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance,
                                            processed, sampled, matched, errors, dateRangeAndCount.lastReferenceDate, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                                    } while (!deleted);

                                    await FinishReprocessBatchChunkAsync(dbContext,
                                        entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                                }
                                else
                                {
                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info(
                                            $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessing.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} there are no initial counts for model {modelKvp.Key}.");
                                    }
                                }
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                context.Services.Log.Error($"Entity Reprocessing: {ex}");
                            }
                        }

                        if (context.Services.Log.IsInfoEnabled)
                        {
                            context.Services.Log.Info(
                                "Entity Reprocessing: Has finished a cycle and will now sleep for 20 seconds,  the database connection to Database will also be closed.");
                        }

                        await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                        await Task.Delay(20000, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
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

                        context.Services.Log.Error($"Entity Reprocessing: {ex}. Waiting.");

                        await Task.Delay(20000, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation EntityReprocessingAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"EntityReprocessingAsync: Error outside of loop {ex}");
            }
        }

        private async Task<bool> LogAndGetTerminateAsync(DbContext dbContext,
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance, int processed,
            int sampled, int matched, int errors, DateTime referenceDate, CancellationToken token = default)
        {
            var deleted = false;
            try
            {
                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is about to report back status to the database.");
                }

                try
                {
                    var repository = new EntityAnalysisModelReprocessingRuleInstanceRepository(dbContext);

                    await repository.UpdateCountsAsync(
                        entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId,
                        sampled, matched, processed, errors, referenceDate, token).ConfigureAwait(false);
                }
                catch
                {
                    deleted = true;
                }

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has updated database with processed {processed}, Sampled {sampled}, Matched {matched}, Errors{errors}.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"LogAndGetTerminate: has produced an error {ex}");
            }

            return deleted;
        }

        private async Task InvokeReprocessingForDocumentAsync(EntityAnalysisModel entityAnalysisModel,
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance, int processed,
            DictionaryNoBoxing<string> entry)
        {
            try
            {
                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is processing {processed} and matched the rule and will now invoke.");
                }

                await EntityAnalysisModelInvoke.InvokeAsync(entityAnalysisModel, entry, entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId).ConfigureAwait(false);

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has completed the invoke.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"InvokeReprocessingForDocumentAsync: has produced an error {ex}");
            }
        }

        private async Task FinishReprocessBatchChunkAsync(DbContext dbContext,
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance, CancellationToken token = default)
        {
            try
            {
                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} no documents returned so the work is done.  Is about to update the database to show that the process has completed.");
                }

                var repository = new EntityAnalysisModelReprocessingRuleInstanceRepository(dbContext);

                await repository.UpdateCompletedAsync(entityAnalysisModelRuleReprocessingInstance
                    .EntityAnalysisModelsReprocessingRuleInstanceId, token).ConfigureAwait(false);

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} processing completed and database updated.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"FinishReprocessBatchChunk: has produced an error {ex}");
            }
        }

        private async Task UpdateEntityAnalysisModelsReprocessingRuleInstanceReferenceDateCountAsync(DbContext dbContext,
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance,
            Guid entityAnalysisModelGuid, DateTime lastReferenceDate, CancellationToken token = default)
        {
            try
            {
                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is about to make initial counts for monitoring as Reference_Date {lastReferenceDate}.");
                }

                var archiveRepository = new ArchiveRepository(dbContext);
                var allCount = await archiveRepository.GetCountsByReferenceDateAsync(entityAnalysisModelGuid, lastReferenceDate, token).ConfigureAwait(false);

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has returned initial counts of {allCount} for monitoring as Reference_Date {lastReferenceDate} and Available_Count {allCount}.");
                }

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} is about to update initial counts for monitoring as Reference_Date {lastReferenceDate} and Available_Count {allCount}.");
                }

                var repository = new EntityAnalysisModelReprocessingRuleInstanceRepository(dbContext);

                await repository.UpdateReferenceDateCountAsync(
                    entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId,
                    allCount, lastReferenceDate, token).ConfigureAwait(false);

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has updated initial counts for monitoring as Reference Date {lastReferenceDate} and Available Count {allCount}.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"UpdateEntityAnalysisModelsReprocessingRuleInstanceReferenceDateCount: has produced an error {ex}");
            }
        }

        private (DateTime lastReferenceDate, long allCount, DateTime adjustedStartDate) EstablishProcessingDateRange(
            EntityAnalysisModelRuleReprocessingInstance entityAnalysisModelRuleReprocessingInstance,
            GetArchiveRangeAndCountsQuery.Dto ranges)
        {
            var lastReferenceDate = Convert.ToDateTime(ranges.Max);
            var allCount = 0l;
            DateTime adjustedStartDate = default;

            try
            {

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Has found a last reference date of {lastReferenceDate}.");
                }

                var firstReferenceDate = Convert.ToDateTime(ranges.Min);

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Has found a first reference date of {firstReferenceDate}.");
                }

                allCount = ranges.Count;

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Has found counts of  {allCount}.  Will now proceed to adjust the date to create a between range.");
                }

                switch (entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType)
                {
                    case "d":
                        adjustedStartDate =
                            lastReferenceDate.AddDays(entityAnalysisModelRuleReprocessingInstance
                                .ReprocessingIntervalValue * -1);

                        if (context.Services.Log.IsInfoEnabled)
                        {
                            context.Services.Log.Info(
                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched d as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");
                        }

                        break;
                    case "h":
                        adjustedStartDate =
                            lastReferenceDate.AddHours(entityAnalysisModelRuleReprocessingInstance
                                .ReprocessingIntervalValue * -1);

                        if (context.Services.Log.IsInfoEnabled)
                        {
                            context.Services.Log.Info(
                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched h as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");
                        }

                        break;
                    case "n":
                        adjustedStartDate = lastReferenceDate.AddMinutes(entityAnalysisModelRuleReprocessingInstance
                            .ReprocessingIntervalValue * -1);

                        if (context.Services.Log.IsInfoEnabled)
                        {
                            context.Services.Log.Info(
                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched n as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");
                        }

                        break;
                    case "s":
                        adjustedStartDate = lastReferenceDate.AddSeconds(entityAnalysisModelRuleReprocessingInstance
                            .ReprocessingIntervalValue * -1);

                        if (context.Services.Log.IsInfoEnabled)
                        {
                            context.Services.Log.Info(
                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched s as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");
                        }

                        break;
                    case "m":
                        adjustedStartDate =
                            lastReferenceDate.AddMonths(entityAnalysisModelRuleReprocessingInstance
                                .ReprocessingIntervalValue * -1);

                        if (context.Services.Log.IsInfoEnabled)
                        {
                            context.Services.Log.Info(
                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched m as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");
                        }

                        break;
                    case "y":
                        adjustedStartDate =
                            lastReferenceDate.AddYears(entityAnalysisModelRuleReprocessingInstance
                                .ReprocessingIntervalValue * -1);

                        if (context.Services.Log.IsInfoEnabled)
                        {
                            context.Services.Log.Info(
                                $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Is switched y as is specified as {entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}{entityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.  Lower Date for range is {adjustedStartDate}.");
                        }

                        break;
                }

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Reprocessing: Reprocessing instance {entityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} Lower Date for range is {adjustedStartDate}.  Finished getting initial counts.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"EstablishProcessingDateRange: has produced an error {ex}");
            }

            return (lastReferenceDate, allCount, adjustedStartDate);
        }

        private async Task<GetArchiveRangeAndCountsQuery.Dto> GetInitialCountsAsync(DbContext dbContext,
            Guid entityAnalysisModelGuid, CancellationToken token = default)
        {
            var value = default(GetArchiveRangeAndCountsQuery.Dto);
            try
            {
                var getArchiveRangeAndCountsQuery = new GetArchiveRangeAndCountsQuery(dbContext);
                value = await getArchiveRangeAndCountsQuery.ExecuteAsync(entityAnalysisModelGuid, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"GetInitialCountsAsync: has produced an error {ex}");
            }

            return value;
        }

        private async Task<(EntityAnalysisModelRuleReprocessingInstance EntityAnalysisModelRuleReprocessingInstance, bool FoundInstance)> GetEntityAnalysisModelRuleReprocessingInstanceAsync(DbContext dbContext,
            KeyValuePair<int, EntityAnalysisModel> modelKvp, CancellationToken token = default)
        {
            var returnTuple = (EntityAnalysisModelRuleReprocessingInstance: new EntityAnalysisModelRuleReprocessingInstance(), FoundInstance: false);

            try
            {
                var (key, entityAnalysisModel) = modelKvp;
                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  The model has been started,  so we will check to see if there is a reprocessing instance for the model.");
                }

                var query = new GetNextEntityAnalysisModelsReprocessingRuleInstanceQuery(dbContext);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Entity Reprocessing:  Has found model id {key}.  Has executed the reader to find reprocessing instances.");
                }

                var record = await query.ExecuteAsync(key, token).ConfigureAwait(false);

                if (record != null)
                {
                    returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelId = record.EntityAnalysisModelId;

                    returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId
                        = record.Id;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Reprocessing:  Has found model id {key}.  Has found a reprocessing instance id of {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId}.");
                    }

                    if (record.ReprocessingIntervalValue.HasValue)
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue =
                            record.ReprocessingIntervalValue.Value;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalValue to {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.");
                        }
                    }
                    else
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue = 1;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalValue to DEFAULT {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalValue}.");
                        }
                    }

                    if (record.ReprocessingIntervalType != null)
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType =
                            record.ReprocessingIntervalType;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalType to {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}.");
                        }
                    }
                    else
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType = "d";

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingIntervalType to DEFAULT {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingIntervalType}.");
                        }
                    }

                    if (record.ReprocessingSample.HasValue)
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingSample = record.ReprocessingSample.Value;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingSample to {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingSample}.");
                        }
                    }
                    else
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingSample = 0;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set ReprocessingSample to DEFAULT {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingSample}.");
                        }
                    }

                    if (record.RuleScriptTypeId.HasValue)
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId = record.RuleScriptTypeId.Value;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set RuleScriptTypeID to {returnTuple.EntityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId}.");
                        }
                    }
                    else
                    {
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId = 1;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing:  Has found model id {key}.  Has set RuleScriptTypeID to DEFAULT {returnTuple.EntityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId}.");
                        }
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Reprocessing:  Has found model id {key}.  Is loading the rule parser and tokens.");
                    }

                    var parser = new Parser(context.Services.Log, [])
                    {
                        EntityAnalysisModelRequestXPaths = [],
                        EntityAnalysisModelInlineScriptProperties = []
                    };

                    foreach (var entityAnalysisModelRequestXPath in
                             entityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths
                                 .Where(entityAnalysisModelRequestXPath => !parser.EntityAnalysisModelRequestXPaths.ContainsKey(entityAnalysisModelRequestXPath.Name)))
                    {
                        parser.EntityAnalysisModelRequestXPaths.Add(entityAnalysisModelRequestXPath.Name,
                            new EntityAnalysisModelRequestXPath
                            {
                                DataTypeId = entityAnalysisModelRequestXPath.DataTypeId,
                                DefaultValue = entityAnalysisModelRequestXPath.DefaultValue,
                                Cache = entityAnalysisModelRequestXPath.Cache
                            });
                    }

                    foreach (var publicProperty in
                             entityAnalysisModel.Collections.EntityAnalysisModelInlineScripts
                                 .SelectMany(entityAnalysisModelInlineScript
                                     => SyntaxTreeHelpers.GetPublicProperties(entityAnalysisModelInlineScript.InlineScriptCode, entityAnalysisModelInlineScript.LanguageId == 2)))
                    {
                        parser.EntityAnalysisModelInlineScriptProperties.TryAdd(publicProperty.Key, publicProperty.Value);
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Reprocessing:  Has found model id {key}.  Has loaded the rule parser and tokens.");
                    }

                    if (record.BuilderRuleScript != null &&
                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId == 1)
                    {
                        var parsedRule = new ParsedRule
                        {
                            OriginalRuleText = record.BuilderRuleScript,
                            ErrorSpans = []
                        };

                        parsedRule = parser.TranslateFromDotNotation(parsedRule);
                        parsedRule = parser.Parse(parsedRule);

                        if (parsedRule.ErrorSpans.Count == 0)
                        {
                            returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript = parsedRule.ParsedRuleText;

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Reprocessing: {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} set builder script as {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript}.");
                            }
                        }
                    }
                    else if (record.CoderRuleScript != null &&
                             returnTuple.EntityAnalysisModelRuleReprocessingInstance.RuleScriptTypeId == 2)
                    {
                        var parsedRule = new ParsedRule
                        {
                            OriginalRuleText = record.CoderRuleScript,
                            ErrorSpans = []
                        };
                        parsedRule = parser.TranslateFromDotNotation(parsedRule);
                        parsedRule = parser.Parse(parsedRule);

                        if (parsedRule.ErrorSpans.Count == 0)
                        {
                            returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript = parsedRule.ParsedRuleText;

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Reprocessing: {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} set coder script as {returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript}.");
                            }
                        }
                    }

                    var gatewayRuleScript = new StringBuilder();
                    gatewayRuleScript.Append("Imports System.IO\r\n");
                    gatewayRuleScript.Append("Imports log4net\r\n");
                    gatewayRuleScript.Append("Imports System.Net\r\n");
                    gatewayRuleScript.Append("Imports System.Collections.Generic\r\n");
                    gatewayRuleScript.Append("Imports Jube.Dictionary\r\n");
                    gatewayRuleScript.Append("Imports Jube.Dictionary.Extensions\r\n");
                    gatewayRuleScript.Append("Imports System\r\n");
                    gatewayRuleScript.Append("Public Class GatewayRule\r\n");
                    gatewayRuleScript.Append(
                        "Public Shared Function Match(Data As DictionaryNoBoxing(Of String), List As Dictionary(Of String, List(Of String)),KVP As PooledDictionary(Of String, DictionaryNoBoxing(Of String)),Log As ILog) As Boolean\r\n");
                    gatewayRuleScript.Append("Dim Matched As Boolean\r\n");
                    gatewayRuleScript.Append("Try\r\n");
                    gatewayRuleScript.Append(returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleScript + "\r\n");
                    gatewayRuleScript.Append("Catch ex As Exception\r\n");
                    gatewayRuleScript.Append("Log.Info(ex.ToString)\r\n");
                    gatewayRuleScript.Append("End Try\r\n");
                    gatewayRuleScript.Append("Return Matched\r\n");
                    gatewayRuleScript.Append("\r\n");
                    gatewayRuleScript.Append("End Function\r\n");
                    gatewayRuleScript.Append("End Class\r\n");

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} set class wrap as {gatewayRuleScript}.");
                    }

                    var gatewayRuleScriptHash = HashHelper.GetHash(gatewayRuleScript.ToString());

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} and will be checked against the hash cache.");
                    }

                    if (context.Caching.HashCacheAssembly.TryGetValue(gatewayRuleScriptHash, out var value))
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} exists in the hash cache and will be allocated to a delegate.");
                        }

                        returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile =
                            value;
                        var classType =
                            returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile.GetType("GatewayRule");

                        var methodInfo = classType?.GetMethod("Match");
                        if (methodInfo != null)
                        {
                            returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompileDelegate =
                                (EntityAnalysisModelRuleReprocessingInstance.Match)Delegate.CreateDelegate(
                                    typeof(EntityAnalysisModelRuleReprocessingInstance.Match), methodInfo);
                        }

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} exists in the hash cache, has been allocated a to a delegate and placed in a shadow list of gateway rules.");
                        }

                        returnTuple.FoundInstance = true;
                    }
                    else
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has not been found in the hash cache and will now be compiled.");
                        }

                        var codeBase = Assembly.GetExecutingAssembly().Location;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug($"Entity Model Sync: The code base path has been returned as {codeBase}.");
                        }

                        var strPathBinary = Path.GetDirectoryName(codeBase);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug($"Entity Model Sync: The code base path has been returned as {codeBase}.");
                        }

                        var compile = new Compile();
                        compile.CompileCode(gatewayRuleScript.ToString(), context.Services.Log,
                        [
                            Path.Combine(strPathBinary ?? throw new InvalidOperationException(), "log4net.dll"),
                            Path.Combine(strPathBinary, "Jube.Dictionary.dll")
                        ], Compile.Language.Vb);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Model {key} and Gateway Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has now been compiled with {compile.Errors} errors.");
                        }

                        if (compile.Errors == null)
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has now been compiled without error,  a delegate will now be allocated.");
                            }

                            returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile = compile.CompiledAssembly;

                            var classType =
                                returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompile.GetType("GatewayRule");
                            var methodInfo = classType?.GetMethod("Match");
                            if (methodInfo != null)
                            {
                                returnTuple.EntityAnalysisModelRuleReprocessingInstance.ReprocessingRuleCompileDelegate =
                                    (EntityAnalysisModelRuleReprocessingInstance.Match)Delegate.CreateDelegate(
                                        typeof(EntityAnalysisModelRuleReprocessingInstance.Match), methodInfo);
                            }

                            context.Caching.HashCacheAssembly.Add(gatewayRuleScriptHash, compile.CompiledAssembly);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} has now been compiled without error,  a delegate has been allocated,  added to hash cache and added to a shadow list of gateway rules.");
                            }

                            returnTuple.FoundInstance = true;
                        }
                        else
                        {
                            foreach (var compileError in compile.Errors)
                            {
                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} compile error {compileError.GetMessage()}.");
                                }
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Reprocessing: Model {key} and Reprocessing Rule Model {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId} has been hashed to {gatewayRuleScriptHash} failed to load.");
                            }
                        }
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Entity Reprocessing:  Has finished loading reprocessing instance {returnTuple.EntityAnalysisModelRuleReprocessingInstance.EntityAnalysisModelsReprocessingRuleInstanceId}.  Will now proceed to select the counts and date ranges.");
                }

            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"EntityAnalysisModelRuleReprocessingInstance: has produced an error {ex}");
            }

            return returnTuple;
        }
    }
}
