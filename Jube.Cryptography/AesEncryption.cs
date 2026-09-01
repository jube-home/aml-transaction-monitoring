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

namespace Jube.Cryptography;

using System.Security.Cryptography;
using System.Text;

public enum IvMode
{
    Random,
    Deterministic
}

public class AesEncryption(string key, IvMode ivMode = IvMode.Random)
{
    public IvMode IvMode { get; } = ivMode;

    private readonly byte[] key =
        Rfc2898DeriveBytes.Pbkdf2(key, Encoding.UTF8.GetBytes(key), 100_000, HashAlgorithmName.SHA256, 32);

    public string Encrypt(string plainText, IvMode encryptIvMode)
    {
        using var aes = Aes.Create();
        aes.Key = key;

        if (encryptIvMode == IvMode.Deterministic)
        {
            aes.IV = SHA256.HashData(Encoding.UTF8.GetBytes(plainText))[..16];
        }

        using var ms = new MemoryStream();
        ms.Write(aes.IV);

        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }
}