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
using Jube.Dto.EntityAnalysisModelTtlCounter;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelTtlCounter
{
    public sealed class EntityAnalysisModelTtlCounterDtoValidator : AbstractValidator<EntityAnalysisModelTtlCounterDto>
    {
        private const int MaxNameLength = 256;
        private static readonly string[] intervals = ["s", "n", "h", "d", "m", "y"];
        private static readonly string[] resolutionIntervals = ["n", "h", "d"];

        public EntityAnalysisModelTtlCounterDtoValidator(EntityAnalysisModelTtlCounterRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[EntityAnalysisModelTtlCounterResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelTtlCounterResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelTtlCounterResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelTtlCounterResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.TtlCounterValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelTtlCounterResources.TtlCounterValueInvalid])
                .WithErrorCode("TtlCounterValueInvalid");

            RuleFor(p => p.TtlCounterDataName)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelTtlCounterResources.TtlCounterDataNameRequired])
                .WithErrorCode("TtlCounterDataNameRequired");

            RuleFor(p => p.TtlCounterDataValue)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelTtlCounterResources.TtlCounterDataValueRequired])
                .WithErrorCode("TtlCounterDataValueRequired")
                .When(p => p.EnableSum);

            RuleFor(p => p.TtlCounterInterval)
                .Must(x => intervals.Contains(x))
                .WithMessage(_ => localiser[EntityAnalysisModelTtlCounterResources.TtlCounterIntervalInvalid])
                .WithErrorCode("TtlCounterIntervalInvalid");

            RuleFor(p => p.ResolutionInterval)
                .Must(x => resolutionIntervals.Contains(x))
                .WithMessage(_ => localiser[EntityAnalysisModelTtlCounterResources.ResolutionIntervalInvalid])
                .WithErrorCode("ResolutionIntervalInvalid");
        }
    }
}