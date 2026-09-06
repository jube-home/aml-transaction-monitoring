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

namespace Jube.Resources
{
    public sealed class EntityAnalysisModelActivationRuleResources
    {
        public const string PermissionDenied = nameof(PermissionDenied);
        public const string NotAuthenticated = nameof(NotAuthenticated);
        public const string PermissionDeniedApproveByReview = nameof(PermissionDeniedApproveByReview);
        public const string EntityAnalysisModelIdInvalid = nameof(EntityAnalysisModelIdInvalid);
        public const string NameRequired = nameof(NameRequired);
        public const string NameMaxLength = nameof(NameMaxLength);
        public const string NameAlreadyExists = nameof(NameAlreadyExists);
        public const string ReviewStatusIdInvalid = nameof(ReviewStatusIdInvalid);
        public const string RuleScriptTypeIdInvalid = nameof(RuleScriptTypeIdInvalid);
        public const string BuilderRuleScriptRequired = nameof(BuilderRuleScriptRequired);
        public const string BuilderRuleScriptMaxLength = nameof(BuilderRuleScriptMaxLength);
        public const string JsonRequired = nameof(JsonRequired);
        public const string CoderRuleScriptRequired = nameof(CoderRuleScriptRequired);
        public const string CoderRuleScriptMaxLength = nameof(CoderRuleScriptMaxLength);
        public const string CaseWorkflowGuidRequired = nameof(CaseWorkflowGuidRequired);
        public const string CaseWorkflowStatusGuidRequired = nameof(CaseWorkflowStatusGuidRequired);
        public const string CaseKeyRequired = nameof(CaseKeyRequired);
        public const string CaseKeyMaxLength = nameof(CaseKeyMaxLength);
        public const string BypassSuspendSampleRange = nameof(BypassSuspendSampleRange);
        public const string BypassSuspendIntervalInvalid = nameof(BypassSuspendIntervalInvalid);
        public const string BypassSuspendValueRange = nameof(BypassSuspendValueRange);
        public const string ResponseElevationRange = nameof(ResponseElevationRange);
        public const string ResponseElevationContentMaxLength = nameof(ResponseElevationContentMaxLength);
        public const string ResponseElevationRedirectMaxLength = nameof(ResponseElevationRedirectMaxLength);
        public const string ResponseElevationKeyRequired = nameof(ResponseElevationKeyRequired);
        public const string ResponseElevationForeColorRequired = nameof(ResponseElevationForeColorRequired);
        public const string ResponseElevationForeColorInvalid = nameof(ResponseElevationForeColorInvalid);
        public const string ResponseElevationBackColorRequired = nameof(ResponseElevationBackColorRequired);
        public const string ResponseElevationBackColorInvalid = nameof(ResponseElevationBackColorInvalid);
        public const string NotificationTypeIdInvalid = nameof(NotificationTypeIdInvalid);
        public const string NotificationDestinationRequired = nameof(NotificationDestinationRequired);
        public const string NotificationDestinationMaxLength = nameof(NotificationDestinationMaxLength);
        public const string NotificationSubjectMaxLength = nameof(NotificationSubjectMaxLength);
        public const string NotificationBodyMaxLength = nameof(NotificationBodyMaxLength);

        public const string EntityAnalysisModelGuidTtlCounterRequired =
            nameof(EntityAnalysisModelGuidTtlCounterRequired);

        public const string EntityAnalysisModelTtlCounterGuidRequired =
            nameof(EntityAnalysisModelTtlCounterGuidRequired);

        public const string ActivationSampleRange = nameof(ActivationSampleRange);
        public const string PriorityRange = nameof(PriorityRange);
    }
}