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

using Jube.Dto.EntityAnalysisModelActivationRule;

namespace Jube.Service.EntityAnalysisModelActivationRule
{
    using ActivationRulePoco = Data.Poco.EntityAnalysisModelActivationRule;

    internal static class EntityAnalysisModelActivationRuleMapper
    {
        public static EntityAnalysisModelActivationRuleDto? ToDto(ActivationRulePoco? activationRule)
        {
            return activationRule is null
                ? null
                : new EntityAnalysisModelActivationRuleDto
                {
                    Id = activationRule.Id,
                    EntityAnalysisModelId = activationRule.EntityAnalysisModelId.GetValueOrDefault(),
                    Name = activationRule.Name,
                    Active = activationRule.Active == 1,
                    Locked = activationRule.Locked == 1,
                    Priority = activationRule.Priority.GetValueOrDefault(),
                    ReviewStatusId = activationRule.ReviewStatusId.GetValueOrDefault(),
                    RuleScriptTypeId = activationRule.RuleScriptTypeId.GetValueOrDefault(),
                    BuilderRuleScript = activationRule.BuilderRuleScript,
                    Json = activationRule.Json,
                    CoderRuleScript = activationRule.CoderRuleScript,
                    EnableSuppression = activationRule.EnableSuppression == 1,
                    EnableCaseWorkflow = activationRule.EnableCaseWorkflow == 1,
                    CaseWorkflowGuid = activationRule.CaseWorkflowGuid,
                    CaseWorkflowStatusGuid = activationRule.CaseWorkflowStatusGuid,
                    CaseKey = activationRule.CaseKey,
                    EnableBypass = activationRule.EnableBypass == 1,
                    BypassSuspendSample = activationRule.BypassSuspendSample.GetValueOrDefault(),
                    BypassSuspendInterval = activationRule.BypassSuspendInterval.GetValueOrDefault(),
                    BypassSuspendValue = activationRule.BypassSuspendValue.GetValueOrDefault(),
                    EnableResponseElevation = activationRule.EnableResponseElevation == 1,
                    ResponseElevation = activationRule.ResponseElevation.GetValueOrDefault(),
                    ResponseElevationContent = activationRule.ResponseElevationContent,
                    ResponseElevationRedirect = activationRule.ResponseElevationRedirect,
                    SendToActivationWatcher = activationRule.SendToActivationWatcher == 1,
                    ResponseElevationKey = activationRule.ResponseElevationKey,
                    ResponseElevationForeColor = activationRule.ResponseElevationForeColor,
                    ResponseElevationBackColor = activationRule.ResponseElevationBackColor,
                    EnableNotification = activationRule.EnableNotification == 1,
                    NotificationTypeId = activationRule.NotificationTypeId.GetValueOrDefault(),
                    NotificationDestination = activationRule.NotificationDestination,
                    NotificationSubject = activationRule.NotificationSubject,
                    NotificationBody = activationRule.NotificationBody,
                    EnableTtlCounter = activationRule.EnableTtlCounter == 1,
                    EntityAnalysisModelGuidTtlCounter = activationRule.EntityAnalysisModelGuidTtlCounter,
                    EntityAnalysisModelTtlCounterGuid = activationRule.EntityAnalysisModelTtlCounterGuid,
                    ActivationSample = activationRule.ActivationSample.GetValueOrDefault(),
                    Visible = activationRule.Visible == 1,
                    EnableReprocessing = activationRule.EnableReprocessing == 1,
                    ReportTable = activationRule.ReportTable == 1,
                    ResponsePayload = activationRule.ResponsePayload == 1,
                    EvaluationCounter = activationRule.EvaluationCounter.GetValueOrDefault(),
                    ActivationCounter = activationRule.ActivationCounter.GetValueOrDefault(),
                    ActivationCounterDate = ToOffset(activationRule.ActivationCounterDate),
                    CreatedUser = activationRule.CreatedUser,
                    CreatedDate = ToOffset(activationRule.CreatedDate),
                    UpdatedUser = activationRule.UpdatedUser,
                    UpdatedDate = ToOffset(activationRule.UpdatedDate),
                    Version = activationRule.Version.GetValueOrDefault(),
                    DeletedUser = activationRule.DeletedUser,
                    DeletedDate = ToOffset(activationRule.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelActivationRuleDto> ToDto(IEnumerable<ActivationRulePoco>? source)
        {
            return (source ?? Enumerable.Empty<ActivationRulePoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static ActivationRulePoco ToPoco(EntityAnalysisModelActivationRuleDto dto)
        {
            return new ActivationRulePoco
            {
                Id = dto.Id,
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                Priority = dto.Priority,
                ReviewStatusId = (byte)dto.ReviewStatusId,
                RuleScriptTypeId = (byte)dto.RuleScriptTypeId,
                BuilderRuleScript = dto.BuilderRuleScript,
                // The Json column is jsonb -- an empty string (sent when RuleScriptTypeId selects the Coder
                // surface, which doesn't populate Json) is not valid JSON, so it must go through as null.
                Json = string.IsNullOrWhiteSpace(dto.Json) ? null : dto.Json,
                CoderRuleScript = dto.CoderRuleScript,
                EnableSuppression = (byte)(dto.EnableSuppression ? 1 : 0),
                EnableCaseWorkflow = (byte)(dto.EnableCaseWorkflow ? 1 : 0),
                CaseWorkflowGuid = dto.CaseWorkflowGuid,
                CaseWorkflowStatusGuid = dto.CaseWorkflowStatusGuid,
                CaseKey = dto.CaseKey,
                EnableBypass = (byte)(dto.EnableBypass ? 1 : 0),
                BypassSuspendSample = dto.BypassSuspendSample,
                BypassSuspendInterval = dto.BypassSuspendInterval,
                BypassSuspendValue = dto.BypassSuspendValue,
                EnableResponseElevation = (byte)(dto.EnableResponseElevation ? 1 : 0),
                ResponseElevation = dto.ResponseElevation,
                ResponseElevationContent = dto.ResponseElevationContent,
                ResponseElevationRedirect = dto.ResponseElevationRedirect,
                SendToActivationWatcher = (byte)(dto.SendToActivationWatcher ? 1 : 0),
                ResponseElevationKey = dto.ResponseElevationKey,
                ResponseElevationForeColor = dto.ResponseElevationForeColor,
                ResponseElevationBackColor = dto.ResponseElevationBackColor,
                EnableNotification = (byte)(dto.EnableNotification ? 1 : 0),
                NotificationTypeId = (byte)dto.NotificationTypeId,
                NotificationDestination = dto.NotificationDestination,
                NotificationSubject = dto.NotificationSubject,
                NotificationBody = dto.NotificationBody,
                EnableTtlCounter = (byte)(dto.EnableTtlCounter ? 1 : 0),
                EntityAnalysisModelGuidTtlCounter = dto.EntityAnalysisModelGuidTtlCounter,
                EntityAnalysisModelTtlCounterGuid = dto.EntityAnalysisModelTtlCounterGuid,
                ActivationSample = dto.ActivationSample,
                Visible = (byte)(dto.Visible ? 1 : 0),
                EnableReprocessing = (byte)(dto.EnableReprocessing ? 1 : 0),
                ReportTable = (byte)(dto.ReportTable ? 1 : 0),
                ResponsePayload = (byte)(dto.ResponsePayload ? 1 : 0),
                // These two counters and their date are also mapped by the legacy AutoMapper configuration this
                // replaces, which has no Ignore() for them -- so, as before, a Create/Update payload that doesn't
                // round-trip them (the page's GetData() never populates them) resets them. See report.md.
                EvaluationCounter = dto.EvaluationCounter,
                ActivationCounter = dto.ActivationCounter,
                ActivationCounterDate = dto.ActivationCounterDate?.UtcDateTime
            };
        }

        private static DateTimeOffset? ToOffset(DateTime? value)
        {
            return value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
        }
    }
}