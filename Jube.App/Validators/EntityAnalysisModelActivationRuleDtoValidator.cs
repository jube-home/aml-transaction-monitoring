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

namespace Jube.App.Validators
{
    using System.Collections.Generic;
    using Data.Repository;
    using Dto;
    using FluentValidation;

    public class EntityAnalysisModelActivationRuleDtoValidator : AbstractValidator<EntityAnalysisModelActivationRuleDto>
    {
        public EntityAnalysisModelActivationRuleDtoValidator(EntityAnalysisModelActivationRuleRepository repository)
        {
            RuleFor(p => p.Name)
                .NotEmpty()
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository.GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);

                    if (existing == null)
                    {
                        return true;
                    }

                    return existing.Id == dto.Id;
                })
                .WithMessage("This name already exists.");

            RuleFor(p => p.EntityAnalysisModelId).GreaterThan(0);
            RuleFor(p => p.Active).NotNull();
            RuleFor(p => p.Locked).NotNull();

            var reviewStatusTypes = new List<int>
            {
                0,
                1,
                2,
                3,
                4
            };
            RuleFor(p => p.ReviewStatusId)
                .Must(m => reviewStatusTypes.Contains(m));

            RuleFor(p => p.EnableSuppression).NotNull();

            RuleFor(p => p.BuilderRuleScript).NotNull();
            RuleFor(p => p.Json).NotEmpty();
            RuleFor(p => p.CoderRuleScript).NotNull();

            var ruleTypes = new List<int>
            {
                1,
                2
            };
            RuleFor(p => p.RuleScriptTypeId).Must(m => ruleTypes.Contains(m));

            RuleFor(p => p.EnableCaseWorkflow).NotNull();

            RuleFor(p => p.CaseWorkflowGuid).NotEmpty()
                .When(w => w.EnableCaseWorkflow);

            RuleFor(p => p.CaseWorkflowStatusGuid).NotEmpty()
                .When(w => w.EnableCaseWorkflow);

            RuleFor(p => p.CaseKey).NotEmpty()
                .When(w => w.EnableCaseWorkflow);

            RuleFor(p => p.EnableBypass).NotNull()
                .When(w => w.EnableCaseWorkflow);

            RuleFor(p => p.BypassSuspendSample).GreaterThanOrEqualTo(0)
                .When(w => w.EnableCaseWorkflow && w.EnableBypass);

            var bypassSuspendIntervalTypes = new List<char>
            {
                'n',
                'h',
                'd',
                'm'
            };
            RuleFor(p => p.BypassSuspendInterval)
                .Must(m => bypassSuspendIntervalTypes.Contains(m))
                .When(w => w.EnableCaseWorkflow && w.EnableBypass);

            RuleFor(p => p.BypassSuspendValue).GreaterThanOrEqualTo(0)
                .When(w => w.EnableCaseWorkflow && w.EnableBypass);

            RuleFor(p => p.EnableResponseElevation).NotNull();

            RuleFor(p => p.ResponseElevation).GreaterThanOrEqualTo(0)
                .When(w => w.EnableResponseElevation);

            RuleFor(p => p.ResponseElevationKey).NotEmpty()
                .When(w => w.EnableResponseElevation && w.SendToActivationWatcher);

            RuleFor(p => p.ResponseElevationForeColor).NotEmpty()
                .When(w => w.EnableResponseElevation && w.SendToActivationWatcher);

            RuleFor(p => p.ResponseElevationBackColor).NotEmpty()
                .When(w => w.EnableResponseElevation && w.SendToActivationWatcher);

            RuleFor(p => p.EnableNotification).NotNull();

            var notificationTypes = new List<int>
            {
                1,
                2
            };
            RuleFor(p => p.NotificationTypeId)
                .Must(m => notificationTypes.Contains(m))
                .When(w => w.EnableNotification);

            RuleFor(p => p.EnableTtlCounter).NotNull();

            RuleFor(p => p.EntityAnalysisModelGuidTtlCounter)
                .NotEmpty()
                .When(w => w.EnableTtlCounter);

            RuleFor(p => p.EntityAnalysisModelTtlCounterGuid)
                .NotEmpty()
                .When(w => w.EnableTtlCounter);

            RuleFor(p => p.ActivationSample).GreaterThanOrEqualTo(0);

            RuleFor(p => p.EnableReprocessing).NotNull();

            RuleFor(p => p.ReportTable).NotNull();
            RuleFor(p => p.ResponsePayload).NotNull();

            RuleFor(p => p.Priority).GreaterThanOrEqualTo(0);
        }
    }
}
