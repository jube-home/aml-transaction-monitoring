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

namespace Jube.CLI
{
    using UserRegistry;

    public static class CommandLine
    {
        public static void Main(string?[] args)
        {
            string? connectionString = null;

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-cs")//Connection String
                {
                    connectionString = args[i + 1];
                }

                if (args[i] != "-urpr")
                {
                    continue;//User Registry Password Reset
                }

                var hash = args[i + 1];
                var userName = args[i + 2];
                var password = args[i + 3];

                if (String.IsNullOrEmpty(hash))
                {
                    Console.WriteLine(@"User Registry Password Reset: No hash passed in arguments.");
                    return;
                }

                if (String.IsNullOrEmpty(userName))
                {
                    Console.WriteLine(@"User Registry Password Reset: No User Name passed in arguments.");
                    return;
                }

                if (String.IsNullOrEmpty(password))
                {
                    Console.WriteLine(@"User Registry Password Reset: No password passed in arguments.");
                    return;
                }

                if (String.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine(
                        @"User Registry Password Reset: No database connection string passed in arguments.");
                    return;
                }

                PasswordReset.Execute(connectionString, hash, userName, password);
            }
        }
    }
}
