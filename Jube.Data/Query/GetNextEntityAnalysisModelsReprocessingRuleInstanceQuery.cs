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
    using System.Data;
    using System.Linq;
    using Context;
    using LinqToDB;

    public class GetNextEntityAnalysisModelsReprocessingRuleInstanceQuery(DbContext dbContext)
    {

        public Dto Execute(int entityAnalysisModelId)
        {
            dbContext.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                var query = dbContext.EntityAnalysisModelReprocessingRuleInstance
                    .Where(w => w.StatusId == 0
                                && (w.Deleted == 0 || w.Deleted == null)
                                && w.EntityAnalysisModelReprocessingRule.Active == 1
                                && w.EntityAnalysisModelReprocessingRule.EntityAnalysisModelId ==
                                entityAnalysisModelId)
                    .OrderBy(o => o.Id)
                    .Select(s =>
                        new Dto
                        {
                            EntityAnalysisModelsReprocessingRuleId = s.EntityAnalysisModelReprocessingRule
                                .Id,
                            ReprocessingIntervalValue =
                                s.EntityAnalysisModelReprocessingRule.ReprocessingValue,
                            ReprocessingSample = s.EntityAnalysisModelReprocessingRule.ReprocessingSample,
                            RuleScriptTypeId = s.EntityAnalysisModelReprocessingRule.RuleScriptTypeId,
                            CoderRuleScript = s.EntityAnalysisModelReprocessingRule.CoderRuleScript,
                            BuilderRuleScript = s.EntityAnalysisModelReprocessingRule.BuilderRuleScript,
                            EntityAnalysisModelId = s.EntityAnalysisModelReprocessingRule.EntityAnalysisModelId.Value,
                            Id =
                                s.Id,
                            ReprocessingIntervalType = s.EntityAnalysisModelReprocessingRule.ReprocessingInterval
                        }
                    )
                    .FirstOrDefault();

                if (query != null)
                {
                    dbContext.EntityAnalysisModelReprocessingRuleInstance
                        .Where(d =>
                            d.Id ==
                            query.Id)
                        .Set(s => s.StatusId, Convert.ToByte(1))
                        .Set(s => s.StartedDate, DateTime.Now)
                        .Update();
                }

                dbContext.CommitTransaction();

                return query;
            }
            catch
            {
                dbContext.RollbackTransaction();
                throw;
            }
        }

        public class Dto
        {
            public int EntityAnalysisModelsReprocessingRuleId { get; set; }
            public int? ReprocessingIntervalValue { get; set; }
            public string ReprocessingIntervalType { get; set; }
            public double? ReprocessingSample { get; set; }
            public int? RuleScriptTypeId { get; set; }
            public string CoderRuleScript { get; set; }
            public string BuilderRuleScript { get; set; }
            public int EntityAnalysisModelId { get; set; }
            public int Id { get; set; }
        }
    }
}
