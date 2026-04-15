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

namespace Jube.App.Middlewares
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Code;
    using DynamicEnvironment;
    using Microsoft.AspNetCore.Http;

    public class TokenRefreshMiddleware
    {
        private const string CookieName = "authentication";
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly RequestDelegate next;

        public TokenRefreshMiddleware(RequestDelegate next, DynamicEnvironment dynamicEnvironment)
        {
            this.next = next;
            this.dynamicEnvironment = dynamicEnvironment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await TryRefreshAccessTokenAsync(context);
            await next(context);
        }

        private Task TryRefreshAccessTokenAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated != true
                || !context.Request.Cookies.TryGetValue(CookieName, out var accessToken)
                || String.IsNullOrEmpty(accessToken))
            {
                return Task.CompletedTask;
            }

            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken))
            {
                return Task.CompletedTask;
            }

            var jwt = handler.ReadJwtToken(accessToken);
            var expiry = jwt.ValidTo;
            var now = DateTime.UtcNow;
            var totalLifetime = expiry - jwt.ValidFrom;
            var remaining = expiry - now;

            if (remaining > totalLifetime / 2)
            {
                return Task.CompletedTask;
            }

            var username = context.User.FindFirstValue(ClaimTypes.Name);
            if (String.IsNullOrEmpty(username))
            {
                return Task.CompletedTask;
            }

            var token = Jwt.CreateToken(username,
                dynamicEnvironment.AppSettings("JWTKey"),
                dynamicEnvironment.AppSettings("JWTValidIssuer"),
                dynamicEnvironment.AppSettings("JWTValidAudience")
            );

            context.Response.Cookies.Append(CookieName, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });
            return Task.CompletedTask;
        }
    }
}
