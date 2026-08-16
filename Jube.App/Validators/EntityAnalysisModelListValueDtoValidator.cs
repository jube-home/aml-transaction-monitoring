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

using System;
using FluentValidation;
using Jube.App.Dto;

namespace Jube.App.Validators
{
    public class EntityAnalysisModelsListValueDtoValidator : AbstractValidator<EntityAnalysisModelListValueDto>
    {
        public EntityAnalysisModelsListValueDtoValidator()
        {
            RuleFor(p => p.ListValue).NotEmpty().WithMessage("Please enter a value for this list.");
            RuleFor(p => p.EntityAnalysisModelListId).GreaterThan(0);
            RuleFor(p => p.DeleteExpiryDate)
                .GreaterThan(_ => DateTimeOffset.UtcNow)
                .When(p => p.DeleteExpiryDate.HasValue)
                .WithMessage("Delete Expiry Date must be greater than the current date and time.");
        }
    }
}