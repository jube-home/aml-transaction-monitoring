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

namespace Jube.Data.Security
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using Dictionary.Extensions;

    public static class HashPassword
    {
        public static string Sha256(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static string Argon2(string password, string key = null)
        {
            return key != null && key.IsNullOrEmpty() ? Isopoh.Cryptography.Argon2.Argon2.Hash(password) : Isopoh.Cryptography.Argon2.Argon2.Hash(password, key);
        }

        public static bool Verify(string passwordHash, string password, string key = null)
        {
            return key != null && (key.IsNullOrEmpty() ? Isopoh.Cryptography.Argon2.Argon2.Verify(passwordHash, password) : Isopoh.Cryptography.Argon2.Argon2.Verify(passwordHash, password, key));
        }

        public static string CreateSecurePassword(int length)
        {
            const string valid = "!@#$%^&*()abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            var res = new StringBuilder();

            using (var rng = RandomNumberGenerator.Create())
            {
                var uintBuffer = new byte[4];
                while (length-- > 0)
                {
                    rng.GetBytes(uintBuffer);
                    var num = BitConverter.ToUInt32(uintBuffer, 0);

                    res.Append(valid[(int)(num % (uint)valid.Length)]);
                }
            }

            return res.ToString();
        }
    }
}
