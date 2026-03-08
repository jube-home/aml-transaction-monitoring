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

        public Task UpdateExpiredCaseDiaryAsync(int id, byte closedStatus, byte lastClosedStatus, CancellationToken token = default)
        {
            return dbContext.Case
                .Where(d => d.Id == id)
                .Set(s => s.ClosedStatusId, closedStatus)
                .Set(s => s.LastClosedStatus, lastClosedStatus)
                .UpdateAsync(token);
        }

        public Task LockToUserAsync(int id, CancellationToken token = default)
        {
            return dbContext.Case
                .Where(d => d.Id == id)
                .Set(s => s.Locked, (byte)1)
                .Set(s => s.LockedUser, userName)
                .Set(s => s.LockedDate, DateTime.Now)
                .UpdateAsync(token);
        }

        public async Task<IEnumerable<Case>> GetAsyncActiveOnlyAsync(CancellationToken token = default)
        {
            return await dbContext.Case.Where(w =>
                w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                !tenantRegistryId.HasValue).ToListAsync(token: token);
        }

        public Task<Case> GetByIdActiveOnlyAsync(int id, CancellationToken token = default)
        {
            return dbContext.Case
                .Where(w =>
                    (w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.Id == id
                    && dbContext.CaseWorkflowStatusRole
                        .Where(r => r.CaseWorkflowStatusGuid == w.CaseWorkflowStatus.Guid
                                    && (r.Deleted == 0 || r.Deleted == null))
                        .Any(r => dbContext.RoleRegistry
                            .Where(rr => rr.Guid == r.RoleRegistryGuid)
                            .Any(rr => dbContext.UserRegistry
                                .Any(u => u.RoleRegistryId == rr.Id && u.Name == userName)))
                    && dbContext.CaseWorkflowRole
                        .Where(r => r.CaseWorkflowGuid == w.CaseWorkflow.Guid
                                    && (r.Deleted == 0 || r.Deleted == null))
                        .Any(r => dbContext.RoleRegistry
                            .Where(rr => rr.Guid == r.RoleRegistryGuid)
                            .Any(rr => dbContext.UserRegistry
                                .Any(u => u.RoleRegistryId == rr.Id && u.Name == userName)))
                )
                .FirstOrDefaultAsync(token);
        }

        public async Task<IEnumerable<Case>> GetByExpiredAsync(CancellationToken token = default)
        {
            return await dbContext.Case.Where(w
                => (w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                    !tenantRegistryId.HasValue)
                   && (w.ClosedStatusId == 0 ||
                       w.ClosedStatusId == 1 ||
                       w.ClosedStatusId == 2 ||
                       w.ClosedStatusId == 4)
                   && DateTime.Now >= w.DiaryDate
                   && w.Diary == 1).ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<Case> InsertAsync(Case model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<Case> UpdateCaseAsync(Case model, CancellationToken token = default)
        {
            await dbContext.UpdateAsync(model, token: token);
            return model;
        }
    }
}
