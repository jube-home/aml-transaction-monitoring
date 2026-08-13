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

namespace Jube.App.Pages.Account
{
    using System;
    using DynamicEnvironment;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly DynamicEnvironment dynamicEnvironment;

        public LoginModel(DynamicEnvironment dynamicEnvironment)
        {
            this.dynamicEnvironment = dynamicEnvironment;
        }

        public IActionResult OnGet(string redirectUrl = "/")
        {
            if (String.IsNullOrWhiteSpace(redirectUrl))
            {
                redirectUrl = "/";
            }

            if (!Url.IsLocalUrl(redirectUrl) || redirectUrl.StartsWith("//") || redirectUrl.StartsWith("\\\\"))
            {
                redirectUrl = "/";
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                if (redirectUrl.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase))
                {
                    redirectUrl = "/";
                }

                return LocalRedirect(redirectUrl);
            }

            var oAuthEnabled = dynamicEnvironment.AppSettings("OAuthAuthentication")
                ?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false;

            var negotiateEnabled = dynamicEnvironment.AppSettings("NegotiateAuthentication")
                ?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false;

            switch (oAuthEnabled)
            {
                case true when negotiateEnabled:
                    throw new InvalidOperationException(
                        "OAuthAuthentication and NegotiateAuthentication are mutually exclusive and cannot both be enabled.");
                case true:
                    return Challenge(new AuthenticationProperties
                    {
                        RedirectUri = redirectUrl
                    }, "OAuth");
            }

            return Page();
        }
    }
}
