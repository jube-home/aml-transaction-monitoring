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
    using Context;
    using LinqToDB;
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

        public IEnumerable<EntityAnalysisModelActivationRuleSuppression> Get()
        {
            return dbContext.EntityAnalysisModelActivationRuleSuppression
                .Where(w =>
                    w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue
                );
        }

        public IEnumerable<EntityAnalysisModelActivationRuleSuppression> GetByEntityAnalysisModelGuidOrderById(
            Guid entityAnalysisModelGuid)
        {
            return dbContext.EntityAnalysisModelActivationRuleSuppression
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelGuid == entityAnalysisModelGuid
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id);
        }

        public EntityAnalysisModelActivationRuleSuppression GetById(int id)
        {
            return dbContext.EntityAnalysisModelActivationRuleSuppression.FirstOrDefault(w =>
                (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Id == id && (w.Deleted == 0 || w.Deleted == null));
        }

        public EntityAnalysisModelActivationRuleSuppression Insert(EntityAnalysisModelActivationRuleSuppression model)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.Now;
            model.Version = 1;
            model.Id = dbContext.InsertWithInt32Identity(model);
            return model;
        }

        public EntityAnalysisModelActivationRuleSuppression Update(EntityAnalysisModelActivationRuleSuppression model)
        {
            EntityAnalysisModelActivationRuleSuppression existing;

            if (model.Id != 0)
            {
                existing = dbContext.EntityAnalysisModelActivationRuleSuppression
                    .FirstOrDefault(w =>
                        w.Id == model.Id
                        && (w.Deleted == 0 || w.Deleted == null));
            }
            else
            {
                existing = dbContext.EntityAnalysisModelActivationRuleSuppression
                    .FirstOrDefault(w => w.SuppressionKey == model.SuppressionKey
                                         && w.SuppressionKeyValue == model.SuppressionKeyValue
                                         && w.EntityAnalysisModelGuid == model.EntityAnalysisModelGuid
                                         && w.EntityAnalysisModelActivationRuleName ==
                                         model.EntityAnalysisModelActivationRuleName
                                         && (w.Deleted == 0 || w.Deleted == null));
            }

            if (existing != null)
            {
                Delete(existing.Id);
            }
            else
            {
                model.CreatedUser = userName;
                model.CreatedDate = DateTime.Now;
                model.Version = 1;
                var id = dbContext.InsertWithInt32Identity(model);
                model.Id = id;
            }

            return model;
        }

        public void Delete(int id)
        {
            var records = dbContext.EntityAnalysisModelActivationRuleSuppression
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Set(s => s.DeletedUser, userName)
                .Update();

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public void DeleteByTenantRegistryIdOutsideOfInstance(int tenantRegistryIdOutsideOfInstance, int importId)
        {
            dbContext.EntityAnalysisModelActivationRuleSuppression
                .Where(d =>
                    d.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Update();
        }
    }
}
