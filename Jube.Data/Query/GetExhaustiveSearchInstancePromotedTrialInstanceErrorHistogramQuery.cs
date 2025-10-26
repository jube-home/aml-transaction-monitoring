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
    using Accord.Statistics.Visualizations;
    using Context;

    public class GetExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public IEnumerable<Dto> Execute(
            int exhaustiveSearchInstanceId)
        {
            var promotedExhaustiveSearchInstanceTrialInstanceId = dbContext
                .ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Id == exhaustiveSearchInstanceId
                    && w.Active == 1
                    && w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                        .EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .OrderByDescending(o => o.Id)
                .Select(s => s.ExhaustiveSearchInstanceTrialInstanceId)
                .FirstOrDefault();

            var errors = dbContext.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
                .Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstanceId == promotedExhaustiveSearchInstanceTrialInstanceId)
                .OrderBy(o => o.Id)
                .Select(s => s.Actual.Value - s.Predicted.Value).ToArray();

            var histogram = new Histogram();
            histogram.Compute(errors, 10);

            return histogram.Bins
                .Select(s => new Dto
                {
                    Bin = Math.Round(s.Range.Min, 2),
                    Frequency = s.Value
                }).ToList();
        }

        public class Dto
        {
            public double Bin { get; set; }
            public int Frequency { get; set; }
        }
    }
}
