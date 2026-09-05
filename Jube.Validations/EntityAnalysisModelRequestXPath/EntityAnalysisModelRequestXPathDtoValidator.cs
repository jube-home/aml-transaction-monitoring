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
using Jube.Dto.EntityAnalysisModelRequestXPath;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelRequestXPath
{
    public sealed class
        EntityAnalysisModelRequestXPathDtoValidator : AbstractValidator<EntityAnalysisModelRequestXPathDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxXPathLength = 1024;
        private const int MaxDefaultValueLength = 1024;

        private static readonly int[] allowedDataTypeIds = [1, 2, 3, 4, 5, 6, 7];
        private static readonly int[] allowedEncryptionIds = [0, 1, 2];
        private static readonly string[] allowedIntervals = ["s", "n", "h", "d"];

        public EntityAnalysisModelRequestXPathDtoValidator(
            EntityAnalysisModelRequestXPathRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelRequestXPathResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.DataTypeId)
                .Must(m => allowedDataTypeIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.DataTypeIdInvalid])
                .WithErrorCode("DataTypeIdInvalid");

            RuleFor(p => p.XPath)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.XPathRequired])
                .WithErrorCode("XPathNotEmpty")
                .MaximumLength(MaxXPathLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelRequestXPathResources.XPathMaxLength], MaxXPathLength))
                .WithErrorCode("XPathMaximumLength");

            RuleFor(p => p.DefaultValue)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.DefaultValueRequired])
                .WithErrorCode("DefaultValueNotEmpty")
                .MaximumLength(MaxDefaultValueLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelRequestXPathResources.DefaultValueMaxLength],
                        MaxDefaultValueLength))
                .WithErrorCode("DefaultValueMaximumLength");

            RuleFor(p => p.DefaultValue)
                .Must(m => int.TryParse(m, out _))
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.DefaultValueDateOffsetInvalid])
                .WithErrorCode("DefaultValueDateOffsetInvalid")
                .When(p => p.DataTypeId == 4);

            RuleFor(p => p.EncryptionId)
                .Must(m => allowedEncryptionIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.EncryptionIdInvalid])
                .WithErrorCode("EncryptionIdInvalid");

            RuleFor(p => p.SearchKeyTtlInterval)
                .Must(m => allowedIntervals.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.SearchKeyTtlIntervalInvalid])
                .WithErrorCode("SearchKeyTtlIntervalInvalid")
                .When(p => p.SearchKey);

            RuleFor(p => p.SearchKeyTtlIntervalValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.SearchKeyTtlIntervalValueRange])
                .WithErrorCode("SearchKeyTtlIntervalValueRange")
                .When(p => p.SearchKey);

            RuleFor(p => p.SearchKeyFetchLimit)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.SearchKeyFetchLimitRange])
                .WithErrorCode("SearchKeyFetchLimitRange")
                .When(p => p.SearchKey);

            RuleFor(p => p.SearchKeyCacheInterval)
                .Must(m => allowedIntervals.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.SearchKeyCacheIntervalInvalid])
                .WithErrorCode("SearchKeyCacheIntervalInvalid")
                .When(p => p.SearchKey && p.SearchKeyCache);

            RuleFor(p => p.SearchKeyCacheValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.SearchKeyCacheValueRange])
                .WithErrorCode("SearchKeyCacheValueRange")
                .When(p => p.SearchKey && p.SearchKeyCache);

            RuleFor(p => p.SearchKeyCacheFetchLimit)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.SearchKeyCacheFetchLimitRange])
                .WithErrorCode("SearchKeyCacheFetchLimitRange")
                .When(p => p.SearchKey && p.SearchKeyCache);

            RuleFor(p => p.SearchKeyCacheTtlInterval)
                .Must(m => allowedIntervals.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.SearchKeyCacheTtlIntervalInvalid])
                .WithErrorCode("SearchKeyCacheTtlIntervalInvalid")
                .When(p => p.SearchKey && p.SearchKeyCache);

            RuleFor(p => p.SearchKeyCacheTtlValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelRequestXPathResources.SearchKeyCacheTtlValueRange])
                .WithErrorCode("SearchKeyCacheTtlValueRange")
                .When(p => p.SearchKey && p.SearchKeyCache);
        }
    }
}