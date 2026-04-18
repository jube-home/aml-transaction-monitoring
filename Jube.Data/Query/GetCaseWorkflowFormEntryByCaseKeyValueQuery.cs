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

namespace Jube.Data.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;

    public class GetCaseWorkflowFormEntryByCaseKeyValueQuery(DbContext dbContext, string userName)
    {
        public async Task<IEnumerable<Dto>> ExecuteAsync(string key, string value, CancellationToken token = default)
        {
            var query = from c in dbContext.Case
                from n in dbContext.CaseWorkflowFormEntry.InnerJoin(w => w.CaseId == c.Id)
                from a in dbContext.CaseWorkflowForm.InnerJoin(w => w.Id == n.CaseWorkflowFormId)
                from i in dbContext.CaseWorkflow.InnerJoin(w => w.Guid == c.CaseWorkflowGuid && (w.Deleted == 0 || w.Deleted == null))
                from s in dbContext.CaseWorkflowStatus.InnerJoin(w => w.Guid == c.CaseWorkflowStatusGuid && (w.Deleted == 0 || w.Deleted == null))
                from m in dbContext.EntityAnalysisModel.InnerJoin(w =>
                    w.Id == i.EntityAnalysisModelId && (w.Deleted == 0 || w.Deleted == null))
                from t in dbContext.TenantRegistry.InnerJoin(w => w.Id == m.TenantRegistryId)
                where c.CaseKey == key
                      && c.CaseKeyValue == value
                      && dbContext.UserInTenant
                          .Any(u => u.TenantRegistryId == t.Id && u.User == userName)
                      && dbContext.CaseWorkflowRole
                          .Where(r => r.CaseWorkflowGuid == i.Guid
                                      && (r.Deleted == 0 || r.Deleted == null))
                          .Any(r => dbContext.RoleRegistry
                              .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                              .Any(rr => dbContext.UserRegistry
                                  .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                      && dbContext.CaseWorkflowStatusRole
                          .Where(r => r.CaseWorkflowStatusGuid == s.Guid
                                      && (r.Deleted == 0 || r.Deleted == null))
                          .Any(r => dbContext.RoleRegistry
                              .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                              .Any(rr => dbContext.UserRegistry
                                  .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                orderby c.Id descending
                select new Dto
                {
                    Id = n.Id,
                    CaseId = n.CaseId.GetValueOrDefault(),
                    CreatedDate = n.CreatedDate.GetValueOrDefault(),
                    CreatedUser = n.CreatedUser,
                    Name = a.Name
                };

            return await query.ToListAsync(token);
        }

        public class Dto
        {
            public int Id { get; set; }
            public int CaseId { get; set; }
            public DateTime CreatedDate { get; set; }
            public string CreatedUser { get; set; }
            public byte ResponseStatusId { get; set; }
            public string Name { get; set; }
        }
    }
}
