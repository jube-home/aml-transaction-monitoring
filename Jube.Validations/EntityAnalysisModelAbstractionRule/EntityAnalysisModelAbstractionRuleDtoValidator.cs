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
using Jube.Dto.EntityAnalysisModelAbstractionRule;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelAbstractionRule
{
    public sealed class
        EntityAnalysisModelAbstractionRuleDtoValidator : AbstractValidator<EntityAnalysisModelAbstractionRuleDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxRuleScriptLength = 65536;

        private static readonly int[] allowedRuleScriptTypeIds = [1, 2];
        private static readonly string[] allowedSearchIntervals = ["s", "n", "h", "d"];
        private static readonly int[] allowedOffsetTypeIds = [1, 2, 3, 4];

        private static readonly int[] allowedSearchFunctionTypeIds =
            [1, 2, 3, 4, 5, 6, 7, 8, 11, 12, 13, 14, 15, 16];

        public EntityAnalysisModelAbstractionRuleDtoValidator(EntityAnalysisModelAbstractionRuleRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionRuleResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionRuleResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelAbstractionRuleResources.NameMaxLength],
                        MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionRuleResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            // RuleScriptTypeId selects the rule's authoring surface (1 = Builder, 2 = Coder) and the two are
            // mutually exclusive: Builder authors BuilderRuleScript/Json, Coder authors CoderRuleScript. Only the
            // selected surface's script is required.
            RuleFor(p => p.RuleScriptTypeId)
                .Must(m => allowedRuleScriptTypeIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionRuleResources.RuleScriptTypeIdInvalid])
                .WithErrorCode("RuleScriptTypeIdInvalid");

            RuleFor(p => p.BuilderRuleScript)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionRuleResources.BuilderRuleScriptRequired])
                .WithErrorCode("BuilderRuleScriptNotEmpty")
                .MaximumLength(MaxRuleScriptLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelAbstractionRuleResources.BuilderRuleScriptMaxLength],
                        MaxRuleScriptLength))
                .WithErrorCode("BuilderRuleScriptMaximumLength")
                .When(p => p.RuleScriptTypeId == 1);

            RuleFor(p => p.Json)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionRuleResources.JsonRequired])
                .WithErrorCode("JsonNotEmpty")
                .When(p => p.RuleScriptTypeId == 1);

            RuleFor(p => p.CoderRuleScript)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionRuleResources.CoderRuleScriptRequired])
                .WithErrorCode("CoderRuleScriptNotEmpty")
                .MaximumLength(MaxRuleScriptLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelAbstractionRuleResources.CoderRuleScriptMaxLength],
                        MaxRuleScriptLength))
                .WithErrorCode("CoderRuleScriptMaximumLength")
                .When(p => p.RuleScriptTypeId == 2);

            RuleFor(p => p.SearchKey)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionRuleResources.SearchKeyRequired])
                .WithErrorCode("SearchKeyNotEmpty")
                .When(w => w.Search);

            RuleFor(p => p.SearchValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionRuleResources.SearchValueRange])
                .WithErrorCode("SearchValueRange")
                .When(w => w.Search);

            RuleFor(p => p.SearchInterval)
                .Must(m => allowedSearchIntervals.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionRuleResources.SearchIntervalInvalid])
                .WithErrorCode("SearchIntervalInvalid")
                .When(w => w.Search);

            RuleFor(p => p.SearchFunctionTypeId)
                .Must(m => allowedSearchFunctionTypeIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionRuleResources.SearchFunctionTypeIdInvalid])
                .WithErrorCode("SearchFunctionTypeIdInvalid")
                .When(w => w.Search);

            RuleFor(p => p.SearchFunctionKey)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionRuleResources.SearchFunctionKeyRequired])
                .WithErrorCode("SearchFunctionKeyNotEmpty")
                .When(w => w.Search && w.SearchFunctionTypeId != 1);

            RuleFor(p => p.OffsetTypeId)
                .Must(m => allowedOffsetTypeIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionRuleResources.OffsetTypeIdInvalid])
                .WithErrorCode("OffsetTypeIdInvalid")
                .When(w => w.Search && w.Offset);

            RuleFor(p => p.OffsetValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelAbstractionRuleResources.OffsetValueRange])
                .WithErrorCode("OffsetValueRange")
                .When(w => w.Search && w.Offset);
        }
    }
}