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
    using System.Collections.Concurrent;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Cache;
    using Data.Poco;
    using DynamicEnvironment;
    using EntityAnalysisModelInvoke.Models.CaseManagement;
    using EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using Jube.Engine.BackgroundTasks.TaskStarters.Case;
    using Jube.Engine.Helpers;
    using log4net;

    public static class ArchiverProcessing
    {
        public static async Task CaseCreationAndArchiveStorageAsync(EntityAnalysisModelInstanceEntryPayload payload,
            JsonSerializationHelper jsonSerializationHelper,
            ArchiveBuffer bulkInsertMessageBuffer,
            ConcurrentQueue<CreateCase> pendingCases,
            DynamicEnvironment dynamicEnvironment,
            CacheService cacheService,
            ILog log,
            CancellationToken token = default)
        {
            try
            {
                if (payload.ArchiveJson.Length == 0)
                {
                    payload.ArchiveJson = BuildJsonResponses.BuildFullJson(payload, jsonSerializationHelper.ArchiveJsonSerializer);
                }

                var jsonString = Encoding.UTF8.GetString(payload.ArchiveJson);

                if (payload.CreateCase != null)
                {
                    if (payload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
                    {
                        await CaseProcessing.CreateAsync(dynamicEnvironment, payload.CreateCase, log, jsonSerializationHelper, payload, token);
                        // ReSharper disable once RedundantAssignment
                        payload.CreateCase = null;
                    }
                    else
                    {
                        payload.CreateCase.Json = jsonString;
                        pendingCases.Enqueue(payload.CreateCase);
                    }
                }

                if (!payload.EnableRdbmsArchive)
                {
                    return;
                }

                if (payload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
                {
                    await ArchiverArchiveRepository.UpdateArchiveAsync(payload, jsonString, dynamicEnvironment, log, token)
                        .ConfigureAwait(false);
                }

                else if (bulkInsertMessageBuffer is null)
                {
                    log.Error("Database Persist: Not implemented bulkInsertMessageBuffer is null.");
                }
                else
                {
                    DataTableInsertToBuffer(bulkInsertMessageBuffer, payload, jsonString, log);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"Database Persist: General exception in processing {ex}.");
            }
            finally
            {
                // ReSharper disable once RedundantAssignment
                payload = null;
            }
        }

        private static void DataTableInsertToBuffer(ArchiveBuffer bulkInsertMessageBuffer,
            EntityAnalysisModelInstanceEntryPayload payload, string jsonString, ILog log)
        {
            try
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Database Persist: Database Persist message is valid for storage with Entry GUID of {payload.EntityAnalysisModelInstanceEntryGuid}.  This is being sent for bulk insert. " +
                        $"Database Persist: The flag to promote report table has been set for this model,  will now check columns are available and add the record to the data table.");
                }

                var model = new Archive
                {
                    Json = jsonString,
                    EntityAnalysisModelInstanceEntryGuid = payload.EntityAnalysisModelInstanceEntryGuid,
                    ResponseElevation = payload.ResponseElevation.Value,
                    EntityAnalysisModelActivationRuleId = payload.PrevailingEntityAnalysisModelActivationRuleId,
                    EntityAnalysisModelId = payload.EntityAnalysisModelId,
                    ActivationRuleCount = payload.EntityAnalysisModelActivationRuleCount,
                    EntryKeyValue = payload.EntityInstanceEntryId,
                    ReferenceDate = payload.ReferenceDate,
                    CreatedDate = DateTime.UtcNow,
                    Version = 1
                };

                bulkInsertMessageBuffer.Archive.Add(model);

                foreach (var reportDatabaseValue in payload.ArchiveKeys)
                {
                    bulkInsertMessageBuffer.ArchiveKeys.Add(reportDatabaseValue);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"Database Persist: An error has occurred as {ex}");
            }
        }
    }
}
