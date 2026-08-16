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

    public class VisualisationRegistryRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public VisualisationRegistryRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public VisualisationRegistryRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public Task<List<VisualisationRegistry>> GetAllTenantsAsync(CancellationToken token = default)
        {
            return dbContext.VisualisationRegistry.Where(w => w.Deleted == null || w.Deleted == 0).ToListAsync(token);
        }

        public Task<VisualisationRegistry> GetByNameAsync(string name, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistry
                .FirstOrDefaultAsync(f =>
                    f.TenantRegistryId == tenantRegistryId
                    && (f.Deleted == 0 || f.Deleted == null)
                    && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<VisualisationRegistry>> GetOrderByIdAsync(CancellationToken token = default)
        {
            return await dbContext.VisualisationRegistry.Where(w =>
                    w.TenantRegistryId == tenantRegistryId
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public Task<VisualisationRegistry> GetByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistry.FirstOrDefaultAsync(w => w.Guid == guid
                                                                            && w.TenantRegistryId == tenantRegistryId
                                                                            && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<VisualisationRegistry> GetByGuidActiveOnlyAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistry.FirstOrDefaultAsync(w => w.Guid == guid
                                                                            && w.TenantRegistryId == tenantRegistryId
                                                                            && w.Active == 1
                                                                            && dbContext.VisualisationRegistryRole
                                                                                .Where(r => r.VisualisationRegistryGuid == w.Guid
                                                                                            && (r.Deleted == 0 || r.Deleted == null))
                                                                                .Any(r => dbContext.RoleRegistry
                                                                                    .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                                                                                    .Any(rr => dbContext.UserRegistry
                                                                                        .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                                                                            && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<VisualisationRegistry> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistry.FirstOrDefaultAsync(w => w.Id == id
                                                                            && w.TenantRegistryId == tenantRegistryId
                                                                            && (w.Deleted == 0 || w.Deleted == null), token);
        }


        public async Task<IEnumerable<VisualisationRegistry>> GetByShowInDirectoryActiveOrderByIdDescAsync(CancellationToken token = default)
        {
            return await dbContext.VisualisationRegistry.Where(w => w.ShowInDirectory == 1
                                                                    && w.Active == 1
                                                                    && dbContext.VisualisationRegistryRole
                                                                        .Where(r => r.VisualisationRegistryGuid == w.Guid
                                                                                    && (r.Deleted == 0 || r.Deleted == null))
                                                                        .Any(r => dbContext.RoleRegistry
                                                                            .Where(rr => rr.Guid == r.RoleRegistryGuid)
                                                                            .Any(rr => dbContext.UserRegistry
                                                                                .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                                                                    && w.TenantRegistryId == tenantRegistryId
                                                                    && (w.Deleted == 0 || w.Deleted == null)).OrderBy(o => o.Id).ToListAsync(token);
        }


        public async Task<VisualisationRegistry> InsertAsync(VisualisationRegistry model, CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.TenantRegistryId = tenantRegistryId;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public async Task<VisualisationRegistry> UpdateAsync(VisualisationRegistry model, CancellationToken token = default)
        {
            var existing = await dbContext.VisualisationRegistry.FirstOrDefaultAsync(w => w.Id
                                                                                          == model.Id
                                                                                          && w.TenantRegistryId == tenantRegistryId
                                                                                          && (w.Deleted == 0 || w.Deleted == null)
                                                                                          && (w.Locked == 0 || w.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.TenantRegistryId = tenantRegistryId;
            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;

            await dbContext.UpdateAsync(model, token: token);

            var mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<VisualisationRegistry, VisualisationRegistryVersion>();
            }, NullLoggerFactory.Instance));

            var audit = mapper.Map<VisualisationRegistryVersion>(existing);
            audit.VisualisationRegistryId = existing.Id;

            return model;
        }

        public Task DeleteAsync(int id, CancellationToken token = default)
        {
            var record = dbContext.VisualisationRegistry
                .FirstOrDefault(u => u.TenantRegistryId == tenantRegistryId
                                     && u.Id == id);

            if (record == null)
            {
                throw new KeyNotFoundException();
            }

            record.DeletedUser = userName;
            record.DeletedDate = DateTime.UtcNow;
            record.Deleted = 1;
            return dbContext.UpdateAsync(record, token: token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistry
                .Where(d => d.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
