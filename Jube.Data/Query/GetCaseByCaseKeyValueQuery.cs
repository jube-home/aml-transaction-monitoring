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

    public class GetCaseByCaseKeyValueQuery(DbContext dbContext, string userName)
    {
        public async Task<IEnumerable<Dto>> ExecuteAsync(string key, string value, CancellationToken token = default)
        {
            var query = from c in dbContext.Case
                from i in dbContext.CaseWorkflow.InnerJoin(w => w.Guid == c.CaseWorkflowGuid)
                from m in dbContext.EntityAnalysisModel.InnerJoin(w =>
                    w.Id == i.EntityAnalysisModelId && (w.Deleted == 0 || w.Deleted == null))
                from t in dbContext.TenantRegistry.InnerJoin(w => w.Id == m.TenantRegistryId)
                from s in dbContext.CaseWorkflowStatus.InnerJoin(w =>
                    w.Guid == c.CaseWorkflowStatusGuid && (w.Deleted == 0 || w.Deleted == null))
                where c.CaseKey == key
                      && c.CaseKeyValue == value
                      && dbContext.UserInTenant
                          .Any(u => u.TenantRegistryId == t.Id && u.User == userName)
                      && dbContext.CaseWorkflowRole
                          .Where(r => r.CaseWorkflowGuid == i.Guid
                                      && (r.Deleted == 0 || r.Deleted == null))
                          .Any(r => dbContext.RoleRegistry
                              .Where(rr => rr.Guid == r.RoleRegistryGuid)
                              .Any(rr => dbContext.UserRegistry
                                  .Any(u => u.RoleRegistryId == rr.Id && u.Name == userName)))
                      && dbContext.CaseWorkflowStatusRole
                          .Where(r => r.CaseWorkflowStatusGuid == s.Guid
                                      && (r.Deleted == 0 || r.Deleted == null))
                          .Any(r => dbContext.RoleRegistry
                              .Where(rr => rr.Guid == r.RoleRegistryGuid)
                              .Any(rr => dbContext.UserRegistry
                                  .Any(u => u.RoleRegistryId == rr.Id && u.Name == userName)))
                orderby c.Id descending
                select new Dto
                {
                    Id = c.Id,
                    EntityAnalysisModelInstanceEntryGuid = c.EntityAnalysisModelInstanceEntryGuid,
                    DiaryDate = c.DiaryDate.GetValueOrDefault(),
                    CaseWorkflowGuid = c.CaseWorkflowGuid,
                    CaseWorkflowStatusName = s.Name,
                    CreatedDate = c.CreatedDate.GetValueOrDefault(),
                    Locked = c.Locked.GetValueOrDefault() == 1,
                    LockedUser = c.LockedUser ?? "",
                    LockedDate = c.LockedDate.GetValueOrDefault(),
                    ClosedStatusId = c.ClosedStatusId.GetValueOrDefault(),
                    ClosedDate = c.ClosedDate.GetValueOrDefault(),
                    ClosedUser = c.ClosedUser ?? "",
                    CaseKey = c.CaseKey,
                    Diary = c.Diary.GetValueOrDefault() == 1,
                    DiaryUser = c.DiaryUser ?? "",
                    Rating = c.Rating.GetValueOrDefault(),
                    CaseKeyValue = c.CaseKeyValue,
                    LastClosedStatus = c.LastClosedStatus.GetValueOrDefault(),
                    ClosedStatusMigrationDate = c.ClosedStatusMigrationDate.GetValueOrDefault(),
                    ForeColor = s.ForeColor,
                    BackColor = s.BackColor
                };

            return await query.ToListAsync(token);
        }

        public class Dto
        {
            public int Id { get; set; }
            public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
            public DateTime DiaryDate { get; set; }
            public Guid CaseWorkflowGuid { get; set; }
            public string CaseWorkflowStatusName { get; set; }
            public DateTime CreatedDate { get; set; }
            public bool Locked { get; set; }
            public string LockedUser { get; set; }
            public DateTime LockedDate { get; set; }
            public byte ClosedStatusId { get; set; }
            public DateTime ClosedDate { get; set; }
            public string ClosedUser { get; set; }
            public string CaseKey { get; set; }
            public bool Diary { get; set; }
            public string DiaryUser { get; set; }
            public byte Rating { get; set; }
            public string CaseKeyValue { get; set; }
            public byte LastClosedStatus { get; set; }
            public DateTime ClosedStatusMigrationDate { get; set; }
            public string ForeColor { get; set; }
            public string BackColor { get; set; }
        }
    }
}
