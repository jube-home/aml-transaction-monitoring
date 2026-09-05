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
using Jube.Dto.EntityAnalysisModelAbstractionCalculation;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelAbstractionCalculation
{
    public sealed class EntityAnalysisModelAbstractionCalculationDtoValidator :
        AbstractValidator<EntityAnalysisModelAbstractionCalculationDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxFunctionScriptLength = 65536;

        private static readonly int[] allowedAbstractionCalculationTypeIds = [1, 2, 3, 4, 5];

        public EntityAnalysisModelAbstractionCalculationDtoValidator(
            EntityAnalysisModelAbstractionCalculationRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelAbstractionCalculationResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionCalculationResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelAbstractionCalculationResources.NameMaxLength],
                        MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionCalculationResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.AbstractionCalculationTypeId)
                .Must(m => allowedAbstractionCalculationTypeIds.Contains(m))
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelAbstractionCalculationResources.AbstractionCalculationTypeIdInvalid])
                .WithErrorCode("AbstractionCalculationTypeIdInvalid");
            
            RuleFor(p => p.EntityAnalysisModelAbstractionNameLeft)
                .NotEmpty()
                .WithMessage(_ => localiser[
                    EntityAnalysisModelAbstractionCalculationResources.EntityAnalysisModelAbstractionNameLeftRequired])
                .WithErrorCode("EntityAnalysisModelAbstractionNameLeftNotEmpty")
                .When(w => w.AbstractionCalculationTypeId != 5);

            RuleFor(p => p.EntityAnalysisModelAbstractionNameRight)
                .NotEmpty()
                .WithMessage(_ => localiser[
                    EntityAnalysisModelAbstractionCalculationResources.EntityAnalysisModelAbstractionNameRightRequired])
                .WithErrorCode("EntityAnalysisModelAbstractionNameRightNotEmpty")
                .When(w => w.AbstractionCalculationTypeId != 5);

            RuleFor(p => p.FunctionScript)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionCalculationResources.FunctionScriptRequired])
                .WithErrorCode("FunctionScriptNotEmpty")
                .MaximumLength(MaxFunctionScriptLength)
                .WithMessage(_ =>
                    string.Format(
                        localiser[EntityAnalysisModelAbstractionCalculationResources.FunctionScriptMaxLength],
                        MaxFunctionScriptLength))
                .WithErrorCode("FunctionScriptMaximumLength")
                .When(w => w.AbstractionCalculationTypeId == 5);
        }
    }
}