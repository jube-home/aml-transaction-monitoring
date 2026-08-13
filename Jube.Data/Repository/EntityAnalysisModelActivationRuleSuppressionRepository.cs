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

    public class EntityAnalysisModelActivationRuleSuppressionRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelActivationRuleSuppressionRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelActivationRuleSuppressionRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }


        public EntityAnalysisModelActivationRuleSuppressionRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<EntityAnalysisModelActivationRuleSuppression>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelActivationRuleSuppression
                .Where(w =>
                    w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue
                ).ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelActivationRuleSuppression>> GetByEntityAnalysisModelGuidOrderByIdAsync(
            Guid entityAnalysisModelGuid, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelActivationRuleSuppression
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelGuid == entityAnalysisModelGuid
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow))
                .OrderBy(o => o.Id).ToListAsync(token).ConfigureAwait(false);
        }

        public Task<EntityAnalysisModelActivationRuleSuppression> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelActivationRuleSuppression.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Id == id && (w.Deleted == 0 || w.Deleted == null)
                && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);
        }

        public async Task<EntityAnalysisModelActivationRuleSuppression> InsertAsync(EntityAnalysisModelActivationRuleSuppression model, CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<EntityAnalysisModelActivationRuleSuppression> UpdateAsync(EntityAnalysisModelActivationRuleSuppression model, CancellationToken token = default)
        {
            EntityAnalysisModelActivationRuleSuppression existing;

            if (model.Id != 0)
            {
                existing = await dbContext.EntityAnalysisModelActivationRuleSuppression
                    .FirstOrDefaultAsync(w =>
                        w.Id == model.Id
                        && (w.Deleted == 0 || w.Deleted == null)
                        && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);
            }
            else
            {
                existing = await dbContext.EntityAnalysisModelActivationRuleSuppression
                    .FirstOrDefaultAsync(w => w.SuppressionKey == model.SuppressionKey
                                              && w.SuppressionKeyValue == model.SuppressionKeyValue
                                              && w.EntityAnalysisModelGuid == model.EntityAnalysisModelGuid
                                              && w.EntityAnalysisModelActivationRuleName ==
                                              model.EntityAnalysisModelActivationRuleName
                                              && (w.Deleted == 0 || w.Deleted == null)
                                              && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);
            }

            if (existing != null)
            {
                await DeleteAsync(existing.Id, token);
            }
            else
            {
                model.CreatedUser = userName;
                model.CreatedDate = DateTime.UtcNow;
                model.Version = 1;
                var id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
                model.Id = id;
            }

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelActivationRuleSuppression
                .FirstOrDefaultAsync(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.Id == id
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            await dbContext.EntityAnalysisModelActivationRuleSuppression
                .Where(d => d.Id == id)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);

            await InsertVersionAsync(existing, token);
        }

        public async Task<EntityAnalysisModelActivationRuleSuppression> UpdateDeleteExpiryDateAsync(
            Guid entityAnalysisModelGuid, string suppressionKey, string suppressionKeyValue,
            string entityAnalysisModelActivationRuleName, DateTime? deleteExpiryDate, CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelActivationRuleSuppression
                .FirstOrDefaultAsync(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.SuppressionKey == suppressionKey
                    && w.SuppressionKeyValue == suppressionKeyValue
                    && w.EntityAnalysisModelGuid == entityAnalysisModelGuid
                    && w.EntityAnalysisModelActivationRuleName == entityAnalysisModelActivationRuleName
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.DeleteExpiryDate == null || w.DeleteExpiryDate > DateTime.UtcNow), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            var version = (existing.Version ?? 0) + 1;

            await dbContext.EntityAnalysisModelActivationRuleSuppression
                .Where(w => w.Id == existing.Id)
                .Set(s => s.DeleteExpiryDate, deleteExpiryDate)
                .Set(s => s.Version, version)
                .UpdateAsync(token);

            await InsertVersionAsync(existing, token);

            existing.DeleteExpiryDate = deleteExpiryDate;
            existing.Version = version;
            return existing;
        }

        private Task InsertVersionAsync(EntityAnalysisModelActivationRuleSuppression existing, CancellationToken token)
        {
            var mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<EntityAnalysisModelActivationRuleSuppression, EntityAnalysisModelActivationRuleSuppressionVersion>();
            }, NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelActivationRuleSuppressionVersion>(existing);
            audit.EntityAnalysisModelActivationRuleSuppressionId = existing.Id;

            return dbContext.InsertAsync(audit, token: token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelActivationRuleSuppression
                .Where(d =>
                    d.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
