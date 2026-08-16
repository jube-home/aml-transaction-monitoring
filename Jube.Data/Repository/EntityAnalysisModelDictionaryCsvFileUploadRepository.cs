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

    public class EntityAnalysisModelDictionaryCsvFileUploadRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelDictionaryCsvFileUploadRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public async Task<IEnumerable<EntityAnalysisModelDictionaryCsvFileUpload>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelDictionaryCsvFileUpload
                .Where(w => w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelDictionaryCsvFileUpload>> GetByEntityAnalysisModelDictionaryIdAsync(
            int entityAnalysisModelDictionaryId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelDictionaryCsvFileUpload
                .Where(w => w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.EntityAnalysisModelDictionary.Id == entityAnalysisModelDictionaryId &&
                            (w.EntityAnalysisModelDictionary.Deleted == 0 ||
                             w.EntityAnalysisModelDictionary.Deleted == null)).ToListAsync(token);
        }

        public Task<EntityAnalysisModelDictionaryCsvFileUpload> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelDictionaryCsvFileUpload.FirstOrDefaultAsync(w =>
                w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && w.EntityAnalysisModelDictionary.Id == id && (w.EntityAnalysisModelDictionary.Deleted == 0 ||
                                                                w.EntityAnalysisModelDictionary.Deleted == null), token);
        }

        public async Task<EntityAnalysisModelDictionaryCsvFileUpload> InsertAsync(EntityAnalysisModelDictionaryCsvFileUpload model, CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.InheritedId = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<EntityAnalysisModelDictionaryCsvFileUpload> UpdateAsync(
            EntityAnalysisModelDictionaryCsvFileUpload
                model, CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelDictionaryCsvFileUpload
                .FirstOrDefaultAsync(w => w.Id
                                          == model.Id
                                          && w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId ==
                                          tenantRegistryId
                                          && (w.EntityAnalysisModelDictionary.Deleted == 0 ||
                                              w.EntityAnalysisModelDictionary.Deleted == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.InheritedId = existing.Id;

            await DeleteAsync(existing.Id, token);

            var id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            model.Id = id;

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.EntityAnalysisModelDictionaryCsvFileUpload
                .Where(d => d.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && d.Id == id
                            && (d.EntityAnalysisModelDictionary.Deleted == 0 ||
                                d.EntityAnalysisModelDictionary.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }
    }
}
