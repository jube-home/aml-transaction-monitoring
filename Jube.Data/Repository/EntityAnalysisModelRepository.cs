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

    public class EntityAnalysisModelRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public EntityAnalysisModelRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public Task<List<EntityAnalysisModel>> GetAllTenantsAsync(CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModel.Where(w => w.Deleted == null || w.Deleted == 0).ToListAsync(token);
        }

        public Task<EntityAnalysisModel> GetByNameAsync(string name, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModel
                .FirstOrDefaultAsync(f =>
                    f.TenantRegistryId == tenantRegistryId
                    && (f.Deleted == 0 || f.Deleted == null)
                    && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<EntityAnalysisModel>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModel.Where(w =>
                (w.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && (w.Deleted == null || w.Deleted == 0)).ToListAsync(token).ConfigureAwait(false);
        }

        public Task<EntityAnalysisModel> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModel.FirstOrDefaultAsync(w
                => (w.TenantRegistryId == tenantRegistryId || !w.TenantRegistryId.HasValue)
                   && w.Id == id && (w.Deleted == null || w.Deleted == 0), token);
        }

        public Task<EntityAnalysisModel> GetByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModel.FirstOrDefaultAsync(w
                => (w.TenantRegistryId == tenantRegistryId || !w.TenantRegistryId.HasValue)
                   && w.Guid == guid && (w.Deleted == null || w.Deleted == 0), token);
        }

        public async Task<EntityAnalysisModel> InsertAsync(EntityAnalysisModel model, CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.TenantRegistryId = tenantRegistryId;
            model.Version = 1;
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<EntityAnalysisModel> UpdateAsync(EntityAnalysisModel model, CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModel
                .FirstOrDefaultAsync(w => w.Id
                                          == model.Id
                                          && (w.TenantRegistryId == tenantRegistryId || !w.TenantRegistryId.HasValue)
                                          && (w.Deleted == 0 || w.Deleted == null)
                                          && (w.Locked == 0 || w.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.TenantRegistryId = tenantRegistryId;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;

            await dbContext.UpdateAsync(model, token: token);

            var mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<EntityAnalysisModel, EntityAnalysisModelVersion>();
            }, NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelVersion>(existing);
            audit.EntityAnalysisModelId = existing.Id;

            await dbContext.InsertAsync(audit, token: token);

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.EntityAnalysisModel
                .Where(d => (d.TenantRegistryId == tenantRegistryId || !d.TenantRegistryId.HasValue)
                            && d.Id == id
                            && (d.Deleted == 0 || d.Deleted == null)
                            && (d.Locked == 0 || d.Locked == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModel
                .Where(d => d.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
