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

    public class EntityAnalysisModelGatewayRuleRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelGatewayRuleRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelGatewayRuleRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public EntityAnalysisModelGatewayRuleRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public Task<EntityAnalysisModelGatewayRule> GetByNameEntityAnalysisModelIdAsync(string name, int entityAnalysisModelId, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelGatewayRule
                .FirstOrDefaultAsync(f =>
                    f.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                    && f.EntityAnalysisModelId == entityAnalysisModelId
                    && (f.Deleted == 0 || f.Deleted == null)
                    && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<EntityAnalysisModelGatewayRule>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelGatewayRule
                .Where(w =>
                    w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<EntityAnalysisModelGatewayRule>> GetByEntityAnalysisModelIdOrderByIdAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelGatewayRule
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<EntityAnalysisModelGatewayRule>> GetByEntityAnalysisModelIdOrderByPriorityAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelGatewayRule
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Priority).ToListAsync(token).ConfigureAwait(false);
        }

        public Task<EntityAnalysisModelGatewayRule> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelGatewayRule.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Id == id && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<EntityAnalysisModelGatewayRule> InsertAsync(EntityAnalysisModelGatewayRule model, CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.Now;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<EntityAnalysisModelGatewayRule> UpdateAsync(EntityAnalysisModelGatewayRule model, CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelGatewayRule
                .FirstOrDefaultAsync(w => w.Id
                                          == model.Id
                                          && w.EntityAnalysisModel.TenantRegistryId ==
                                          tenantRegistryId
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
                cfg.CreateMap<EntityAnalysisModelGatewayRule, EntityAnalysisModelGatewayRuleVersion>();
            }, NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelGatewayRuleVersion>(existing);
            audit.EntityAnalysisModelGatewayRuleId = existing.Id;

            await dbContext.InsertAsync(audit, token: token);

            return model;
        }

        public Task UpdateCounterAsync(int id, long evaluationCounter, long activationCounter, DateTime activationCounterDate, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelGatewayRule
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ActivationCounter, activationCounter)
                .Set(s => s.ActivationCounterDate, activationCounterDate)
                .Set(s => s.EvaluationCounter, evaluationCounter)
                .UpdateAsync(token);
        }

        public Task DeleteAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelGatewayRule
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Locked == 0 || d.Locked == null)
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelGatewayRule
                .Where(d =>
                    d.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .UpdateAsync(token);
        }
    }
}
