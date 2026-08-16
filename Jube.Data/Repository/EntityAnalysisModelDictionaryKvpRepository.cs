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

    public class EntityAnalysisModelDictionaryKvpRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelDictionaryKvpRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelDictionaryKvpRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public EntityAnalysisModelDictionaryKvpRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<EntityAnalysisModelDictionaryKvp>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelDictionaryKvp
                .Where(w =>
                    w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                    !tenantRegistryId.HasValue).ToListAsync(token);
        }

        public Task<EntityAnalysisModelDictionaryKvp> GetByIdKvpKeyAsync(int id, string key, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelDictionaryKvp.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                 !tenantRegistryId.HasValue)
                && w.EntityAnalysisModelDictionaryId == id && w.KvpKey == key
                && (w.Deleted == 0 || w.Deleted == null)
                && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);
        }

        public async Task<IEnumerable<EntityAnalysisModelDictionaryKvp>> GetByEntityAnalysisModelDictionaryIdOrderByIdAsync(
            int entityAnalysisModelDictionaryId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelDictionaryKvp
                .Where(w =>
                    (w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                     !tenantRegistryId.HasValue)
                    && (w.EntityAnalysisModelDictionary.EntityAnalysisModel.Deleted == 0 ||
                        w.EntityAnalysisModelDictionary.EntityAnalysisModel.Deleted == null)
                    && w.EntityAnalysisModelDictionaryId == entityAnalysisModelDictionaryId &&
                    (w.Deleted == 0 || w.Deleted == null)
                    && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow))
                .OrderBy(o => o.Id).ToListAsync(token).ConfigureAwait(false);
        }

        public Task<EntityAnalysisModelDictionaryKvp> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelDictionaryKvp.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                 !tenantRegistryId.HasValue)
                && w.EntityAnalysisModelDictionaryId == id && (w.Deleted == 0 || w.Deleted == null)
                && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);
        }

        public async Task<EntityAnalysisModelDictionaryKvp> InsertAsync(EntityAnalysisModelDictionaryKvp model, CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<EntityAnalysisModelDictionaryKvp>
            UpdateAsync(EntityAnalysisModelDictionaryKvp model, CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelDictionaryKvp
                .FirstOrDefaultAsync(w => w.Id == model.Id
                                          && w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId ==
                                          tenantRegistryId
                                          && (w.Deleted == 0 || w.Deleted == null)
                                          && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;

            await dbContext.UpdateAsync(model, token: token);

            var mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<EntityAnalysisModelDictionaryKvp, EntityAnalysisModelDictionaryKvpVersion>();
            }, NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelDictionaryKvpVersion>(existing);
            audit.EntityAnalysisModelDictionaryKvpId = existing.Id;

            await dbContext.InsertAsync(audit, token: token);

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.EntityAnalysisModelDictionaryKvp
                .Where(d =>
                    (d.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                     !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Deleted == 0 || d.Deleted == null)
                    && (d.DeleteExpiryDate == null || d.DeleteExpiryDate > DateTime.UtcNow))
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
            return dbContext.EntityAnalysisModelDictionaryKvp
                .Where(d =>
                    d.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
