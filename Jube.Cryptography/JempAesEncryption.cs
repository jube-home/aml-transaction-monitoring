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

namespace Jube.Cryptography
{
    using System.Security.Cryptography;
    using System.Text;
    using Exceptions;

    public class JempAesEncryption
    {
        private readonly byte[] key;
        private readonly bool legacyFallbackEnabled;
        private readonly byte[] passwordDerivedLegacyIv;
        private readonly byte[] salt;

        public JempAesEncryption(string password, string salt, bool legacyFallbackEnabled = false)
        {
            using var keyDerivationFunction =
                new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 100_000, HashAlgorithmName.SHA256);

            key = keyDerivationFunction.GetBytes(32);// 256-bit key
            passwordDerivedLegacyIv = keyDerivationFunction.GetBytes(16);
            this.salt = Encoding.UTF8.GetBytes(salt);
            this.legacyFallbackEnabled = legacyFallbackEnabled;
        }

        public byte[] Encrypt(byte[] data)
        {
            try
            {
                var hmac = ComputeHmac(data);
                var iv = RandomNumberGenerator.GetBytes(16);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var ms = new MemoryStream();
                ms.Write(hmac);
                ms.Write(iv);

                using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();

                return ms.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidEncryptionException(ex.Message);
            }
        }

        public byte[] Decrypt(byte[] encryptedData)
        {
            try
            {
                return DecryptWithRandomIv(encryptedData);
            }
            catch (InvalidHmacException) when (legacyFallbackEnabled)
            {
                return DecryptLegacyWithoutRandomIv(encryptedData);
            }
            catch (Exception) when (legacyFallbackEnabled)
            {
                return DecryptLegacyWithoutRandomIv(encryptedData);
            }
            catch (Exception ex)
            {
                throw new InvalidDecryptionException(ex.Message);
            }
        }

        private byte[] DecryptLegacyWithoutRandomIv(byte[] encryptedData)
        {
            try
            {
                var hmac = encryptedData[..32];
                var clippedEncryptedData = encryptedData[32..];

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = passwordDerivedLegacyIv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var ms = new MemoryStream();
                using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
                cs.Write(clippedEncryptedData, 0, clippedEncryptedData.Length);
                cs.FlushFinalBlock();

                var decryptedData = ms.ToArray();
                return !CryptographicOperations.FixedTimeEquals(ComputeHmacLegacy(decryptedData), hmac) ? throw new InvalidHmacException() : decryptedData;

            }
            catch (Exception ex)
            {
                throw new InvalidDecryptionException(ex.Message);
            }
        }

        private byte[] DecryptWithRandomIv(byte[] encryptedData)
        {
            try
            {
                var hmac = encryptedData[..32];
                var iv = encryptedData[32..48];
                var clippedEncryptedData = encryptedData[48..];

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var ms = new MemoryStream();
                using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
                cs.Write(clippedEncryptedData, 0, clippedEncryptedData.Length);
                cs.FlushFinalBlock();

                var decryptedData = ms.ToArray();
                return !CryptographicOperations.FixedTimeEquals(ComputeHmac(decryptedData), hmac) ? throw new InvalidHmacException() : decryptedData;
            }
            catch (Exception ex)
            {
                throw new InvalidDecryptionException(ex.Message);
            }
        }

        private byte[] ComputeHmac(byte[] data)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(data);
        }

        private byte[] ComputeHmacLegacy(byte[] data)
        {
            using var hmac = new HMACSHA256(salt);
            return hmac.ComputeHash(data);
        }
    }
}
