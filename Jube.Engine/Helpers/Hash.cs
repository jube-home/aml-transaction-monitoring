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

namespace Jube.Engine.Helpers
{
    using System.Security.Cryptography;
    using System.Text;

    public static class Hash
    {
        public static string GetHash(string theInput)
        {
            using var hasher = MD5.Create();
            var bytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(theInput));

            var sBuilder = new StringBuilder();

            foreach (var t in bytes)
            {
                sBuilder.Append(t.ToString("X2"));
            }

            return sBuilder.ToString();
        }
    }
}
