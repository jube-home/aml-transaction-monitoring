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

    public class EntityAnalysisModelAsynchronousQueueBalanceRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public EntityAnalysisModelAsynchronousQueueBalanceRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelAsynchronousQueueBalanceRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IEnumerable<EntityAnalysisModelAsynchronousQueueBalance> Get(int limit)
        {
            return (IOrderedQueryable<EntityAnalysisModelAsynchronousQueueBalance>)dbContext
                .EntityAnalysisModelAsynchronousQueueBalance
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .OrderByDescending(o => o.Id)
                .Take(limit);
        }

        public IEnumerable<EntityAnalysisModelAsynchronousQueueBalance> GetByEntityModelId(Guid entityAnalysisModelGuid,
            int limit)
        {
            return (IOrderedQueryable<EntityAnalysisModelAsynchronousQueueBalance>)dbContext
                .EntityAnalysisModelAsynchronousQueueBalance
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.EntityAnalysisModelGuid == entityAnalysisModelGuid)
                .OrderByDescending(o => o.Id)
                .Take(limit);
        }

        public EntityAnalysisModelAsynchronousQueueBalance Insert(EntityAnalysisModelAsynchronousQueueBalance model)
        {
            model.Id = dbContext.InsertWithInt32Identity(model);
            return model;
        }
    }
}
