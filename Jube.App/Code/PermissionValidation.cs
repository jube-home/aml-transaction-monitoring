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

namespace Jube.App.Code
{
    using System.Linq;
    using Data.Context;
    using Data.Security;

    public class PermissionValidation
    {
        private readonly PermissionValidationDto permissionValidationDto;

        public PermissionValidation(DbContext dbContext, string userName)
        {
            var permissionValidation = new Data.Security.PermissionValidation();
            permissionValidationDto = permissionValidation.GetPermissionsAsync(dbContext, userName).Result;
        }

        public PermissionValidation(string connectionString, string userName)
        {
            var permissionValidation = new Data.Security.PermissionValidation();
            permissionValidationDto = permissionValidation.GetPermissionsAsync(connectionString, userName).Result;
        }

        public bool Landlord
        {
            get
            {
                return permissionValidationDto.Landlord;
            }
        }

        public bool Validate(int[] testPermissionSpecifications)
        {
            return testPermissionSpecifications.Any(testPermissionSpecification
                => permissionValidationDto.Permissions.Contains(testPermissionSpecification));
        }
    }
}
