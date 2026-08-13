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

namespace Jube.App.Validators
{
    using System.Collections.Generic;
    using Data.Repository;
    using Dto;
    using FluentValidation;

    public class EntityAnalysisModelSanctionDtoValidator : AbstractValidator<EntityAnalysisModelSanctionDto>
    {
        public EntityAnalysisModelSanctionDtoValidator(EntityAnalysisModelSanctionRepository repository)
        {
            RuleFor(p => p.Name)
                .NotEmpty()
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository.GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);

                    if (existing == null)
                    {
                        return true;
                    }

                    return existing.Id == dto.Id;
                })
                .WithMessage("This name already exists.");

            RuleFor(p => p.EntityAnalysisModelId).GreaterThan(0);
            RuleFor(p => p.Active).NotNull();
            RuleFor(p => p.Locked).NotNull();
            RuleFor(p => p.Distance).LessThanOrEqualTo(5);
            RuleFor(p => p.MultipartStringDataName).NotEmpty();
            RuleFor(p => p.CacheValue).GreaterThanOrEqualTo(0);

            var intervals = new List<char>
            {
                's',
                'n',
                'h',
                'd'
            };
            RuleFor(p => p.CacheInterval).Must(x => intervals.Contains(x));

            RuleFor(p => p.ReportTable).NotNull();
            RuleFor(p => p.ResponsePayload).NotNull();

            RuleFor(p => p.MaxDistanceRatio).InclusiveBetween(0, 1)
                .When(p => p.MaxDistanceRatio.HasValue);

            RuleFor(p => p.MaxCoverageRatio).GreaterThanOrEqualTo(0)
                .When(p => p.MaxCoverageRatio.HasValue);
        }
    }
}
