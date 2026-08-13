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
    using AutoMapper;
    using Context;
    using LinqToDB;
    using Microsoft.Extensions.Logging.Abstractions;
    using Poco;

    public class RoleRegistryRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public RoleRegistryRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public RoleRegistryRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public Task<List<RoleRegistry>> GetAllTenantsAsync(CancellationToken token = default)
        {
            return dbContext.RoleRegistry.Where(w => w.Deleted == null || w.Deleted == 0).ToListAsync(token);
        }

        public Task<RoleRegistry> GetByNameAsync(string name, CancellationToken token = default)
        {
            return dbContext.RoleRegistry
                .FirstOrDefaultAsync(f =>
                    f.TenantRegistryId == tenantRegistryId
                    && (f.Deleted == 0 || f.Deleted == null)
                    && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<RoleRegistry>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.RoleRegistry.Where(w => w.TenantRegistryId == tenantRegistryId
                                                           && (w.Deleted == 0 || w.Deleted == null)).ToListAsync(token);
        }

        public Task<RoleRegistry> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.RoleRegistry.FirstOrDefaultAsync(w => w.Id == id
                                                                   && w.TenantRegistryId == tenantRegistryId
                                                                   && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<RoleRegistry> InsertAsync(RoleRegistry model, CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.TenantRegistryId = tenantRegistryId;
            model.Version = 1;
            model.CreatedDate = DateTime.UtcNow;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<RoleRegistry> UpdateAsync(RoleRegistry model, CancellationToken token = default)
        {
            var existing = await dbContext.RoleRegistry
                .FirstOrDefaultAsync(u => u.TenantRegistryId == tenantRegistryId
                                          && u.Id == model.Id
                                          && (u.Deleted == 0 || u.Deleted == null)
                                          && (u.Locked == 0 || u.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.TenantRegistryId = tenantRegistryId;

            await dbContext.UpdateAsync(model, token: token);

            var mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RoleRegistry, RoleRegistryVersion>();
            }, NullLoggerFactory.Instance));

            var audit = mapper.Map<RoleRegistryVersion>(existing);
            audit.RoleRegistryId = existing.Id;

            await dbContext.InsertAsync(audit, token: token);

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.RoleRegistry
                .Where(d => d.Id == id
                            && d.TenantRegistryId == tenantRegistryId
                            && (d.Deleted == 0 || d.Deleted == null)
                            && (d.Locked == 0 || d.Locked == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId, CancellationToken token = default)
        {
            return dbContext.RoleRegistry
                .Where(d => d.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
