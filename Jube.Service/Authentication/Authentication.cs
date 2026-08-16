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

namespace Jube.Service.Authentication
{
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Data.Security;
    using Dto.Authentication;
    using Exceptions.Authentication;

    public class Authentication(DbContext dbContext, bool passwordAsymmetricEncryption = false, string? passwordAsymmetricEncryptionPrivateKey = null)
    {

        private const int MinLength = 12;
        private const int MaxLength = 128;

        private static readonly (Func<string, bool> Test, string Message)[] Rules =
        [
            (p => p.Length >= MinLength, $"At least {MinLength} characters"),
            (p => p.Length <= MaxLength, $"No more than {MaxLength} characters"),
            (p => p.Any(Char.IsUpper), "At least one uppercase letter"),
            (p => p.Any(Char.IsLower), "At least one lowercase letter"),
            (p => p.Any(Char.IsDigit), "At least one number"),
            (p => p.Any(c => !Char.IsLetterOrDigit(c)), "At least one special character"),
            (p => !Regex.IsMatch(p, @"(.)\1{2,}"), "No character repeated more than twice consecutively"),
            (p => !Regex.IsMatch(p, @"^(password|123456|qwerty)", RegexOptions.IgnoreCase), "Cannot start with common patterns")
        ];
        public async Task<bool> IsWirePasswordHashAsync(string userName, CancellationToken token = default)
        {
            var userRegistryRepository = new UserRegistryRepository(dbContext);
            var userRegistry = await userRegistryRepository.GetByUserNameAsync(userName, token);

            if (userRegistry == null)
            {
                return true;
            }

            return userRegistry.WirePasswordHash == 1;
        }

        public async Task AuthenticateByNegotiateAsync(string userName, string? localIp,
            string? userAgent, bool mfa, CancellationToken token = default)
        {
            const int authenticationMethod = 2;

            var userLogin = new UserLogin
            {
                LocalIp = localIp,
                UserAgent = userAgent,
                AuthenticationTypeId = authenticationMethod
            };

            var userRegistryRepository = new UserRegistryRepository(dbContext);
            var userRegistry = await userRegistryRepository.GetByUserNameAsync(userName, token);

            if (userRegistry == null)
            {
                await LogLoginFailedAsync(userLogin, userName, 1, authenticationMethod, token);
                throw new NoUserException();
            }

            if (userRegistry.Active != 1)
            {
                await LogLoginFailedAsync(userLogin, userRegistry.Name, 2, authenticationMethod, token);
                throw new NotActiveException();
            }

            if (userRegistry.PasswordLocked == 1)
            {
                await LogLoginFailedAsync(userLogin, userRegistry.Name, 3, authenticationMethod, token);
                throw new PasswordLockedException();
            }

            await LogLoginSuccessAsync(userLogin, userRegistry.Name, authenticationMethod, token);
        }

        public async Task AuthenticateByUserNamePasswordAsync(AuthenticationRequestDto authenticationRequestDto,
            string? passwordHashingKey, int lockPasswordAfter = 3, CancellationToken token = default)
        {
            const int authenticationMethod = 1;
            var userRegistryRepository = new UserRegistryRepository(dbContext);
            var userRegistry = await userRegistryRepository.GetByUserNameAsync(authenticationRequestDto.UserName, token);

            var userLogin = new UserLogin
            {
                RemoteIp = authenticationRequestDto.RemoteIp,
                LocalIp = authenticationRequestDto.LocalIp,
                UserAgent = authenticationRequestDto.UserAgent
            };

            if (userRegistry == null)
            {
                await LogLoginFailedAsync(userLogin, authenticationRequestDto.UserName ?? "", 1, 1, token);
                throw new NoUserException();
            }

            if (userRegistry.Active != 1)
            {
                await LogLoginFailedAsync(userLogin, userRegistry.Name, 2, authenticationMethod, token);
                throw new NotActiveException();
            }

            if (userRegistry.PasswordLocked == 1)
            {
                await LogLoginFailedAsync(userLogin, userRegistry.Name, 3, authenticationMethod, token);
                throw new PasswordLockedException();
            }

            var password = authenticationRequestDto.Password;
            if (String.IsNullOrEmpty(password))
            {
                await LogLoginFailedAsync(userLogin, userRegistry.Name, 6, authenticationMethod, token);
                throw new PasswordEmptyException();
            }

            if (passwordAsymmetricEncryption && passwordAsymmetricEncryptionPrivateKey != null)
            {
                password = DecryptPassword(password, passwordAsymmetricEncryptionPrivateKey);
            }

            if (!HashPassword.Verify(userRegistry.Password, password, passwordHashingKey))
            {
                await userRegistryRepository.IncrementFailedPasswordAsync(userRegistry.Id, token);

                if (String.IsNullOrEmpty(authenticationRequestDto.NewPassword))
                {
                    if (!userRegistry.PasswordExpiryDate.HasValue || !userRegistry.PasswordCreatedDate.HasValue)
                    {
                        await LogLoginFailedAsync(userLogin, userRegistry.Name, 4, authenticationMethod, token);
                        throw new PasswordNewMustChangeException();
                    }
                }

                if (userRegistry.FailedPasswordCount > lockPasswordAfter)
                {
                    await userRegistryRepository.SetLockedAsync(userRegistry.Id, token);
                }

                await LogLoginFailedAsync(userLogin, userRegistry.Name, 5, authenticationMethod, token);

                throw new BadCredentialsException();
            }

            if (!String.IsNullOrEmpty(authenticationRequestDto.NewPassword))
            {
                var newPassword = authenticationRequestDto.NewPassword;
                if (passwordAsymmetricEncryption && passwordAsymmetricEncryptionPrivateKey != null)
                {
                    newPassword = DecryptPassword(newPassword, passwordAsymmetricEncryptionPrivateKey);
                }

                if (userRegistry.WirePasswordHash != 1)
                {
                    ValidatePasswordStrength(newPassword);
                }

                var hashedPassword = HashPassword.Argon2(newPassword, passwordHashingKey);

                await userRegistryRepository.SetPasswordAsync(userRegistry.Id, hashedPassword, DateTime.UtcNow.AddDays(90),
                    userRegistry.WirePasswordHash == 1, token);
            }
            else
            {
                if (!userRegistry.PasswordExpiryDate.HasValue || !(DateTime.UtcNow <= userRegistry.PasswordExpiryDate.Value))
                {
                    throw new PasswordExpiredException();
                }
            }

            await LogLoginSuccessAsync(userLogin, userRegistry.Name, authenticationMethod, token);

            if (userRegistry.FailedPasswordCount > 0)
            {
                await userRegistryRepository.ResetFailedPasswordCountAsync(userRegistry.Id, token);
            }
        }

        public async Task ChangePasswordAsync(string? userName, ChangePasswordRequestDto changePasswordRequestDto,
            string? passwordHashingKey, CancellationToken token = default)
        {
            var userRegistryRepository = new UserRegistryRepository(dbContext);
            var userRegistry = await userRegistryRepository.GetByUserNameAsync(userName, token);

            var password = changePasswordRequestDto.Password;
            if (String.IsNullOrEmpty(password))
            {
                throw new PasswordEmptyException();
            }

            if (passwordAsymmetricEncryption && passwordAsymmetricEncryptionPrivateKey != null)
            {
                password = DecryptPassword(password, passwordAsymmetricEncryptionPrivateKey);
            }

            if (!HashPassword.Verify(userRegistry.Password, password, passwordHashingKey))
            {
                throw new BadCredentialsException();
            }

            var newPassword = changePasswordRequestDto.NewPassword;
            if (String.IsNullOrEmpty(newPassword))
            {
                var errors = new List<string>
                {
                    "Missing New Password."
                };

                throw new PasswordStrengthException(errors);
            }

            if (passwordAsymmetricEncryption && passwordAsymmetricEncryptionPrivateKey != null)
            {
                newPassword = DecryptPassword(newPassword, passwordAsymmetricEncryptionPrivateKey);
            }

            if (userRegistry.WirePasswordHash != 1)
            {
                ValidatePasswordStrength(newPassword);
            }

            var hashedPassword = HashPassword.Argon2(newPassword, passwordHashingKey);

            await userRegistryRepository.SetPasswordAsync(userRegistry.Id, hashedPassword, DateTime.UtcNow.AddDays(90),
                userRegistry.WirePasswordHash == 1, token);
        }

        private Task LogLoginFailedAsync(UserLogin userLogin, string createdUser, int failureTypeId, int authenticationMethod, CancellationToken token = default)
        {
            var userLoginRepository = new UserLoginRepository(dbContext, createdUser);
            userLogin.Failed = 1;
            userLogin.FailureTypeId = failureTypeId;
            userLogin.AuthenticationTypeId = authenticationMethod;
            return userLoginRepository.InsertAsync(userLogin, token);
        }

        private Task LogLoginSuccessAsync(UserLogin userLogin, string createdUser, int authenticationMethod, CancellationToken token = default)
        {
            var userLoginRepository = new UserLoginRepository(dbContext, createdUser);
            userLogin.Failed = 0;
            userLogin.AuthenticationTypeId = authenticationMethod;
            return userLoginRepository.InsertAsync(userLogin, token);
        }

        private static string DecryptPassword(string encryptedPassword, string privateKeyBase64)
        {
            var privateKeyPem = Encoding.UTF8.GetString(Convert.FromBase64String(privateKeyBase64));

            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            var encryptedBytes = Convert.FromBase64String(encryptedPassword);
            var decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        private static void ValidatePasswordStrength(string password)
        {
            var failures = Rules
                .Where(rule => !rule.Test(password))
                .Select(rule => rule.Message)
                .ToList();

            if (failures.Count > 0)
            {
                throw new PasswordStrengthException(failures);
            }
        }
    }
}
