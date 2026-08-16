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

namespace Jube.App.Dto.UserRegistry
{
    using System;

    public class UserRegistryDto
    {
        public int Id { get; set; }
        public Guid RoleRegistryGuid { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public DateTimeOffset? CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public int Version { get; set; }
        public bool PasswordLocked { get; set; }
        public bool Active { get; set; }
        public int InheritedId { get; set; }
        public bool WirePasswordHash { get; set; }
    }
}
