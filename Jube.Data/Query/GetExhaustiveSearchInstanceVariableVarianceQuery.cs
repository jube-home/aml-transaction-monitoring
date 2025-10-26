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
    using System.Collections.Generic;
    using System.Linq;
    using Context;

    public class GetExhaustiveSearchInstanceVariableVarianceQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetExhaustiveSearchInstanceVariableVarianceQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public IEnumerable<Dto> Execute(
            int exhaustiveSearchInstanceVariableId)
        {
            return dbContext.ExhaustiveSearchInstanceVariableMultiCollinearity
                .Where(w => w.ExhaustiveSearchInstanceVariable
                                .ExhaustiveSearchInstance.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.ExhaustiveSearchInstanceVariableId == exhaustiveSearchInstanceVariableId)
                .OrderBy(o => o.CorrelationAbsRank)
                .Select(s => new Dto
                {
                    Name = s.TestExhaustiveSearchInstanceVariable.Name,
                    Correlation = s.Correlation.Value,
                    CorrelationAbsRank = s.CorrelationAbsRank.Value
                });
        }

        public class Dto
        {
            public double Correlation { get; set; }
            public int CorrelationAbsRank { get; set; }
            public string Name { get; set; }
        }
    }
}
