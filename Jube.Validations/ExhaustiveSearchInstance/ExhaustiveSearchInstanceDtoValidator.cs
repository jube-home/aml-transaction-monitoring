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
using Jube.Dto.ExhaustiveSearchInstance;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.ExhaustiveSearchInstance
{
    public sealed class ExhaustiveSearchInstanceDtoValidator : AbstractValidator<ExhaustiveSearchInstanceDto>
    {
        private const int MaxNameLength = 256;

        public ExhaustiveSearchInstanceDtoValidator(ExhaustiveSearchInstanceRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[ExhaustiveSearchInstanceResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.AnomalyProbability)
                .InclusiveBetween(0, 1)
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.AnomalyProbabilityRange])
                .WithErrorCode("AnomalyProbabilityRange")
                .When(w => w.Anomaly);

            RuleFor(p => p.FilterJson)
                .NotEmpty()
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.FilterJsonRequired])
                .WithErrorCode("FilterJsonNotEmpty")
                .When(w => w.Filter);

            RuleFor(p => p.FilterSql)
                .NotEmpty()
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.FilterSqlRequired])
                .WithErrorCode("FilterSqlNotEmpty")
                .When(w => w.Filter);

            RuleFor(p => p.FilterTokens)
                .NotEmpty()
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.FilterTokensRequired])
                .WithErrorCode("FilterTokensNotEmpty")
                .When(w => w.Filter);
        }
    }
}
