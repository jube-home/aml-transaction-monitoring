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
    using Poco;

    public class VisualisationRegistryParameterRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public VisualisationRegistryParameterRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public VisualisationRegistryParameterRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public VisualisationRegistryParameterRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public Task<VisualisationRegistryParameter> GetByNameVisualisationRegistryIdAsync(string name, int visualisationRegistryId, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistryParameter
                .FirstOrDefaultAsync(f =>
                    f.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                    && f.VisualisationRegistryId == visualisationRegistryId
                    && (f.Deleted == 0 || f.Deleted == null)
                    && f.Name.ToLower() == name.ToLower(), token);
        }

        public Task<List<VisualisationRegistryParameter>> GetByVisualisationRegistryDatasourceIdAsync(int visualisationRegistryDatasourceId, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistryParameter
                .Where(vrp => vrp.VisualisationRegistry.VisualisationRegistryDatasource
                                  .Any(vrd => vrd.Id == visualisationRegistryDatasourceId)
                              && vrp.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                              && (vrp.Deleted == 0 || vrp.Deleted == null))
                .ToListAsync(token);
        }

        public async Task<IEnumerable<VisualisationRegistryParameter>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.VisualisationRegistryParameter
                .Where(w =>
                    (w.VisualisationRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && (w.Deleted == 0 || w.Deleted == null)).ToListAsync(token);
        }

        public async Task<IEnumerable<VisualisationRegistryParameter>> GetByVisualisationRegistryIdOrderByIdAsync(
            int visualisationRegistryId, CancellationToken token = default)
        {
            return await dbContext.VisualisationRegistryParameter
                .Where(w =>
                    (w.VisualisationRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.VisualisationRegistryId == visualisationRegistryId &&
                    (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public async Task<IEnumerable<VisualisationRegistryParameter>> GetByVisualisationRegistryIdActiveOnlyAsync(
            int visualisationRegistryId, CancellationToken token = default)
        {
            return await dbContext.VisualisationRegistryParameter
                .Where(w =>
                    (w.VisualisationRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.VisualisationRegistryId == visualisationRegistryId
                    && w.Active == 1
                    && dbContext.VisualisationRegistryRole
                        .Where(r => r.VisualisationRegistryGuid == w.VisualisationRegistry.Guid
                                    && (r.Deleted == 0 || r.Deleted == null))
                        .Any(r => dbContext.RoleRegistry
                            .Where(rr => rr.Guid == r.RoleRegistryGuid)
                            .Any(rr => dbContext.UserRegistry
                                .Any(u => u.RoleRegistryId == rr.Id && u.Name == userName)))
                    && dbContext.VisualisationRegistryParameterRole
                        .Where(r => r.VisualisationRegistryParameterGuid == w.Guid
                                    && (r.Deleted == 0 || r.Deleted == null))
                        .Any(r => dbContext.RoleRegistry
                            .Where(rr => rr.Guid == r.RoleRegistryGuid)
                            .Any(rr => dbContext.UserRegistry
                                .Any(u => u.RoleRegistryId == rr.Id && u.Name == userName)))
                    && (w.Deleted == 0 || w.Deleted == null)).ToListAsync(token);
        }

        public Task<VisualisationRegistryParameter> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistryParameter.FirstOrDefaultAsync(w
                => (w.VisualisationRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                   && w.Id == id
                   && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<VisualisationRegistryParameter> InsertAsync(VisualisationRegistryParameter model, CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.Now;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<VisualisationRegistryParameter> UpdateAsync(VisualisationRegistryParameter model, CancellationToken token = default)
        {
            var existing = await dbContext.VisualisationRegistryParameter
                .FirstOrDefaultAsync(w =>
                    (w.VisualisationRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue) &&
                    w.Id == model.Id
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.Locked == 0 || w.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.Now;

            await dbContext.UpdateAsync(model, token: token);

            var mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<VisualisationRegistryParameter, VisualisationRegistryParameterVersion>();
            }));

            var audit = mapper.Map<VisualisationRegistryParameterVersion>(existing);
            audit.VisualisationRegistryParameterId = existing.Id;

            await dbContext.InsertAsync(audit, token: token);

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.VisualisationRegistryParameter
                .Where(d =>
                    (d.VisualisationRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Locked == 0 || d.Locked == null)
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

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistryParameter
                .Where(d =>
                    d.VisualisationRegistry.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .UpdateAsync(token);
        }
    }
}
