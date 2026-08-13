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
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    [Authorize]
    public class ChangePassword : PageModel
    {
        private readonly DynamicEnvironment dynamicEnvironment;

        public ChangePassword(DynamicEnvironment dynamicEnvironment)
        {
            this.dynamicEnvironment = dynamicEnvironment;
        }

        public ActionResult OnGet()
        {
            if (dynamicEnvironment.AppSettings("OAuthAuthentication").Equals("True", StringComparison.CurrentCultureIgnoreCase)
                || dynamicEnvironment.AppSettings("NegotiateAuthentication").Equals("True", StringComparison.CurrentCultureIgnoreCase))
            {
                return Forbid();
            }

            return new PageResult();
        }
    }
}
