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

    public class EntityAnalysisModelReprocessingRuleInstanceRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelReprocessingRuleInstanceRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelReprocessingRuleInstanceRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<EntityAnalysisModelReprocessingRuleInstance>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelReprocessingRuleInstance
                .Where(w =>
                    w.EntityAnalysisModelReprocessingRule.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                    || !tenantRegistryId.HasValue)
                .ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelReprocessingRuleInstance>> GetByEntityAnalysisModelsReprocessingRuleIdAsync(
            int entityAnalysisModelsReprocessingRuleId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelReprocessingRuleInstance
                .Where(w =>
                    (w.EntityAnalysisModelReprocessingRule.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                     || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelReprocessingRuleId == entityAnalysisModelsReprocessingRuleId &&
                    (w.Deleted == 0 || w.Deleted == null))
                .OrderByDescending(o => o.Id).ToListAsync(token);
        }

        public Task<EntityAnalysisModelReprocessingRuleInstance> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelReprocessingRuleInstance.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModelReprocessingRule.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                 || !tenantRegistryId.HasValue)
                && w.Id == id && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<EntityAnalysisModelReprocessingRuleInstance> InsertAsync(EntityAnalysisModelReprocessingRuleInstance model, CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<EntityAnalysisModelReprocessingRuleInstance> InsertByExistingUpdateUncompletedAsync(
            EntityAnalysisModelReprocessingRuleInstance model, CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelReprocessingRuleInstance
                .FirstOrDefaultAsync(w =>
                        (w.EntityAnalysisModelReprocessingRule.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                         || !tenantRegistryId.HasValue)
                        && w.EntityAnalysisModelReprocessingRuleId
                        == model.EntityAnalysisModelReprocessingRuleId
                        && (w.Deleted == 0 || w.Deleted == null)
                        && w.StatusId != 4
                    , token);

            if (existing != null)
            {
                throw new KeyNotFoundException();
            }

            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.StatusId = 0;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<EntityAnalysisModelReprocessingRuleInstance> UpdateCountsAsync
            (int id, int sampledCount, int matchedCount, int processedCount, int errorCount, DateTime referenceDate, CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelReprocessingRuleInstance
                .FirstOrDefaultAsync(w => w.Id
                    == id && (w.Deleted == 0 || w.Deleted == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            existing.SampledCount = sampledCount;
            existing.MatchedCount = matchedCount;
            existing.ProcessedCount = processedCount;
            existing.ErrorCount = errorCount;
            existing.ReferenceDate = referenceDate;
            existing.UpdatedDate = DateTime.UtcNow;
            existing.StatusId = 3;

            await dbContext.UpdateAsync(existing, token: token);

            return existing;
        }

        public async Task<EntityAnalysisModelReprocessingRuleInstance> UpdateAsync(EntityAnalysisModelReprocessingRuleInstance model, CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelReprocessingRuleInstance
                .FirstOrDefaultAsync(w => w.Id
                                          == model.Id
                                          && (w.EntityAnalysisModelReprocessingRule.EntityAnalysisModel.TenantRegistryId ==
                                              tenantRegistryId
                                              || !tenantRegistryId.HasValue)
                                          && (w.Deleted == 0 || w.Deleted == null), token).ConfigureAwait(false);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Id = existing.Id;

            var id = await dbContext
                .InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            await DeleteAsync(existing.Id, token).ConfigureAwait(false);

            model.Id = id;

            return model;
        }

        public Task UpdateReferenceDateCountAsync(int id, long availableCount, DateTime referenceDate, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelReprocessingRuleInstance
                .Where(d =>
                    d.Id == id)
                .Set(s => s.AvailableCount, availableCount)
                .Set(s => s.StatusId, (byte)2)
                .Set(s => s.ReferenceDate, referenceDate)
                .UpdateAsync(token);
        }

        public Task UpdateCompletedAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelReprocessingRuleInstance
                .Where(d =>
                    d.Id == id)
                .Set(s => s.CompletedDate, DateTime.UtcNow)
                .Set(s => s.StatusId, (byte)4)
                .UpdateAsync(token);
        }

        public Task DeleteAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelReprocessingRuleInstance
                .Where(d =>
                    (d.EntityAnalysisModelReprocessingRule.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                     || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);
        }
    }
}
