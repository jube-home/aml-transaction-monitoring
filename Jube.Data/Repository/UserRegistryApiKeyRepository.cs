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
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Poco;

    public class UserRegistryApiKeyRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public UserRegistryApiKeyRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public UserRegistryApiKeyRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public Task<List<UserRegistryApiKey>> GetAllAsync(CancellationToken token = default)
        {
            return dbContext.UserRegistryApiKey
                .Where(w => (w.Deleted == 0 || w.Deleted == null)
                            && (w.UserRegistry.RoleRegistry.Deleted == 0 || w.UserRegistry.RoleRegistry.Deleted == null))
                .ToListAsync(token);
        }

        public Task<UserRegistryApiKey> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.UserRegistryApiKey
                .Where(w => (w.Deleted == 0 || w.Deleted == null) && w.Id == id)
                .FirstOrDefaultAsync(token);
        }

        public Task<List<UserRegistryApiKey>> GetByUserRegistryIdAsync(int userRegistryId, CancellationToken token = default)
        {
            return dbContext.UserRegistryApiKey
                .Where(w =>
                    w.UserRegistry.RoleRegistry.TenantRegistryId == tenantRegistryId
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.UserRegistry.RoleRegistry.Deleted == 0 || w.UserRegistry.RoleRegistry.Deleted == null)
                    && w.UserRegistryId == userRegistryId).ToListAsync(token);
        }

        public async Task<UserRegistryApiKey> InsertAsync(UserRegistryApiKey model, CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.Now;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.UserRegistryApiKey
                .Where(d =>
                    (d.UserRegistry.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }
    }
}
