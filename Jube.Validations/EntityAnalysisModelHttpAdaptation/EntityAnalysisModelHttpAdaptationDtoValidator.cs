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
using Jube.Dto.EntityAnalysisModelHttpAdaptation;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelHttpAdaptation
{
    public sealed class
        EntityAnalysisModelHttpAdaptationDtoValidator : AbstractValidator<EntityAnalysisModelHttpAdaptationDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxHttpEndpointLength = 2048;

        public EntityAnalysisModelHttpAdaptationDtoValidator(EntityAnalysisModelHttpAdaptationRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[EntityAnalysisModelHttpAdaptationResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelHttpAdaptationResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelHttpAdaptationResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelHttpAdaptationResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.HttpEndpoint)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelHttpAdaptationResources.HttpEndpointRequired])
                .WithErrorCode("HttpEndpointNotEmpty")
                .MaximumLength(MaxHttpEndpointLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelHttpAdaptationResources.HttpEndpointMaxLength],
                        MaxHttpEndpointLength))
                .WithErrorCode("HttpEndpointMaximumLength");
            
            RuleFor(p => p.HttpEndpoint)
                .Must(m => m!.StartsWith('/'))
                .WithMessage(_ => localiser[EntityAnalysisModelHttpAdaptationResources.HttpEndpointMustBeAbsolutePath])
                .WithErrorCode("HttpEndpointMustBeAbsolutePath")
                .When(w => !string.IsNullOrEmpty(w.HttpEndpoint));

            RuleFor(p => p.Priority)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelHttpAdaptationResources.PriorityRange])
                .WithErrorCode("PriorityRange");
        }
    }
}