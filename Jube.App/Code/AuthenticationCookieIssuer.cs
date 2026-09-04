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

namespace Jube.App.Code
{
    using System;
    using DynamicEnvironment;
    using Jube.Dto.Authentication;
    using Microsoft.AspNetCore.Http;

    public static class AuthenticationCookieIssuer
    {
        public static AuthenticationResponseDto IssueAuthenticationCookies(
            HttpResponse response,
            DynamicEnvironment dynamicEnvironment,
            string userName)
        {
            var token = Jwt.CreateToken(userName,
                dynamicEnvironment.AppSettings("JWTKey"),
                dynamicEnvironment.AppSettings("JWTValidIssuer"),
                dynamicEnvironment.AppSettings("JWTValidAudience")
            );

            var expiration = DateTime.UtcNow.AddMinutes(15);

            var authenticationDto = new AuthenticationResponseDto
            {
                Token = token,
                Expiration = expiration
            };

            var cookieExpiration = dynamicEnvironment.AppSettings("SessionCookie")
                .Equals("True", StringComparison.OrdinalIgnoreCase) ? (DateTime?)null : expiration;

            var cookieOptions = new CookieOptions
            {
                Expires = cookieExpiration,
                HttpOnly = false
            };

            if (dynamicEnvironment.AppSettings("SecureHttpCookie").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                cookieOptions.Secure = true;
                cookieOptions.SameSite = SameSiteMode.Strict;
            }

            response.Cookies.Append("authentication-jwt", authenticationDto.Token, cookieOptions);
            response.Cookies.Append("authentication-expiry", expiration.ToString("O"), cookieOptions);

            return authenticationDto;
        }
    }
}
