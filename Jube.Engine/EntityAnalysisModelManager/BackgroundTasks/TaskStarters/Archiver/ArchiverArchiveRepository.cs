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

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.Archiver
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Cache;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using DynamicEnvironment;
    using EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using log4net;

    public static class ArchiverArchiveRepository
    {
        public static async Task BulkCopyArchiveBufferAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, ArchiveBuffer bulkInsertMessageBuffer,
            DynamicEnvironment dynamicEnvironment, CacheService cacheService, ILog log, CancellationToken token = default)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        "Database Persist: The bulk copy threshold has been exceeded and the SQL Bulk Copy will be executed. A timer has been started.");
                }

                var dbContext =
                    DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), log);
                dbContext.CommandTimeout = 0;

                if (log.IsInfoEnabled)
                {
                    log.Info("Database Persist: Opened an SQL Bulk Collection via repository.");
                }

                var repositoryArchive = new ArchiveRepository(dbContext);
                try
                {
                    await repositoryArchive.BulkCopyAsync(bulkInsertMessageBuffer.Archive, token).ConfigureAwait(false);

                    await cacheService.CacheWalRepository.FlushWalAsync(tenantRegistryId, entityAnalysisModelGuid,
                        Dns.GetHostName(), bulkInsertMessageBuffer.Archive.Select(s => s.EntityAnalysisModelInstanceEntryGuid).ToArray());

                    bulkInsertMessageBuffer.Archive.Clear();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    bulkInsertMessageBuffer.Archive.Clear();
                    log.Error($"Database Persist:  Archive Bulk Copy failed {ex}.");
                }

                var repositoryArchiveKeys = new ArchiveKeyRepository(dbContext);
                try
                {
                    // ReSharper disable once MethodSupportsCancellation
 #pragma warning disable CA2016
                    await repositoryArchiveKeys.BulkCopyAsync(bulkInsertMessageBuffer.ArchiveKeys).ConfigureAwait(false);
 #pragma warning restore CA2016

                    bulkInsertMessageBuffer.ArchiveKeys.Clear();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    bulkInsertMessageBuffer.ArchiveKeys.Clear();
                    log.Error($"Database Persist:  Archive Keys Bulk Copy failed {ex}.");
                }

                await dbContext.CloseAsync(token).ConfigureAwait(false);
                await dbContext.DisposeAsync(token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info("Database Persist: Closed an SQL Bulk Collection.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"Database Persist: An error has been created on build insert as {ex}");
            }
        }

        public static async Task UpdateArchiveAsync(EntityAnalysisModelInstanceEntryPayload payload,
            string jsonString, DynamicEnvironment dynamicEnvironment, ILog log, CancellationToken token = default)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Database Persist: Database Persist message is valid for storage with Entry GUID of {payload.EntityAnalysisModelInstanceEntryGuid}.  This is being sent for update as it is reprocess.");
            }

            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), log);
            
            try
            {
                var archiveRepository = new ArchiveRepository(dbContext);

                var model = new Archive
                {
                    Json = jsonString,
                    EntityAnalysisModelInstanceEntryGuid = payload.EntityAnalysisModelInstanceEntryGuid,
                    ResponseElevation = payload.ResponseElevation.Value,
                    EntityAnalysisModelActivationRuleId = payload.PrevailingEntityAnalysisModelActivationRuleId,
                    ActivationRuleCount = payload.EntityAnalysisModelActivationRuleCount,
                    EntryKeyValue = payload.EntityInstanceEntryId,
                    EntityAnalysisModelsReprocessingRuleInstanceId = payload.EntityAnalysisModelReprocessingRuleInstanceId
                };

                await archiveRepository.UpdateAsync(model, token).ConfigureAwait(false);

                var archiveKeyRepository = new ArchiveKeyRepository(dbContext);
                var currentArchiveKeyList = new List<ArchiveKey>();
                foreach (var archiveKey in payload.ArchiveKeys)
                {
                    currentArchiveKeyList.Add(archiveKey);
                    archiveKey.EntityAnalysisModelsReprocessingRuleInstanceId = payload.EntityAnalysisModelReprocessingRuleInstanceId;
                    await archiveKeyRepository.UpsertAsync(archiveKey, token).ConfigureAwait(false);
                }
                await archiveKeyRepository.DeleteWhereNotInListAsync(currentArchiveKeyList, payload.EntityAnalysisModelReprocessingRuleInstanceId).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (log.IsInfoEnabled)
                {
                    log.Error($"Database Persist: error processing payload as {ex}.");
                }
            }
            finally
            {
                await dbContext.CloseAsync(token).ConfigureAwait(false);
                await dbContext.DisposeAsync(token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Database Persist: Database Persist message is valid for storage with Entry GUID of {payload.EntityAnalysisModelInstanceEntryGuid}.  Has finished reprocess ");
                }
            }
        }
    }
}
