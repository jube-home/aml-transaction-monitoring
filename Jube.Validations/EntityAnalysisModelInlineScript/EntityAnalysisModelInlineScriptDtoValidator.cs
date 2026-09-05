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
using Jube.Dto.EntityAnalysisModelInlineScript;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelInlineScript
{
    public sealed class
        EntityAnalysisModelInlineScriptDtoValidator : AbstractValidator<EntityAnalysisModelInlineScriptDto>
    {
        private const int MaxNameLength = 256;

        public EntityAnalysisModelInlineScriptDtoValidator(
            EntityAnalysisModelInlineScriptRepository repository,
            EntityAnalysisInlineScriptRepository entityAnalysisInlineScriptRepository, IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[EntityAnalysisModelInlineScriptResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelInlineScriptResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelInlineScriptResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelInlineScriptResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.EntityAnalysisInlineScriptId)
                .GreaterThan(0)
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelInlineScriptResources.EntityAnalysisInlineScriptIdRequired])
                .WithErrorCode("EntityAnalysisInlineScriptIdNotEmpty")
                .MustAsync(async (id, cancellation) =>
                {
                    var existing = await entityAnalysisInlineScriptRepository.GetByIdAsync(id, cancellation);
                    return existing != null;
                })
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelInlineScriptResources.EntityAnalysisInlineScriptIdInvalid])
                .WithErrorCode("EntityAnalysisInlineScriptIdInvalid");
        }
    }
}