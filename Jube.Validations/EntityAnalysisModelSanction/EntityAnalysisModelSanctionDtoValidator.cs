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
using Jube.Dto.EntityAnalysisModelSanction;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelSanction
{
    public sealed class EntityAnalysisModelSanctionDtoValidator : AbstractValidator<EntityAnalysisModelSanctionDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxMultipartStringDataNameLength = 256;

        private static readonly char[] allowedCacheIntervals = ['s', 'n', 'h', 'd'];
        private static readonly byte[] allowedAggregationTypeIds = [1, 2, 3, 4, 5, 6, 7, 8];

        public EntityAnalysisModelSanctionDtoValidator(EntityAnalysisModelSanctionRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[EntityAnalysisModelSanctionResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelSanctionResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelSanctionResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelSanctionResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.MultipartStringDataName)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelSanctionResources.MultipartStringDataNameRequired])
                .WithErrorCode("MultipartStringDataNameNotEmpty")
                .MaximumLength(MaxMultipartStringDataNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelSanctionResources.MultipartStringDataNameMaxLength],
                        MaxMultipartStringDataNameLength))
                .WithErrorCode("MultipartStringDataNameMaximumLength");
            
            RuleFor(p => p.Distance)
                .InclusiveBetween(0, 5)
                .WithMessage(_ => localiser[EntityAnalysisModelSanctionResources.DistanceRange])
                .WithErrorCode("DistanceRange");

            RuleFor(p => p.CacheValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelSanctionResources.CacheValueRange])
                .WithErrorCode("CacheValueRange");

            RuleFor(p => p.CacheInterval)
                .Must(m => allowedCacheIntervals.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelSanctionResources.CacheIntervalInvalid])
                .WithErrorCode("CacheIntervalInvalid");
            
            RuleFor(p => p.AggregationTypeId)
                .Must(m => !m.HasValue || allowedAggregationTypeIds.Contains(m.Value))
                .WithMessage(_ => localiser[EntityAnalysisModelSanctionResources.AggregationTypeIdInvalid])
                .WithErrorCode("AggregationTypeIdInvalid");

            RuleFor(p => p.MaxDistanceRatio)
                .InclusiveBetween(0, 1)
                .WithMessage(_ => localiser[EntityAnalysisModelSanctionResources.MaxDistanceRatioRange])
                .WithErrorCode("MaxDistanceRatioRange")
                .When(p => p.MaxDistanceRatio.HasValue);

            RuleFor(p => p.MaxCoverageRatio)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelSanctionResources.MaxCoverageRatioRange])
                .WithErrorCode("MaxCoverageRatioRange")
                .When(p => p.MaxCoverageRatio.HasValue);
        }
    }
}