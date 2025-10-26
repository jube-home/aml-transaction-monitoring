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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Context;
    using LinqToDB;
    using Poco;

    public class UserInTenantRepository
    {
        private readonly DbContext dbContext;
        private readonly UserInTenant userInTenant;
        private readonly string userName;

        public UserInTenantRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            userInTenant = this.dbContext.UserInTenant.FirstOrDefault(w => w.User == userName);
            this.userName = userName;
        }

        public void Update(string user, int tenantRegistryId)
        {
            var existing = dbContext.UserInTenant
                .FirstOrDefault(w => w.User == user);

            if (existing != null)
            {
                existing.TenantRegistryId = tenantRegistryId;
                existing.SwitchedUser = userName;
                existing.SwitchedDate = DateTime.Now;

                dbContext.Update(existing);

                var userInTenantSwitchLog = new UserInTenantSwitchLog
                {
                    TenantRegistryId = tenantRegistryId,
                    SwitchedDate = existing.SwitchedDate,
                    SwitchedUser = userName,
                    UserInTenantId = existing.Id
                };

                dbContext.Insert(userInTenantSwitchLog);
            }
            else
            {
                dbContext.Insert(new UserInTenant
                {
                    TenantRegistryId = tenantRegistryId,
                    SwitchedUser = userName,
                    SwitchedDate = DateTime.Now
                });
            }
        }

        public UserInTenant GetCurrentTenantRegistry()
        {
            return userInTenant;
        }

        public IEnumerable<UserInTenant> Get()
        {
            return dbContext.UserInTenant.Where(w =>
                w.TenantRegistryId == userInTenant.TenantRegistryId);
        }
    }
}
