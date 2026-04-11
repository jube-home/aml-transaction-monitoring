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

namespace Jube.ApiTokensCache
{
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Text;

    public static class ApiKeyHelper
    {
        private const char Separator = ':';
        private const int Version = 1;
        private const int SecretBytes = 32;
        private const int ChecksumLen = 8;

        public static ApiKeyComponents Generate(
            string userId, string hmacSecret)
        {
            var secret = Base62Encode(RandomNumberGenerator.GetBytes(SecretBytes));

            var bodyForChecksum = String.Join(Separator,
                secret,
                Version,
                EscapeSegment(userId));

            var body = String.Join(Separator,
                secret,
                Version,
                EscapeSegment(userId),
                ComputeHmacChecksum(bodyForChecksum, hmacSecret));

            var issuedKey = Base62Encode(Encoding.UTF8.GetBytes(body));

            return new ApiKeyComponents
            {
                ApiKey = issuedKey,
                ApiKeyDisplay = issuedKey[..8],
                ApiKeyHash = ComputeSha256Hash(issuedKey),
                UserName = userId
            };
        }

        public static bool TryParse(string rawKey, string hmacSecret, out ApiKeyComponents? components)
        {
            components = null;

            if (String.IsNullOrWhiteSpace(rawKey))
            {
                return false;
            }

            string plaintext;
            try
            {
                var bytes = Base62Decode(rawKey);
                plaintext = Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return false;
            }

            var parts = plaintext.Split(Separator);
            if (parts.Length != 4)
            {
                return false;
            }

            var (secret, version, userId, checksum)
                = (parts[0], parts[1], parts[2], parts[3]);

            if (Int32.Parse(version) != Version)
            {
                return false;
            }

            var body = String.Join(Separator, secret, version, userId);
            var expectedChecksum = ComputeHmacChecksum(body, hmacSecret);

            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(checksum),
                    Encoding.UTF8.GetBytes(expectedChecksum)))
            {
                return false;
            }

            components = new ApiKeyComponents
            {
                ApiKey = rawKey,
                ApiKeyDisplay = rawKey[..8] + "…",
                ApiKeyHash = ComputeSha256Hash(rawKey),
                UserName = UnescapeSegment(userId)
            };

            return true;
        }

        private static string ComputeSha256Hash(string key)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string ComputeHmacChecksum(string body, string hmacSecret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(hmacSecret);
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var hash = HMACSHA256.HashData(keyBytes, bodyBytes);
            return Convert.ToHexString(hash)[..ChecksumLen].ToLowerInvariant();
        }

        private static string EscapeSegment(string value)
        {
            return value.Replace("%", "%25")
                .Replace(":", "%3A");
        }

        private static string UnescapeSegment(string value)
        {
            return value.Replace("%3A", ":").Replace("%25", "%");
        }

        private static string Base62Encode(byte[] bytes)
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            var leadingZeros = 0;
            while (leadingZeros < bytes.Length && bytes[leadingZeros] == 0)
            {
                leadingZeros++;
            }

            var result = new StringBuilder();
            var value = new BigInteger(bytes, true, true);
            var base62 = new BigInteger(62);

            while (value > 0)
            {
                value = BigInteger.DivRem(value, base62, out var remainder);
                result.Insert(0, chars[(int)remainder]);
            }

            return new string(chars[0], leadingZeros) + result;
        }

        private static byte[] Base62Decode(string encoded)
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            var leadingZeros = 0;
            while (leadingZeros < encoded.Length && encoded[leadingZeros] == chars[0])
            {
                leadingZeros++;
            }

            var value = BigInteger.Zero;
            foreach (var c in encoded.Substring(leadingZeros))
            {
                var digit = chars.IndexOf(c);
                if (digit < 0)
                {
                    throw new FormatException($"Invalid character: {c}");
                }
                value = value * 62 + digit;
            }

            var data = value.ToByteArray(true, true);
            var result = new byte[leadingZeros + (value.IsZero ? 0 : data.Length)];
            Array.Copy(data, 0, result, leadingZeros, data.Length);
            return result;
        }
    }
}
