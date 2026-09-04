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

namespace Jube.Validations.Authentication
{
    using DynamicEnvironment;
    using FluentValidation;
    using Jube.Dto.Authentication;

    public class AuthenticationRequestDtoValidator : AbstractValidator<AuthenticationRequestDto>
    {
        public AuthenticationRequestDtoValidator(DynamicEnvironment dynamicEnvironment)
        {
            RuleFor(p => p.UserName).NotEmpty();
            RuleFor(p => p.Password).NotEmpty().When(w => !w.PasswordChangeState);
            RuleFor(p => p.Mfa).NotEmpty()
                .When(_ => dynamicEnvironment.AppSettings("EnableMultifactorAuthentication")
                    .Equals("True", StringComparison.OrdinalIgnoreCase));
            RuleFor(p => p.NewPassword)
                .NotEmpty().When(w => w.PasswordChangeState);
        }
    }
}