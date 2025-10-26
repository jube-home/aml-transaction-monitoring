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

    public class GetExhaustiveSearchInstanceVariableQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetExhaustiveSearchInstanceVariableQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public IEnumerable<Dto> Execute(
            int exhaustiveSearchInstanceId)
        {
            var histograms = dbContext.ExhaustiveSearchInstanceVariableHistogram
                .Where(w =>
                    w.ExhaustiveSearchInstanceVariable.ExhaustiveSearchInstance.EntityAnalysisModel.TenantRegistryId ==
                    tenantRegistryId
                    && w.ExhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceId == exhaustiveSearchInstanceId)
                .ToList();

            var variables = dbContext.ExhaustiveSearchInstanceVariable
                .Where(w =>
                    w.ExhaustiveSearchInstance.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                    && w.ExhaustiveSearchInstanceId == exhaustiveSearchInstanceId).ToList();

            var joined = new List<Dto>();
            foreach (var variable in variables)
            {
                var join = new Dto
                {
                    Id = variable.Id,
                    Mean = variable.Mean.GetValueOrDefault(),
                    StandardDeviation = variable.StandardDeviation.GetValueOrDefault(),
                    Name = variable.Name,
                    Kurtosis = variable.Kurtosis.GetValueOrDefault(),
                    Skewness = variable.Skewness.GetValueOrDefault(),
                    Maximum = variable.Maximum.GetValueOrDefault(),
                    Iqr = variable.Iqr.GetValueOrDefault(),
                    DistinctValues = variable.DistinctValues.GetValueOrDefault(),
                    Correlation = variable.Correlation.GetValueOrDefault(),
                    CorrelationAbsRank = variable.CorrelationAbsRank.GetValueOrDefault(),
                    NormalisationType = variable.NormalisationTypeId switch
                    {
                        0 => "No",
                        1 => "Binary",
                        2 => "Z Score",
                        _ => "Default"
                    }
                };

                foreach (var histogram in histograms
                             .Where(w =>
                                 w.ExhaustiveSearchInstanceVariableId == variable.Id))
                {
                    join.HistogramValues.Add(new Dto.HistogramValue
                    {
                        Frequency = histogram.Frequency.GetValueOrDefault(),
                        Bin = histogram.BinRangeStart.GetValueOrDefault()
                    });
                }

                joined.Add(join);
            }

            return joined;
        }

        public class Dto
        {
            public int Id { get; set; }
            public double Mean { get; set; }
            public double StandardDeviation { get; set; }
            public string Name { get; set; }
            public double Kurtosis { get; set; }
            public double Skewness { get; set; }
            public double Maximum { get; set; }
            public double Minimum { get; set; }
            public double Iqr { get; set; }
            public string NormalisationType { get; set; }
            public int DistinctValues { get; set; }
            public double Correlation { get; set; }
            public int CorrelationAbsRank { get; set; }
            // ReSharper disable once CollectionNeverQueried.Global
            public List<HistogramValue> HistogramValues { get; set; } = [];

            public class HistogramValue
            {
                public int Frequency { get; set; }
                public double Bin { get; set; }
            }
        }
    }
}
