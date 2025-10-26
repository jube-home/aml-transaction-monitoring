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
        public void AuthenticateByUserNamePassword(AuthenticationRequestDto authenticationRequestDto,
            string? passwordHashingKey)
        {
            var userRegistryRepository = new UserRegistryRepository(dbContext);
            var userRegistry = userRegistryRepository.GetByUserName(authenticationRequestDto.UserName);

            var userLogin = new UserLogin
            {
                RemoteIp = authenticationRequestDto.RemoteIp,
                LocalIp = authenticationRequestDto.UserAgent
            };

            if (userRegistry == null)
            {
                LogLoginFailed(userLogin, authenticationRequestDto.UserName ?? "", 1);
                throw new NoUserException();
            }

            if (userRegistry.Active != 1)
            {
                LogLoginFailed(userLogin, userRegistry.Name, 2);
                throw new NotActiveException();
            }

            if (userRegistry.PasswordLocked == 1)
            {
                LogLoginFailed(userLogin, userRegistry.Name, 3);
                throw new PasswordLockedException();
            }

            if (!userRegistry.PasswordExpiryDate.HasValue
                || String.IsNullOrEmpty(userRegistry.Password)
                || !userRegistry.PasswordCreatedDate.HasValue)
            {
                LogLoginFailed(userLogin, userRegistry.Name, 4);
                throw new PasswordNewMustChangeException();
            }

            if (!HashPassword.Verify(userRegistry.Password, authenticationRequestDto.Password, passwordHashingKey))
            {
                userRegistryRepository.IncrementFailedPassword(userRegistry.Id);

                if (userRegistry.FailedPasswordCount > 8)
                {
                    userRegistryRepository.SetLocked(userRegistry.Id);
                }

                LogLoginFailed(userLogin, userRegistry.Name, 5);

                throw new BadCredentialsException();
            }

            if (!String.IsNullOrEmpty(authenticationRequestDto.NewPassword))
            {
                var hashedPassword = HashPassword.GenerateHash(authenticationRequestDto.NewPassword, passwordHashingKey);

                userRegistryRepository.SetPassword(userRegistry.Id, hashedPassword, DateTime.Now.AddDays(90));
            }
            else
            {
                if (!(DateTime.Now <= userRegistry.PasswordExpiryDate.Value))
                {
                    throw new PasswordExpiredException();
                }
            }

            LogLoginSuccess(userLogin, userRegistry.Name);

            if (userRegistry.FailedPasswordCount > 0)
            {
                userRegistryRepository.ResetFailedPasswordCount(userRegistry.Id);
            }

        }

        public void ChangePassword(string? userName, ChangePasswordRequestDto changePasswordRequestDto,
            string? passwordHashingKey)
        {
            var userRegistryRepository = new UserRegistryRepository(dbContext);
            var userRegistry = userRegistryRepository.GetByUserName(userName);

            if (!HashPassword.Verify(userRegistry.Password,
                    changePasswordRequestDto.Password, passwordHashingKey))
            {
                throw new BadCredentialsException();
            }

            var hashedPassword = HashPassword.GenerateHash(changePasswordRequestDto.NewPassword, passwordHashingKey);

            userRegistryRepository.SetPassword(userRegistry.Id, hashedPassword, DateTime.Now.AddDays(90));
        }

        private void LogLoginFailed(UserLogin userLogin, string createdUser, int failureTypeId)
        {
            var userLoginRepository = new UserLoginRepository(dbContext, createdUser);
            userLogin.Failed = 1;
            userLogin.FailureTypeId = failureTypeId;
            userLoginRepository.Insert(userLogin);
        }

        private void LogLoginSuccess(UserLogin userLogin, string createdUser)
        {
            var userLoginRepository = new UserLoginRepository(dbContext, createdUser);
            userLogin.Failed = 0;
            userLoginRepository.Insert(userLogin);
        }
    }
}
