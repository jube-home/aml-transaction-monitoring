namespace Jube.Engine.BackgroundTasks.TaskStarters.Case
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Poco;
    using Data.Query;
    using Data.Repository;
    using DynamicEnvironment;
    using EntityAnalysisModelInvoke.Models.CaseManagement;
    using EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Extensions;
    using Helpers;
    using Jube.Case;
    using log4net;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class CaseProcessing
    {
        public static async Task CreateAsync(DynamicEnvironment dynamicEnvironment,
            CreateCase createCase,
            ILog log,
            JsonSerializationHelper jsonSerializationHelper,
            EntityAnalysisModelInstanceEntryPayload payload = null,
            CancellationToken token = default)
        {
            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Case Creation: has received a case creation message with case entry GUID of {createCase.EntityAnalysisModelInstanceEntryGuid}.");
                }

                var repositoryCase = new CaseRepository(dbContext);
                var query = new GetExistingCasePriorityQuery(dbContext);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Case Creation: connection to the database established for case entry GUID of {createCase.EntityAnalysisModelInstanceEntryGuid}.");
                }

                var model = new Case
                {
                    EntityAnalysisModelInstanceEntryGuid = createCase.EntityAnalysisModelInstanceEntryGuid,
                    CaseWorkflowGuid = createCase.CaseWorkflowGuid,
                    CaseWorkflowStatusGuid = createCase.CaseWorkflowStatusGuid,
                    CaseKey = createCase.CaseKey,
                    CaseKeyValue = createCase.CaseKeyValue,
                    Locked = 0,
                    Rating = 0,
                    CreatedDate = DateTime.UtcNow
                };

                if (createCase.SuspendBypass)
                {
                    model.Diary = 1;
                    model.DiaryDate = createCase.SuspendBypassDate;
                    model.ClosedStatusId = 4;
                }
                else
                {
                    model.Diary = 0;
                    model.DiaryDate = createCase.SuspendBypassDate;
                    model.ClosedStatusId = 0;
                    model.DiaryDate = DateTime.UtcNow;
                }

                model.Json = createCase.Json;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Case Creation: Have created a case creation SQL command with Case Entry GUID of {createCase.EntityAnalysisModelInstanceEntryGuid}, " +
                        $"Case Workflow ID of {createCase.CaseWorkflowGuid}, " +
                        $"Case Workflow Status ID of {createCase.CaseWorkflowStatusGuid}, Case Key of {createCase.CaseKeyValue}, " +
                        $"Case XML Bytes {createCase.Json.Length}");
                }

                var existing = await query.ExecuteAsync(model.CaseWorkflowGuid, model.CaseKey, model.CaseKeyValue, token).ConfigureAwait(false);

                var repositoryCasesWorkflowsStatus = new CaseWorkflowStatusRepository(dbContext, createCase.TenantRegistryId);

                CaseWorkflowStatus finalCasesWorkflowsStatus = null;
                if (existing == null)
                {
                    finalCasesWorkflowsStatus =
                        await repositoryCasesWorkflowsStatus.GetByGuidAsync(model.CaseWorkflowStatusGuid, token).ConfigureAwait(false);

                    await repositoryCase.InsertAsync(model, token).ConfigureAwait(false);
                }
                else
                {
                    var existingCasesWorkflowsStatus =
                        await repositoryCasesWorkflowsStatus.GetByGuidAsync(model.CaseWorkflowStatusGuid, token).ConfigureAwait(false);

                    if (existingCasesWorkflowsStatus.Priority < existing.Priority)
                    {
                        finalCasesWorkflowsStatus =
                            await repositoryCasesWorkflowsStatus.GetByGuidAsync(model.CaseWorkflowStatusGuid, token).ConfigureAwait(false);

                        model.Id = existing.CaseId;
                        model.Locked = 0;
                        model.CaseWorkflowStatusGuid = createCase.CaseWorkflowStatusGuid;
                        await repositoryCase.UpdateCaseAsync(model, token).ConfigureAwait(false);
                    }
                }

                var caseBytes = 0;
                if (createCase.Json != null)
                {
                    if (finalCasesWorkflowsStatus != null)
                    {
                        caseBytes = createCase.Json.Length;

                        if (finalCasesWorkflowsStatus.EnableNotification == 1 ||
                            finalCasesWorkflowsStatus.EnableHttpEndpoint == 1)
                        {

                            var context = payload ?? JsonConvert.DeserializeObject<EntityAnalysisModelInstanceEntryPayload>(createCase.Json, jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

                            if (finalCasesWorkflowsStatus.EnableNotification == 1)
                            {
                                var notificationSubject = context.ReplaceTokens(finalCasesWorkflowsStatus.NotificationSubject);
                                var notificationDestination = context.ReplaceTokens(finalCasesWorkflowsStatus.NotificationDestination);
                                var notificationBody = context.ReplaceTokens(finalCasesWorkflowsStatus.NotificationBody);

                                var notification = new Notification(log, dynamicEnvironment);
                                await notification.SendAsync(finalCasesWorkflowsStatus.NotificationTypeId ?? 1,
                                    notificationDestination,
                                    notificationSubject,
                                    notificationBody, token);
                            }

                            if (finalCasesWorkflowsStatus.EnableHttpEndpoint == 1)
                            {
                                var endpoint = context.ReplaceTokens(finalCasesWorkflowsStatus.HttpEndpoint);
                                if (finalCasesWorkflowsStatus.HttpEndpointTypeId == 1)
                                {
                                    await SendHttpEndpoint.PostAsync(endpoint, PreparePostBodyString(model, jsonSerializationHelper.ArchiveJsonSerializer, finalCasesWorkflowsStatus), log);
                                }
                                else
                                {
                                    await SendHttpEndpoint.GetAsync(endpoint, log);
                                }
                            }
                        }
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Case Creation: Executed Case Entry GUID of {createCase.EntityAnalysisModelInstanceEntryGuid}, " +
                        $"Case Workflow ID of {createCase.EntityAnalysisModelInstanceEntryGuid}, " +
                        $"Case Workflow Status ID of {createCase.CaseWorkflowStatusGuid}, " +
                        $"Case Key of {createCase.CaseKeyValue}, " +
                        $"Case JSON Bytes {caseBytes}");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"Case Creation: error processing payload as {ex}.");
            }
            finally
            {
                await dbContext.CloseAsync(token).ConfigureAwait(false);
                await dbContext.DisposeAsync(token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info("Case Creation: closed the database connection.");
                }
            }
        }

        private static string PreparePostBodyString(Case createCase, JsonSerializer jsonSerializer, CaseWorkflowStatus finalCasesWorkflowsStatus, EntityAnalysisModelInstanceEntryPayload payload = null)
        {
            var jObject = JObject.FromObject(createCase, jsonSerializer);

            if (payload == null)
            {
                jObject["context"] = JObject.Parse(createCase.Json);
            }
            else
            {
                jObject["context"] = JObject.FromObject(payload);
            }

            jObject["caseWorkflowStatus"] = finalCasesWorkflowsStatus.Name;
            jObject.Remove("json");
            return jObject.ToString();
        }
    }
}
