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
using Jube.Service.Dto.Authentication;

namespace Jube.Validations.Authentication
{
    public class AuthenticationRequestDtoValidator : AbstractValidator<AuthenticationRequestDto>
    {
        public AuthenticationRequestDtoValidator()
        {
            RuleFor(p => p.UserName).NotEmpty();
            RuleFor(p => p.Password).NotEmpty();
            RuleFor(p => p.NewPassword)
                .MinimumLength(8).WithMessage("Your password length must be at least 8 characters.")
                .MaximumLength(16).WithMessage("Your password length must not exceed 16 characters.")
                .Matches(@"[A-Z]+").WithMessage("Your password must contain at least one uppercase letter.")
                .Matches(@"[a-z]+").WithMessage("Your password must contain at least one lowercase letter.")
                .Matches(@"[0-9]+").WithMessage("Your password must contain at least one number.")
                .Matches(@"[\!\?\*\.]+").WithMessage("Your password must contain at least one (!? *.).")
                .Equal(e => e.RepeatNewPassword).WithMessage("Repeat New Password.")
                .When(w => !String.IsNullOrEmpty(w.NewPassword));
            RuleFor(p => p.NewPassword)
                .NotEmpty().When(w => w.PasswordChangeState);
        }
    }
}