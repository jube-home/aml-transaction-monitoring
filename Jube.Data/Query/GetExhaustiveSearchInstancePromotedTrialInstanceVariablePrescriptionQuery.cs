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

    public class GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery(DbContext dbContext)
    {

        public IEnumerable<Dto> Execute(
            int exhaustiveSearchInstancePromotedTrialInstanceId)
        {
            var variables =
                from t in dbContext.ExhaustiveSearchInstanceTrialInstanceVariable
                from p in dbContext
                    .ExhaustiveSearchInstanceTrialInstanceVariablePrescription
                    .Where(w =>
                        w.ExhaustiveSearchInstanceTrialInstanceVariableId == t.Id).DefaultIfEmpty()
                from s in dbContext
                    .ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
                    .Where(w =>
                        w.ExhaustiveSearchInstanceTrialInstanceVariableId == t.Id).DefaultIfEmpty()
                from g in dbContext
                    .ExhaustiveSearchInstancePromotedTrialInstance
                    .Where(w => w.ExhaustiveSearchInstanceTrialInstanceId == t.ExhaustiveSearchInstanceTrialInstanceId)
                from v in dbContext
                    .ExhaustiveSearchInstanceVariable
                    .Where(w => w.Id == t.ExhaustiveSearchInstanceVariableId)
                where g.Id == exhaustiveSearchInstancePromotedTrialInstanceId
                      && (t.Removed == 0 || t.Removed == null)
                orderby s.Sensitivity descending
                select new Dto
                {
                    Id = v.Id,
                    Name = v.Name,
                    VariableMean = v.Mean.Value,
                    VariableStandardDeviation = v.StandardDeviation.Value,
                    VariableMaximum = v.Maximum.Value,
                    VariableMinimum = v.Minimum.Value,
                    PrescriptionMean = p.Mean ?? 0,
                    PrescriptionStandardDeviation = p.StandardDeviation ?? 0,
                    PrescriptionMaximum = p.Maximum ?? 0,
                    PrescriptionMinimum = p.Minimum ?? 0,
                    Sensitivity = s.Sensitivity ?? 0
                };

            return variables;
        }

        public class Dto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double VariableMean { get; set; }
            public double VariableStandardDeviation { get; set; }
            public double VariableMaximum { get; set; }
            public double VariableMinimum { get; set; }
            public double PrescriptionMean { get; set; }
            public double PrescriptionStandardDeviation { get; set; }
            public double PrescriptionMaximum { get; set; }
            public double PrescriptionMinimum { get; set; }
            public double Sensitivity { get; set; }
        }
    }
}
