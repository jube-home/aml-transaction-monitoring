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
using Jube.Dto.EntityAnalysisModelInlineFunction;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelInlineFunction
{
    public sealed class
        EntityAnalysisModelInlineFunctionDtoValidator : AbstractValidator<EntityAnalysisModelInlineFunctionDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxFunctionScriptLength = 65536;

        private static readonly int[] allowedReturnDataTypeIds = [1, 2, 3, 4, 5];
        private static readonly int[] allowedEncryptionIds = [0, 1, 2];

        public EntityAnalysisModelInlineFunctionDtoValidator(
            EntityAnalysisModelInlineFunctionRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[EntityAnalysisModelInlineFunctionResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelInlineFunctionResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelInlineFunctionResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelInlineFunctionResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.ReturnDataTypeId)
                .Must(m => allowedReturnDataTypeIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelInlineFunctionResources.ReturnDataTypeIdInvalid])
                .WithErrorCode("ReturnDataTypeIdInvalid");

            RuleFor(p => p.FunctionScript)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelInlineFunctionResources.FunctionScriptRequired])
                .WithErrorCode("FunctionScriptNotEmpty")
                .MaximumLength(MaxFunctionScriptLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelInlineFunctionResources.FunctionScriptMaxLength],
                        MaxFunctionScriptLength))
                .WithErrorCode("FunctionScriptMaximumLength");

            RuleFor(p => p.EncryptionId)
                .Must(m => allowedEncryptionIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelInlineFunctionResources.EncryptionIdInvalid])
                .WithErrorCode("EncryptionIdInvalid");
        }
    }
}