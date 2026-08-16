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
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using HttpHeaders;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;

    public class ResponseHttpHeadersMiddleware
    {
        private readonly IReadOnlyDictionary<string, string> headersValue;
        private readonly RequestDelegate next;

        public ResponseHttpHeadersMiddleware(RequestDelegate next, HttpHeadersFromDatabase httpHeadersFromDatabase)
        {
            this.next = next;
            headersValue = httpHeadersFromDatabase.HeaderValues;
        }

        public Task InvokeAsync(HttpContext context)
        {
            foreach (var (key, value) in headersValue)
            {
                context.Response.Headers[key] = new StringValues(value);
            }

            return next(context);
        }
    }
}
