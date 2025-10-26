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

namespace Jube.CLI.UserRegistry
{
    using Data.Context;
    using Data.Repository;
    using Data.Security;

    public static class PasswordReset
    {
        public static void Execute(string? connectionString, string? hash, string? userName, string? password)
        {
            var dbContext = DataConnectionDbContext.GetDbContextDataConnection(connectionString);
            var repository = new UserRegistryRepository(dbContext);

            var userRegistry = repository.GetByUserName(userName);

            if (userRegistry != null)
            {
                repository.SetPassword(userRegistry.Id, HashPassword.GenerateHash(password, hash), DateTime.Now);
            }
            else
            {
                Console.WriteLine(@"User Registry Password Reset: User Name not found.");
            }

            dbContext.Close();
            dbContext.Dispose();
        }
    }
}
