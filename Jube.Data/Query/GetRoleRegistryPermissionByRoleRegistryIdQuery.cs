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

namespace Jube.Data.Query
{
    using System.Collections.Generic;
    using System.Linq;
    using Context;

    public class GetRoleRegistryPermissionByRoleRegistryIdQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetRoleRegistryPermissionByRoleRegistryIdQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public IEnumerable<Dto> Execute(int roleRegistryId)
        {
            return dbContext.RoleRegistryPermission
                .Where(w => w.RoleRegistryId == roleRegistryId
                            && w.RoleRegistry.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .Select(s => new Dto
                {
                    Name = s.PermissionSpecification.Name,
                    Id = s.Id,
                    Active = s.Active == 1,
                    RoleRegistryId = s.RoleRegistryId.Value
                }).ToList();
        }

        public class Dto
        {
            public string Name { get; set; }
            public int Id { get; set; }
            public bool Active { get; set; }
            public int RoleRegistryId { get; set; }
        }
    }
}
