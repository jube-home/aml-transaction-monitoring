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

    public class GetCaseNoteByCaseKeyValueQuery(DbContext dbContext, string user)
    {


        public IEnumerable<Dto> Execute(string key, string value)
        {
            var query = from c in dbContext.Case
                from n in dbContext.CaseNote.InnerJoin(w => w.CaseId == c.Id)
                from a in dbContext.CaseWorkflowAction.InnerJoin(w => w.Id == n.ActionId)
                from i in dbContext.CaseWorkflow.InnerJoin(w => w.Guid == c.CaseWorkflowGuid)
                from m in dbContext.EntityAnalysisModel.InnerJoin(w =>
                    w.Id == i.EntityAnalysisModelId && (w.Deleted == 0 || w.Deleted == null))
                from t in dbContext.TenantRegistry.InnerJoin(w => w.Id == m.TenantRegistryId)
                from u in dbContext.UserInTenant.InnerJoin(w => w.TenantRegistryId == t.Id)
                orderby n.Id descending
                where c.CaseKey == key && c.CaseKeyValue == value && u.User == user
                select new Dto
                {
                    Id = n.Id,
                    CaseId = n.CaseId.GetValueOrDefault(),
                    CreatedDate = n.CreatedDate.GetValueOrDefault(),
                    CreatedUser = n.CreatedUser,
                    Note = n.Note,
                    ActionId = n.ActionId.GetValueOrDefault(),
                    Action = a.Name,
                    PriorityId = n.PriorityId.GetValueOrDefault(),
                    Priority = ConvertPriorityIdToString(n.PriorityId.GetValueOrDefault())
                };

            return query;
        }

        private string ConvertPriorityIdToString(int id)
        {
            return id switch
            {
                1 => "High",
                2 => "Medium",
                3 => "Low",
                _ => "Medium"
            };
        }

        public class Dto
        {
            public int Id { get; set; }
            public int CaseId { get; set; }
            public DateTime CreatedDate { get; set; }
            public string CreatedUser { get; set; }
            public string Note { get; set; }
            public int ActionId { get; set; }
            public string Action { get; set; }
            public int PriorityId { get; set; }
            public string Priority { get; set; }
        }
    }
}
