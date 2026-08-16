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

    public class EntityAnalysisModelReprocessingRuleRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelReprocessingRuleRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public Task<EntityAnalysisModelReprocessingRule> GetByNameEntityAnalysisModelIdAsync(string name, int entityAnalysisModelId, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelReprocessingRule
                .FirstOrDefaultAsync(f =>
                    f.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                    && f.EntityAnalysisModelId == entityAnalysisModelId
                    && (f.Deleted == 0 || f.Deleted == null)
                    && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<EntityAnalysisModelReprocessingRule>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelReprocessingRule
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId).ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelReprocessingRule>> GetByEntityAnalysisModelIdAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelReprocessingRule
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.EntityAnalysisModelId == entityAnalysisModelId &&
                            (w.Deleted == 0 || w.Deleted == null)).ToListAsync(token);
        }

        public Task<EntityAnalysisModelReprocessingRule> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelReprocessingRule.FirstOrDefaultAsync(w =>
                w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && w.Id == id && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<EntityAnalysisModelReprocessingRule> InsertAsync(EntityAnalysisModelReprocessingRule model, CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<EntityAnalysisModelReprocessingRule> UpdateAsync(EntityAnalysisModelReprocessingRule model, CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelReprocessingRule
                .FirstOrDefaultAsync(w => w.Id
                                          == model.Id
                                          && (w.Deleted == 0 || w.Deleted == null)
                                          && (w.Locked == 0 || w.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Id = existing.Id;

            var id = await dbContext
                .InsertWithInt32IdentityAsync(model, token: token);

            await DeleteAsync(existing.Id, token);

            model.Id = id;

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.EntityAnalysisModelReprocessingRule
                .Where(d => d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && d.Id == id
                            && (d.Locked == 0 || d.Locked == null)
                            && (d.Deleted == 0 || d.Deleted == null))
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
