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

    public class GetEntityAnalysisModelActivationRuleSuppressionQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetEntityAnalysisModelActivationRuleSuppressionQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public IEnumerable<Dto> Execute(Guid entityAnalysisModelGuid, string suppressionKey, string suppressionKeyValue)
        {
            var suppressions = dbContext.EntityAnalysisModelActivationRuleSuppression
                .Where(w => w.SuppressionKey == suppressionKey && w.SuppressionKeyValue == suppressionKeyValue
                                                               && w.EntityAnalysisModelGuid == entityAnalysisModelGuid
                                                               && (w.Deleted == 0 || w.Deleted == null)
                                                               && w.EntityAnalysisModel.TenantRegistryId ==
                                                               tenantRegistryId)
                .Select(s => s.EntityAnalysisModelActivationRuleName).ToList();

            var models =
                (from m in dbContext.EntityAnalysisModel
                    join x in dbContext.EntityAnalysisModelRequestXpath
                        on m.Id equals x.EntityAnalysisModelId
                    join r in dbContext.EntityAnalysisModelActivationRule
                        on m.Id equals r.EntityAnalysisModelId
                    where x.EnableSuppression == 1
                          && (x.Deleted == 0 || x.Deleted == null)
                          && (m.Deleted == 0 || m.Deleted == null)
                          && (r.Deleted == 0 || r.Deleted == null)
                          && m.TenantRegistryId == tenantRegistryId
                          && x.Name == suppressionKey
                          && m.Guid == entityAnalysisModelGuid
                    select new
                    {
                        r.Name,
                        m.Guid,
                        r.Id
                    }).Distinct().ToList();

            var responses = models
                .Select(model => new Dto
                {
                    Name = model.Name,
                    EntityAnalysisModelGuid = model.Guid,
                    EntityAnalysisModelActivationRuleSuppressionId = model.Id,
                    Suppression = suppressions.Contains(model.Name)
                }).ToList();

            return responses;
        }

        public class Dto
        {
            public string Name { get; set; }
            public bool Suppression { get; set; }
            public int EntityAnalysisModelActivationRuleSuppressionId { get; set; }
            public Guid EntityAnalysisModelGuid { get; set; }
        }
    }
}
