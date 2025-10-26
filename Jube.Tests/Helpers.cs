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

namespace Jube.Test
{
    using System;
    using System.IO;
    using System.Reflection;

    public static class Helpers
    {
        public static string ReadFileContents(string filePath)
        {
            return File.ReadAllText(Path.Combine(
                GetParentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, 4) ?? String.Empty,
                $"Jube.Tests/{filePath}"));
        }

        private static string? GetParentDirectory(string? path, int parentCount)
        {
            while (true)
            {
                if (String.IsNullOrEmpty(path) || parentCount < 1)
                {
                    return path;
                }

                var parent = Path.GetDirectoryName(path);

                if (--parentCount <= 0 || parent == null)
                {
                    return parent;
                }

                path = parent;
            }
        }
    }
}
