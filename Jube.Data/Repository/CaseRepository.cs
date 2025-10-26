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

    public class CaseRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public CaseRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public CaseRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public void UpdateExpiredCaseDiary(int id, byte closedStatus, byte lastClosedStatus)
        {
            dbContext.Case
                .Where(d => d.Id == id)
                .Set(s => s.ClosedStatusId, closedStatus)
                .Set(s => s.LastClosedStatus, lastClosedStatus)
                .Update();
        }

        public void LockToUser(int id)
        {
            dbContext.Case
                .Where(d => d.Id == id)
                .Set(s => s.Locked, (byte)1)
                .Set(s => s.LockedUser, userName)
                .Set(s => s.LockedDate, DateTime.Now)
                .Update();
        }

        public IEnumerable<Case> Get()
        {
            return dbContext.Case.Where(w =>
                w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                !tenantRegistryId.HasValue);
        }

        public Case GetById(int id)
        {
            return dbContext.Case.FirstOrDefault(w
                => (w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                    !tenantRegistryId.HasValue)
                   && w.Id == id);
        }

        public IEnumerable<Case> GetByExpired()
        {
            return dbContext.Case.Where(w
                => (w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                    !tenantRegistryId.HasValue)
                   && (w.ClosedStatusId == 0 ||
                       w.ClosedStatusId == 1 ||
                       w.ClosedStatusId == 2 ||
                       w.ClosedStatusId == 4)
                   && DateTime.Now >= w.DiaryDate
                   && w.Diary == 1);
        }

        public Case Insert(Case model)
        {
            model.Id = dbContext.InsertWithInt32Identity(model);
            return model;
        }

        public Case Update(Case model)
        {
            dbContext.Update(model);
            return model;
        }
    }
}
