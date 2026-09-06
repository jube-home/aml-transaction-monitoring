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

using System.Text.RegularExpressions;
using FluentValidation;
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelActivationRule;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelActivationRule
{
    public sealed partial class
        EntityAnalysisModelActivationRuleDtoValidator : AbstractValidator<EntityAnalysisModelActivationRuleDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxRuleScriptLength = 65536;
        private const int MaxFreeTextLength = 65536;
        private const int MaxShortTextLength = 512;

        private static readonly int[] allowedReviewStatusIds = [0, 1, 2, 3, 4];
        private static readonly int[] allowedRuleScriptTypeIds = [1, 2];
        private static readonly int[] allowedNotificationTypeIds = [1, 2];
        private static readonly char[] allowedBypassSuspendIntervals = ['n', 'h', 'd', 'm'];

        public EntityAnalysisModelActivationRuleDtoValidator(EntityAnalysisModelActivationRuleRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelActivationRuleResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.ReviewStatusId)
                .Must(m => allowedReviewStatusIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.ReviewStatusIdInvalid])
                .WithErrorCode("ReviewStatusIdInvalid");

            RuleFor(p => p.RuleScriptTypeId)
                .Must(m => allowedRuleScriptTypeIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.RuleScriptTypeIdInvalid])
                .WithErrorCode("RuleScriptTypeIdInvalid");
            
            RuleFor(p => p.BuilderRuleScript)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.BuilderRuleScriptRequired])
                .WithErrorCode("BuilderRuleScriptNotEmpty")
                .MaximumLength(MaxRuleScriptLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelActivationRuleResources.BuilderRuleScriptMaxLength],
                        MaxRuleScriptLength))
                .WithErrorCode("BuilderRuleScriptMaximumLength")
                .When(p => p.RuleScriptTypeId == 1);

            RuleFor(p => p.Json)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.JsonRequired])
                .WithErrorCode("JsonNotEmpty")
                .When(p => p.RuleScriptTypeId == 1);

            RuleFor(p => p.CoderRuleScript)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.CoderRuleScriptRequired])
                .WithErrorCode("CoderRuleScriptNotEmpty")
                .MaximumLength(MaxRuleScriptLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelActivationRuleResources.CoderRuleScriptMaxLength],
                        MaxRuleScriptLength))
                .WithErrorCode("CoderRuleScriptMaximumLength")
                .When(p => p.RuleScriptTypeId == 2);

            RuleFor(p => p.CaseWorkflowGuid)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.CaseWorkflowGuidRequired])
                .WithErrorCode("CaseWorkflowGuidNotEmpty")
                .When(w => w.EnableCaseWorkflow);

            RuleFor(p => p.CaseWorkflowStatusGuid)
                .NotEmpty()
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleResources.CaseWorkflowStatusGuidRequired])
                .WithErrorCode("CaseWorkflowStatusGuidNotEmpty")
                .When(w => w.EnableCaseWorkflow);

            RuleFor(p => p.CaseKey)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.CaseKeyRequired])
                .WithErrorCode("CaseKeyNotEmpty")
                .MaximumLength(MaxShortTextLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelActivationRuleResources.CaseKeyMaxLength],
                        MaxShortTextLength))
                .WithErrorCode("CaseKeyMaximumLength")
                .When(w => w.EnableCaseWorkflow);

            RuleFor(p => p.BypassSuspendSample)
                .InclusiveBetween(0d, 1d)
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.BypassSuspendSampleRange])
                .WithErrorCode("BypassSuspendSampleRange")
                .When(w => w.EnableCaseWorkflow && w.EnableBypass);

            RuleFor(p => p.BypassSuspendInterval)
                .Must(m => allowedBypassSuspendIntervals.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.BypassSuspendIntervalInvalid])
                .WithErrorCode("BypassSuspendIntervalInvalid")
                .When(w => w.EnableCaseWorkflow && w.EnableBypass);

            RuleFor(p => p.BypassSuspendValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.BypassSuspendValueRange])
                .WithErrorCode("BypassSuspendValueRange")
                .When(w => w.EnableCaseWorkflow && w.EnableBypass);

            RuleFor(p => p.ResponseElevation)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.ResponseElevationRange])
                .WithErrorCode("ResponseElevationRange")
                .When(w => w.EnableResponseElevation);

            RuleFor(p => p.ResponseElevationContent)
                .MaximumLength(MaxFreeTextLength)
                .WithMessage(_ =>
                    string.Format(
                        localiser[EntityAnalysisModelActivationRuleResources.ResponseElevationContentMaxLength],
                        MaxFreeTextLength))
                .WithErrorCode("ResponseElevationContentMaximumLength");

            RuleFor(p => p.ResponseElevationRedirect)
                .MaximumLength(MaxShortTextLength)
                .WithMessage(_ =>
                    string.Format(
                        localiser[EntityAnalysisModelActivationRuleResources.ResponseElevationRedirectMaxLength],
                        MaxShortTextLength))
                .WithErrorCode("ResponseElevationRedirectMaximumLength");

            RuleFor(p => p.ResponseElevationKey)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.ResponseElevationKeyRequired])
                .WithErrorCode("ResponseElevationKeyNotEmpty")
                .When(w => w.EnableResponseElevation && w.SendToActivationWatcher);

            RuleFor(p => p.ResponseElevationForeColor)
                .NotEmpty()
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleResources.ResponseElevationForeColorRequired])
                .WithErrorCode("ResponseElevationForeColorNotEmpty")
                .Must(m => string.IsNullOrEmpty(m) || HexColor().IsMatch(m))
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleResources.ResponseElevationForeColorInvalid])
                .WithErrorCode("ResponseElevationForeColorInvalid")
                .When(w => w.EnableResponseElevation && w.SendToActivationWatcher);

            RuleFor(p => p.ResponseElevationBackColor)
                .NotEmpty()
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleResources.ResponseElevationBackColorRequired])
                .WithErrorCode("ResponseElevationBackColorNotEmpty")
                .Must(m => string.IsNullOrEmpty(m) || HexColor().IsMatch(m))
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleResources.ResponseElevationBackColorInvalid])
                .WithErrorCode("ResponseElevationBackColorInvalid")
                .When(w => w.EnableResponseElevation && w.SendToActivationWatcher);

            RuleFor(p => p.NotificationTypeId)
                .Must(m => allowedNotificationTypeIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.NotificationTypeIdInvalid])
                .WithErrorCode("NotificationTypeIdInvalid")
                .When(w => w.EnableNotification);

            RuleFor(p => p.NotificationDestination)
                .NotEmpty()
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleResources.NotificationDestinationRequired])
                .WithErrorCode("NotificationDestinationNotEmpty")
                .MaximumLength(MaxShortTextLength)
                .WithMessage(_ =>
                    string.Format(
                        localiser[EntityAnalysisModelActivationRuleResources.NotificationDestinationMaxLength],
                        MaxShortTextLength))
                .WithErrorCode("NotificationDestinationMaximumLength")
                .When(w => w.EnableNotification);

            RuleFor(p => p.NotificationSubject)
                .MaximumLength(MaxShortTextLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelActivationRuleResources.NotificationSubjectMaxLength],
                        MaxShortTextLength))
                .WithErrorCode("NotificationSubjectMaximumLength");

            RuleFor(p => p.NotificationBody)
                .MaximumLength(MaxFreeTextLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelActivationRuleResources.NotificationBodyMaxLength],
                        MaxFreeTextLength))
                .WithErrorCode("NotificationBodyMaximumLength");

            RuleFor(p => p.EntityAnalysisModelGuidTtlCounter)
                .NotEmpty()
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleResources.EntityAnalysisModelGuidTtlCounterRequired])
                .WithErrorCode("EntityAnalysisModelGuidTtlCounterNotEmpty")
                .When(w => w.EnableTtlCounter);

            RuleFor(p => p.EntityAnalysisModelTtlCounterGuid)
                .NotEmpty()
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleResources.EntityAnalysisModelTtlCounterGuidRequired])
                .WithErrorCode("EntityAnalysisModelTtlCounterGuidNotEmpty")
                .When(w => w.EnableTtlCounter);

            RuleFor(p => p.ActivationSample)
                .InclusiveBetween(0d, 1d)
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.ActivationSampleRange])
                .WithErrorCode("ActivationSampleRange");

            RuleFor(p => p.Priority)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleResources.PriorityRange])
                .WithErrorCode("PriorityRange");
        }

        [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
        private static partial Regex HexColor();
    }
}