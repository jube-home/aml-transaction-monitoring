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
    using Context;
    using LinqToDB;

    public class GetCaseEventByCaseKeyValueQuery(DbContext dbContext, string user)
    {

        public IEnumerable<Dto> Execute(string key, string value)
        {
            var query = from c in dbContext.Case
                from e in dbContext.CaseEvent.InnerJoin(w => w.CaseId == c.Id)
                from i in dbContext.CaseWorkflow.InnerJoin(w => w.Guid == c.CaseWorkflowGuid)
                from m in dbContext.EntityAnalysisModel.InnerJoin(w =>
                    w.Id == i.EntityAnalysisModelId && (w.Deleted == 0 || w.Deleted == null))
                from t in dbContext.TenantRegistry.InnerJoin(w => w.Id == m.TenantRegistryId)
                from u in dbContext.UserInTenant.InnerJoin(w => w.TenantRegistryId == t.Id)
                from s in dbContext.CaseWorkflowStatus.LeftJoin(w =>
                    w.Guid == c.CaseWorkflowStatusGuid && w.CaseWorkflowId == i.Id &&
                    (w.Deleted == 0 || w.Deleted == null))
                orderby e.Id descending
                where c.CaseKey == key && c.CaseKeyValue == value && u.User == user
                select e;

            var getCaseEventByCaseKeyValueQueryDtos = new List<Dto>();
            foreach (var caseEvent in query)
            {
                var getCaseEventByCaseKeyValueQueryDto = new Dto
                {
                    Id = caseEvent.Id,
                    CaseId = caseEvent.CaseId.GetValueOrDefault(),
                    CreatedDate = caseEvent.CreatedDate.GetValueOrDefault(),
                    CreatedUser = caseEvent.CreatedUser,
                    Before = caseEvent.Before,
                    After = caseEvent.After,
                    CaseEventType = caseEvent.CaseEventTypeId switch
                    {
                        1 => "Automatic Unlock",
                        2 => "Skim Case",
                        3 => "Automatic Lock",
                        4 => "Full Case Fetch",
                        5 => "Closed",
                        6 => "Manual Lock",
                        7 => "Diary",
                        8 => "Workflow Change",
                        9 => "Status Change",
                        10 => "Diary Date Change",
                        11 => "Rating Change",
                        12 => "Suspend Closed",
                        13 => "Suspend Open",
                        14 => "Allocate Lock",
                        _ => "Unknown"
                    }
                };

                getCaseEventByCaseKeyValueQueryDtos.Add(getCaseEventByCaseKeyValueQueryDto);
            }

            return getCaseEventByCaseKeyValueQueryDtos;
        }

        public class Dto
        {
            public int Id { get; set; }
            public int CaseId { get; set; }
            public string CaseEventType { get; set; }
            public DateTime CreatedDate { get; set; }
            public string CreatedUser { get; set; }
            public string Before { get; set; }
            public string After { get; set; }
        }
    }
}
