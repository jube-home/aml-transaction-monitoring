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
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Data.Security;
    using Dto.Authentication;
    using Exceptions.Authentication;

    public class Authentication(DbContext dbContext)
    {
        public async Task AuthenticateByUserNamePasswordAsync(AuthenticationRequestDto authenticationRequestDto,
            string? passwordHashingKey, CancellationToken token = default)
        {
            var userRegistryRepository = new UserRegistryRepository(dbContext);
            var userRegistry = await userRegistryRepository.GetByUserNameAsync(authenticationRequestDto.UserName, token);

            var userLogin = new UserLogin
            {
                RemoteIp = authenticationRequestDto.RemoteIp,
                LocalIp = authenticationRequestDto.UserAgent
            };

            if (userRegistry == null)
            {
                await LogLoginFailedAsync(userLogin, authenticationRequestDto.UserName ?? "", 1, token);
                throw new NoUserException();
            }

            if (userRegistry.Active != 1)
            {
                await LogLoginFailedAsync(userLogin, userRegistry.Name, 2, token);
                throw new NotActiveException();
            }

            if (userRegistry.PasswordLocked == 1)
            {
                await LogLoginFailedAsync(userLogin, userRegistry.Name, 3, token);
                throw new PasswordLockedException();
            }

            if (String.IsNullOrEmpty(userRegistry.Password))
            {
                await LogLoginFailedAsync(userLogin, userRegistry.Name, 6, token);
                throw new PasswordEmptyException();
            }

            if (!HashPassword.Verify(userRegistry.Password, authenticationRequestDto.Password, passwordHashingKey))
            {
                await userRegistryRepository.IncrementFailedPasswordAsync(userRegistry.Id, token);

                if (String.IsNullOrEmpty(authenticationRequestDto.NewPassword))
                {
                    if (!userRegistry.PasswordExpiryDate.HasValue || !userRegistry.PasswordCreatedDate.HasValue)
                    {
                        await LogLoginFailedAsync(userLogin, userRegistry.Name, 4, token);
                        throw new PasswordNewMustChangeException();
                    }
                }

                if (userRegistry.FailedPasswordCount >= 8)
                {
                    await userRegistryRepository.SetLockedAsync(userRegistry.Id, token);
                }

                await LogLoginFailedAsync(userLogin, userRegistry.Name, 5, token);

                throw new BadCredentialsException();
            }

            if (!String.IsNullOrEmpty(authenticationRequestDto.NewPassword))
            {
                var hashedPassword = HashPassword.GenerateHash(authenticationRequestDto.NewPassword, passwordHashingKey);

                await userRegistryRepository.SetPasswordAsync(userRegistry.Id, hashedPassword, DateTime.Now.AddDays(90), token);
            }
            else
            {
                if (!userRegistry.PasswordExpiryDate.HasValue || !(DateTime.Now <= userRegistry.PasswordExpiryDate.Value))
                {
                    throw new PasswordExpiredException();
                }
            }

            await LogLoginSuccessAsync(userLogin, userRegistry.Name, token);

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

            if (!HashPassword.Verify(userRegistry.Password,
                    changePasswordRequestDto.Password, passwordHashingKey))
            {
                throw new BadCredentialsException();
            }

            var hashedPassword = HashPassword.GenerateHash(changePasswordRequestDto.NewPassword, passwordHashingKey);

            await userRegistryRepository.SetPasswordAsync(userRegistry.Id, hashedPassword, DateTime.Now.AddDays(90), token);
        }

        private Task LogLoginFailedAsync(UserLogin userLogin, string createdUser, int failureTypeId, CancellationToken token = default)
        {
            var userLoginRepository = new UserLoginRepository(dbContext, createdUser);
            userLogin.Failed = 1;
            userLogin.FailureTypeId = failureTypeId;
            return userLoginRepository.InsertAsync(userLogin, token);
        }

        private Task LogLoginSuccessAsync(UserLogin userLogin, string createdUser, CancellationToken token = default)
        {
            var userLoginRepository = new UserLoginRepository(dbContext, createdUser);
            userLogin.Failed = 0;
            return userLoginRepository.InsertAsync(userLogin, token);
        }
    }
}
