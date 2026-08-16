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

namespace Jube.App.Dto
{
    using System;
    using Interfaces;

    public class EntityAnalysisModelActivationRuleDto : IUpdated
    {
        public int EntityAnalysisModelId { get; set; }
        public string BuilderRuleScript { get; set; }
        public string Json { get; set; }
        public string Name { get; set; }
        public double ResponseElevation { get; set; }
        public Guid CaseWorkflowGuid { get; set; }
        public bool EnableCaseWorkflow { get; set; }
        public bool Active { get; set; }
        public bool Locked { get; set; }
        public Guid EntityAnalysisModelTtlCounterGuid { get; set; }
        public bool ResponsePayload { get; set; }
        public bool EnableTtlCounter { get; set; }
        public string ResponseElevationContent { get; set; }
        public bool SendToActivationWatcher { get; set; }
        public string ResponseElevationForeColor { get; set; }
        public string ResponseElevationBackColor { get; set; }
        public Guid CaseWorkflowStatusGuid { get; set; }
        public double ActivationSample { get; set; }
        public long ActivationCounter { get; set; }
        public DateTimeOffset? ActivationCounterDate { get; set; }
        public long EvaluationCounter { get; set; }
        public string ResponseElevationRedirect { get; set; }
        public byte ReviewStatusId { get; set; }
        public bool ReportTable { get; set; }
        public bool EnableNotification { get; set; }
        public byte NotificationTypeId { get; set; }
        public string NotificationDestination { get; set; }
        public string NotificationSubject { get; set; }
        public string NotificationBody { get; set; }
        public string CoderRuleScript { get; set; }
        public byte RuleScriptTypeId { get; set; }
        public bool EnableResponseElevation { get; set; }
        public string CaseKey { get; set; }
        public bool EnableBypass { get; set; }
        public char BypassSuspendInterval { get; set; }
        public int BypassSuspendValue { get; set; }
        public double BypassSuspendSample { get; set; }
        public bool Visible { get; set; }
        public bool EnableReprocessing { get; set; }
        public bool EnableSuppression { get; set; }
        public Guid EntityAnalysisModelGuidTtlCounter { get; set; }
        public string ResponseElevationKey { get; set; }
        public double Priority { get; set; }
        public int Id { get; set; }
        public string CreatedUser { get; set; }
        public DateTimeOffset? CreatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public int Version { get; set; }
        public string DeletedUser { get; set; }
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
