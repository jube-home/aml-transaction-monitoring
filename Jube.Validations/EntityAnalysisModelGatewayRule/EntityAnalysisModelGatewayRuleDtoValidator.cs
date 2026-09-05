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

using FluentValidation;
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelGatewayRule;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelGatewayRule
{
    public sealed class
        EntityAnalysisModelGatewayRuleDtoValidator : AbstractValidator<EntityAnalysisModelGatewayRuleDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxRuleScriptLength = 65536;

        private static readonly int[] allowedRuleScriptTypeIds = [1, 2];

        public EntityAnalysisModelGatewayRuleDtoValidator(EntityAnalysisModelGatewayRuleRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[EntityAnalysisModelGatewayRuleResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelGatewayRuleResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelGatewayRuleResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelGatewayRuleResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            // RuleScriptTypeId selects the rule's authoring surface (1 = Builder, 2 = Coder) and the two are
            // mutually exclusive: Builder authors BuilderRuleScript/Json, Coder authors CoderRuleScript. Only the
            // selected surface's script is required.
            RuleFor(p => p.BuilderRuleScript)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelGatewayRuleResources.BuilderRuleScriptRequired])
                .WithErrorCode("BuilderRuleScriptNotEmpty")
                .MaximumLength(MaxRuleScriptLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelGatewayRuleResources.BuilderRuleScriptMaxLength],
                        MaxRuleScriptLength))
                .WithErrorCode("BuilderRuleScriptMaximumLength")
                .When(p => p.RuleScriptTypeId == 1);

            RuleFor(p => p.Json)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelGatewayRuleResources.JsonRequired])
                .WithErrorCode("JsonNotEmpty")
                .When(p => p.RuleScriptTypeId == 1);

            RuleFor(p => p.CoderRuleScript)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelGatewayRuleResources.CoderRuleScriptRequired])
                .WithErrorCode("CoderRuleScriptNotEmpty")
                .MaximumLength(MaxRuleScriptLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelGatewayRuleResources.CoderRuleScriptMaxLength],
                        MaxRuleScriptLength))
                .WithErrorCode("CoderRuleScriptMaximumLength")
                .When(p => p.RuleScriptTypeId == 2);

            RuleFor(p => p.RuleScriptTypeId)
                .Must(m => allowedRuleScriptTypeIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelGatewayRuleResources.RuleScriptTypeIdInvalid])
                .WithErrorCode("RuleScriptTypeIdInvalid");

            RuleFor(p => p.GatewaySample)
                .InclusiveBetween(0, 1)
                .WithMessage(_ => localiser[EntityAnalysisModelGatewayRuleResources.GatewaySampleRange])
                .WithErrorCode("GatewaySampleRange");

            RuleFor(p => p.MaxResponseElevation)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelGatewayRuleResources.MaxResponseElevationRange])
                .WithErrorCode("MaxResponseElevationRange");

            RuleFor(p => p.Priority)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelGatewayRuleResources.PriorityRange])
                .WithErrorCode("PriorityRange");
        }
    }
}