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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Poco;

    public class SessionCaseJournalRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public SessionCaseJournalRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public Task<SessionCaseJournal> GetByCaseWorkflowGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.SessionCaseJournal.FirstOrDefaultAsync(w
                => w.CreatedUser == userName &&
                   w.CaseWorkflow.Guid == guid, token);
        }

        public async Task<SessionCaseJournal> UpsertAsync(SessionCaseJournal model, CancellationToken token = default)
        {
            var existing = await dbContext.SessionCaseJournal
                .FirstOrDefaultAsync(w => w.CaseWorkflowGuid == model.CaseWorkflowGuid
                                          && w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                                          && w.CreatedUser == userName, token);

            if (existing == null)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.CreatedUser = userName;
                model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
                return model;
            }

            existing.CreatedDate = DateTime.UtcNow;
            existing.Json = model.Json;
            await dbContext.UpdateAsync(existing, token: token);
            return model;
        }
    }
}
