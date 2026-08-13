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
    using System;
    using System.Collections.Generic;
    using Data.Repository;
    using Dto;
    using FluentValidation;

    public class EntityAnalysisModelRequestXPathDtoValidator : AbstractValidator<EntityAnalysisModelRequestXPathDto>
    {
        public EntityAnalysisModelRequestXPathDtoValidator(EntityAnalysisModelRequestXPathRepository repository)
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
            RuleFor(p => p.EnableSuppression).NotNull();

            var dataTypes = new List<int>
            {
                1,
                2,
                3,
                4,
                5,
                6,
                7
            };

            RuleFor(p => p.DataTypeId).Must(m => dataTypes.Contains(m));
            RuleFor(p => p.XPath).NotEmpty();

            RuleFor(p => p.DefaultValue).NotEmpty();
            RuleFor(p => p.DefaultValue)
                .Must(m => Int32.TryParse(m, out _))
                .When(p => p.DataTypeId == 4)
                .WithMessage("Default value for a Date field must be an integer number of days offset from now.");

            RuleFor(p => p.SearchKey).NotNull();
            RuleFor(p => p.SearchKeyTtlInterval).NotNull();
            RuleFor(p => p.SearchKeyCacheTtlInterval).NotNull();
            RuleFor(p => p.SearchKeyFetchLimit).NotNull();
            RuleFor(p => p.SearchKeyCache).NotNull();

            var intervalTypes = new List<string>
            {
                "s",
                "n",
                "h",
                "d"
            };

            RuleFor(p => p.SearchKeyCacheInterval).Must(m => intervalTypes.Contains(m));
            RuleFor(p => p.SearchKeyCacheTtlInterval).Must(m => intervalTypes.Contains(m));
            RuleFor(p => p.SearchKeyCacheValue).GreaterThanOrEqualTo(0);
            RuleFor(p => p.SearchKeyCacheSample).NotNull();
            RuleFor(p => p.SearchKeyCacheFetchLimit).GreaterThanOrEqualTo(0);
            RuleFor(p => p.SearchKeyCacheTtlValue).GreaterThanOrEqualTo(0);
            RuleFor(p => p.ReportTable).NotNull();
            RuleFor(p => p.ResponsePayload).NotNull();

            var encryptionTypes = new List<int>
            {
                0,
                1,
                2
            };

            RuleFor(p => p.EncryptionId).Must(m => encryptionTypes.Contains(m));
        }
    }
}
