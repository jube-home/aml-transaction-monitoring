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

namespace Jube.Service.Security
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Jube.Data.Context;
    using Jube.Data.Security;
    using log4net;
    using DataPermissionValidation = Jube.Data.Security.PermissionValidation;

    public sealed class PermissionValidation
    {
        private readonly PermissionValidationDto dto;

        private PermissionValidation(PermissionValidationDto dto) => this.dto = dto;

        public static async Task<PermissionValidation> CreateAsync(
            DbContext dbContext, string userName, ILog log, CancellationToken token = default)
        {
            var loaded = await new DataPermissionValidation()
                .GetPermissionsAsync(dbContext, userName, log).ConfigureAwait(false);
            
            return new PermissionValidation(loaded);
        }

        public bool Landlord => dto.Landlord;
        public bool Validate(int[] specs) => specs.Any(s => dto.Permissions.Contains(s));
    }
}