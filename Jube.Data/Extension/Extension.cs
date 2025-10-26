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

using System;

namespace Jube.Data.Extension
{
    public static class Extension
    {
        public static string AsString(this object obj)
        {
            return (string)obj;
        }

        public static double AsDouble(this object obj)
        {
            return (double)obj;
        }

        public static int AsInt(this object obj)
        {
            return (int)obj;
        }

        public static long AsLong(this object obj)
        {
            return (long)obj;
        }

        public static short AsShort(this object obj)
        {
            return (short)obj;
        }

        public static byte AsByte(this object obj)
        {
            return (byte)obj;
        }

        public static Guid AsGuid(this object obj)
        {
            return Guid.Parse(obj.ToString() ?? string.Empty);
        }

        public static DateTime AsDateTime(this object obj)
        {
            return Convert.ToDateTime(obj);
        }
    }
}