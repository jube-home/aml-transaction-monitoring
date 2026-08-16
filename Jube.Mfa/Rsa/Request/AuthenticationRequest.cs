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

namespace Jube.Mfa.Rsa.Request
{
    /// <summary>
    /// The wire shape posted to the RSA Authentication Manager REST API.
    /// </summary>
    public class AuthenticationRequest
    {
        public string ClientId { get; set; } = String.Empty;
        public string SubjectName { get; set; } = String.Empty;
        public List<SubjectCredentials> SubjectCredentials { get; set; } = [];
        public Context Context { get; set; } = new();
    }
}
