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

    public class GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;

        public GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        private IEnumerable<Dto> Execute
            (int promotedExhaustiveSearchInstanceTrialInstanceId)
        {
            var query = from v in
                    dbContext.ExhaustiveSearchInstanceVariable
                join t in dbContext.ExhaustiveSearchInstanceTrialInstanceVariable
                    on v.Id equals t.ExhaustiveSearchInstanceVariableId
                from p in dbContext.ExhaustiveSearchInstanceTrialInstanceVariablePrescription
                    .Where(w1 => w1.ExhaustiveSearchInstanceTrialInstanceVariableId == t.Id).DefaultIfEmpty()
                from s in dbContext.ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
                    .Where(w2 => w2.ExhaustiveSearchInstanceTrialInstanceVariableId == t.Id).DefaultIfEmpty()
                where (t.Removed == 0 || t.Removed == null)
                      && t.ExhaustiveSearchInstanceTrialInstanceId == promotedExhaustiveSearchInstanceTrialInstanceId
                orderby t.VariableSequence
                select new Dto
                {
                    Id = v.Id,
                    Name = v.Name,
                    Mean = v.Mean ?? v.Mean.Value,
                    Maximum = v.Maximum ?? v.Maximum.Value,
                    Minimum = v.Minimum ?? v.Minimum.Value,
                    StandardDeviation = v.StandardDeviation ?? v.StandardDeviation.Value,
                    NormalisationTypeId = v.NormalisationTypeId.GetValueOrDefault(),
                    EmptyRange = v.Maximum + v.Minimum == 0,
                    VariableSequence = v.VariableSequence.GetValueOrDefault(),
                    ProcessingTypeId = v.ProcessingTypeId.GetValueOrDefault()
                };
            return query;
        }

        public IEnumerable<Dto> ExecuteByExhaustiveSearchInstanceTrialInstanceId(
            int exhaustiveSearchInstanceTrialInstanceId)
        {
            return Execute(exhaustiveSearchInstanceTrialInstanceId);
        }

        public IEnumerable<Dto> ExecuteByExhaustiveSearchInstanceId(
            int exhaustiveSearchInstanceId)
        {
            var promotedExhaustiveSearchInstanceTrialInstanceId = dbContext
                .ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Id == exhaustiveSearchInstanceId
                    && w.Active == 1
                    && (w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                        .EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue))
                .OrderByDescending(o => o.Id)
                .Select(s => s.ExhaustiveSearchInstanceTrialInstanceId.GetValueOrDefault())
                .FirstOrDefault();

            return Execute(promotedExhaustiveSearchInstanceTrialInstanceId);
        }

        public class Dto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double Mean { get; set; }
            public double Maximum { get; set; }
            public double Minimum { get; set; }
            public double StandardDeviation { get; set; }
            public byte NormalisationTypeId { get; set; }
            public bool EmptyRange { get; set; }
            public int VariableSequence { get; set; }
            public int ProcessingTypeId { get; set; }
        }
    }
}
