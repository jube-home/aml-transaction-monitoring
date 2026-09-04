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

namespace Jube.Validations.EntityAnalysisModel
{
    using System;
    using FluentValidation;
    using Jube.Data.Repository;
    using Jube.Dto.EntityAnalysisModel;
    using Jube.Resources;
    using Microsoft.Extensions.Localization;
    
    public sealed class EntityAnalysisModelDtoValidator : AbstractValidator<EntityAnalysisModelDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxXPathLength = 1024;

        private static readonly byte[] allowedPayloadLocationTypeIds = [1, 3];
        private static readonly char[] allowedIntervals = ['s', 'n', 'h', 'd'];

        public EntityAnalysisModelDtoValidator(EntityAnalysisModelRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => String.Format(localiser[EntityAnalysisModelResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository.GetByNameAsync(name, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.EntryName)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelResources.EntryNameRequired])
                .WithErrorCode("EntryNameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    String.Format(localiser[EntityAnalysisModelResources.EntryNameMaxLength], MaxNameLength))
                .WithErrorCode("EntryNameMaximumLength");

            RuleFor(p => p.EntryXPath)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelResources.EntryXPathRequired])
                .WithErrorCode("EntryXPathNotEmpty")
                .MaximumLength(MaxXPathLength)
                .WithMessage(_ =>
                    String.Format(localiser[EntityAnalysisModelResources.EntryXPathMaxLength], MaxXPathLength))
                .WithErrorCode("EntryXPathMaximumLength");

            RuleFor(p => p.ReferenceDateName)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelResources.ReferenceDateNameRequired])
                .WithErrorCode("ReferenceDateNameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    String.Format(localiser[EntityAnalysisModelResources.ReferenceDateNameMaxLength], MaxNameLength))
                .WithErrorCode("ReferenceDateNameMaximumLength");

            RuleFor(p => p.ReferenceDatePayloadLocationTypeId)
                .Must(m => allowedPayloadLocationTypeIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelResources.ReferenceDatePayloadLocationTypeIdInvalid])
                .WithErrorCode("ReferenceDatePayloadLocationTypeIdInvalid");
            
            RuleFor(p => p.ReferenceDateXPath)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelResources.ReferenceDateXPathRequired])
                .WithErrorCode("ReferenceDateXPathNotEmpty")
                .MaximumLength(MaxXPathLength)
                .WithMessage(_ => String.Format(localiser[EntityAnalysisModelResources.ReferenceDateXPathMaxLength],
                    MaxXPathLength))
                .WithErrorCode("ReferenceDateXPathMaximumLength")
                .When(p => p.ReferenceDatePayloadLocationTypeId != 3);

            RuleFor(p => p.CacheFetchLimit)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelResources.CacheFetchLimitRange])
                .WithErrorCode("CacheFetchLimitRange");

            RuleFor(p => p.CacheTtlInterval)
                .Must(m => allowedIntervals.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelResources.CacheTtlIntervalInvalid])
                .WithErrorCode("CacheTtlIntervalInvalid");

            RuleFor(p => p.CacheTtlIntervalValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelResources.CacheTtlIntervalValueRange])
                .WithErrorCode("CacheTtlIntervalValueRange");

            RuleFor(p => p.MaxResponseElevation)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelResources.MaxResponseElevationRange])
                .WithErrorCode("MaxResponseElevationRange");
            
            RuleFor(p => p.MaxResponseElevationInterval)
                .Must(m => allowedIntervals.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelResources.MaxResponseElevationIntervalInvalid])
                .WithErrorCode("MaxResponseElevationIntervalInvalid")
                .When(p => p.EnableResponseElevationLimit);

            RuleFor(p => p.MaxResponseElevationValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelResources.MaxResponseElevationValueRange])
                .WithErrorCode("MaxResponseElevationValueRange")
                .When(p => p.EnableResponseElevationLimit);

            RuleFor(p => p.MaxResponseElevationThreshold)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelResources.MaxResponseElevationThresholdRange])
                .WithErrorCode("MaxResponseElevationThresholdRange")
                .When(p => p.EnableResponseElevationLimit);
            
            RuleFor(p => p.MaxActivationWatcherInterval)
                .Must(m => allowedIntervals.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelResources.MaxActivationWatcherIntervalInvalid])
                .WithErrorCode("MaxActivationWatcherIntervalInvalid")
                .When(p => p.EnableActivationWatcher);

            RuleFor(p => p.MaxActivationWatcherValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelResources.MaxActivationWatcherValueRange])
                .WithErrorCode("MaxActivationWatcherValueRange")
                .When(p => p.EnableActivationWatcher);

            RuleFor(p => p.MaxActivationWatcherThreshold)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelResources.MaxActivationWatcherThresholdRange])
                .WithErrorCode("MaxActivationWatcherThresholdRange")
                .When(p => p.EnableActivationWatcher);

            RuleFor(p => p.ActivationWatcherSample)
                .InclusiveBetween(0, 1)
                .WithMessage(_ => localiser[EntityAnalysisModelResources.ActivationWatcherSampleRange])
                .WithErrorCode("ActivationWatcherSampleRange")
                .When(p => p.EnableActivationWatcher);
        }
    }
}