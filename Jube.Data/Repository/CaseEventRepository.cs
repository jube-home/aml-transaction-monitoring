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
    using LinqToDB.Data;
    using Poco;

    public class CaseEventRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public CaseEventRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseEventRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public CaseEventRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IEnumerable<CaseEvent> Get()
        {
            return dbContext.CaseEvent.Where(w =>
                w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                !tenantRegistryId.HasValue);
        }

        public CaseEvent GetById(int id)
        {
            return dbContext.CaseEvent.FirstOrDefault(w
                => (w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                    !tenantRegistryId.HasValue)
                   && w.Id == id);
        }

        public void UpdateAbstractionRuleMatches(int id)
        {
            dbContext.EntityAnalysisModelSearchKeyDistinctValueCalculationInstance
                .Where(d => d.EntityAnalysisModelSearchKeyCalculationInstanceId == id)
                .Set(s => s.AbstractionRulesMatchesUpdatedDate, DateTime.Now)
                .Update();
        }

        public CaseEvent Insert(CaseEvent model)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.Now;
            model.Id = dbContext.InsertWithInt32Identity(model);
            return model;
        }

        public void BulkInsert(IEnumerable<CaseEvent> models)
        {
            dbContext.BulkCopy(models);
        }
    }
}
