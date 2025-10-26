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

    public class GetCaseWorkflowXPathByCaseWorkflowIdQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetCaseWorkflowXPathByCaseWorkflowIdQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public IEnumerable<Dto> Execute(Guid caseWorkflowGuid)
        {
            return dbContext.CaseWorkflowXPath.Where(w
                    => w.CaseWorkflow.Guid == caseWorkflowGuid &&
                       w.Active == 1 && (w.Deleted == 0 || w.Deleted == null) &&
                       w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                )
                .Select(s => new Dto
                {
                    Name = s.Name,
                    XPath = FormatXPath(s.XPath),
                    ConditionalRegularExpressionFormatting = s.ConditionalRegularExpressionFormatting == 1,
                    ConditionalFormatForeColor = s.ConditionalFormatForeColor,
                    ConditionalFormatBackColor = s.ConditionalFormatBackColor,
                    RegularExpression = s.RegularExpression,
                    ForeRowColorScope = s.ForeRowColorScope == 1,
                    BackRowColorScope = s.BackRowColorScope == 1,
                    BoldLineFormatForeColor = s.BoldLineFormatForeColor,
                    BoldLineFormatBackColor = s.BoldLineFormatBackColor,
                    BoldLineMatched = s.BoldLineMatched == 1
                });
        }

        private string FormatXPath(string xPath)
        {
            var splits = xPath.Split(".");
            return splits[0].ToLower() switch
            {
                "data" => splits[1],
                _ => xPath
            };
        }

        public class Dto
        {
            public string Name { get; set; }
            public string XPath { get; set; }
            public bool ConditionalRegularExpressionFormatting { get; set; }
            public string ConditionalFormatForeColor { get; set; }
            public string ConditionalFormatBackColor { get; set; }
            public string RegularExpression { get; set; }
            public bool ForeRowColorScope { get; set; }
            public bool BackRowColorScope { get; set; }
            public string BoldLineFormatForeColor { get; set; }
            public string BoldLineFormatBackColor { get; set; }
            public bool BoldLineMatched { get; set; }
        }
    }
}
