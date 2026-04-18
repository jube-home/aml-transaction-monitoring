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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Poco;

    public class CaseWorkflowFormEntryValueRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public CaseWorkflowFormEntryValueRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public async Task<IEnumerable<CaseWorkflowFormEntryValue>> GetByCaseWorkflowFormEntryIdActiveOnlyAsync(int caseWorkflowFormEntryId, CancellationToken token = default)
        {
            return await dbContext.CaseWorkflowFormEntryValue.Where(w
                    => w.CaseWorkflowsFormsEntry.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId ==
                       tenantRegistryId
                       && (w.CaseWorkflowsFormsEntry.Case.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                           w.CaseWorkflowsFormsEntry.Case.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                       && (w.CaseWorkflowsFormsEntry.Case.CaseWorkflow.Deleted == 0 ||
                           w.CaseWorkflowsFormsEntry.Case.CaseWorkflow.Deleted == null)
                       && (w.CaseWorkflowsFormsEntry.Case.CaseWorkflowStatus.Deleted == 0 ||
                           w.CaseWorkflowsFormsEntry.Case.CaseWorkflowStatus.Deleted == null)
                       && w.CaseWorkflowFormEntryId == caseWorkflowFormEntryId
                       && dbContext.CaseWorkflowRole
                           .Where(r => r.CaseWorkflowGuid == w.CaseWorkflowsFormsEntry.Case.CaseWorkflow.Guid
                                       && (r.Deleted == 0 || r.Deleted == null))
                           .Any(r => dbContext.RoleRegistry
                               .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                               .Any(rr => dbContext.UserRegistry
                                   .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                       && dbContext.CaseWorkflowStatusRole
                           .Where(r => r.CaseWorkflowStatusGuid == w.CaseWorkflowsFormsEntry.Case.CaseWorkflowStatus.Guid
                                       && (r.Deleted == 0 || r.Deleted == null))
                           .Any(r => dbContext.RoleRegistry
                               .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                               .Any(rr => dbContext.UserRegistry
                                   .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                )
                .OrderByDescending(o => o.Id).ToListAsync(token);
        }

        public async Task<CaseWorkflowFormEntryValue> InsertAsync(CaseWorkflowFormEntryValue model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }
    }
}
