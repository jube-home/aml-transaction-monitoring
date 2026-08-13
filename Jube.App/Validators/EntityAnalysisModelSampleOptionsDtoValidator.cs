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
    using Dto;
    using FluentValidation;

    public class EntityAnalysisModelSampleOptionsDtoValidator : AbstractValidator<EntityAnalysisModelSampleOptionsDto>
    {
        public EntityAnalysisModelSampleOptionsDtoValidator()
        {
            RuleFor(p => p.EntityAnalysisModelGuid)
                .NotEmpty()
                .WithMessage("An Entity Analysis Model must be selected.");

            RuleFor(p => p.DateFrom)
                .NotEmpty()
                .WithMessage("A start date is required.");

            RuleFor(p => p.DateTo)
                .NotEmpty()
                .WithMessage("An end date is required.")
                .GreaterThan(p => p.DateFrom)
                .WithMessage("End date must be after the start date.");

            RuleFor(p => p.Sample)
                .InclusiveBetween(0.0, 1.0)
                .WithMessage("Sample rate must be between 0 and 1.");
        }
    }
}
