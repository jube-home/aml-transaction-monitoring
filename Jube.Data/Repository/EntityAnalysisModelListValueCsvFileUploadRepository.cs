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

    public class EntityAnalysisModelListValueCsvFileUploadRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelListValueCsvFileUploadRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public async Task<IEnumerable<EntityAnalysisModelListCsvFileUpload>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelListCsvFileUpload
                .Where(w => w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelListCsvFileUpload>> GetByEntityAnalysisModelListIdAsync(
            int entityAnalysisModelListId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelListCsvFileUpload
                .Where(w => w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.EntityAnalysisModelList.Id == entityAnalysisModelListId &&
                            (w.EntityAnalysisModelList.Deleted == 0 || w.EntityAnalysisModelList.Deleted == null))
                .ToListAsync(token);
        }

        public Task<EntityAnalysisModelListCsvFileUpload> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelListCsvFileUpload.FirstOrDefaultAsync(w =>
                w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && w.EntityAnalysisModelList.Id == id &&
                (w.EntityAnalysisModelList.Deleted == 0 || w.EntityAnalysisModelList.Deleted == null), token);
        }

        public async Task<EntityAnalysisModelListCsvFileUpload> InsertAsync(EntityAnalysisModelListCsvFileUpload model, CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.InheritedId = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<EntityAnalysisModelListCsvFileUpload>
            UpdateAsync(EntityAnalysisModelListCsvFileUpload model, CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelListCsvFileUpload
                .FirstOrDefaultAsync(w => w.Id == model.Id
                                          && w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                                          && (w.EntityAnalysisModelList.Deleted == 0 ||
                                              w.EntityAnalysisModelList.Deleted == null), token);

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
            var records = await dbContext.EntityAnalysisModelListCsvFileUpload
                .Where(d => d.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && d.Id == id
                            && (d.EntityAnalysisModelList.Deleted == 0 || d.EntityAnalysisModelList.Deleted == null))
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
