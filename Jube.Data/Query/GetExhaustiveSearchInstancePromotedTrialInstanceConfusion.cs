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
    using System.Linq;
    using Context;

    public class GetExhaustiveSearchInstancePromotedTrialInstanceConfusionQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetExhaustiveSearchInstancePromotedTrialInstanceConfusionQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public Dto Execute(
            int exhaustiveSearchInstanceId)
        {
            var confusion = dbContext
                .ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Id == exhaustiveSearchInstanceId
                    && w.Active == 1
                    && w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                        .EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .Select(s =>
                    new Dto
                    {
                        Score = s.Score.Value,
                        FalseNegative = s.FalseNegative.Value,
                        FalsePositive = s.FalsePositive.Value,
                        TrueNegative = s.TrueNegative.Value,
                        TruePositive = s.TruePositive.Value
                    })
                .FirstOrDefault();

            var tableTotal = 0;
            var positiveRowTotal = 0;
            var positiveColumnTotal = 0;
            var negativeRowTotal = 0;
            var negativeColumnTotal = 0;
            var truePositiveRowTotal = 0d;
            var truePositiveColumnTotal = 0d;
            var truePositiveTableTotal = 0d;
            var falsePositiveRowTotal = 0d;
            var falsePositiveColumnTotal = 0d;
            var falsePositiveTableTotal = 0d;
            var falseNegativeRowTotal = 0d;
            var falseNegativeColumnTotal = 0d;
            var falseNegativeTableTotal = 0d;
            var trueNegativeRowTotal = 0d;
            var trueNegativeColumnTotal = 0d;
            var trueNegativeTableTotal = 0d;
            var negativeColumnTableTotal = 0d;
            var positiveColumnTableTotal = 0d;
            var positiveRowTableTotal = 0d;
            var negativeRowTableTotal = 0d;

            if (confusion != null)
            {
                tableTotal = confusion.TruePositive + confusion.TrueNegative + confusion.FalseNegative +
                             confusion.TrueNegative;
                positiveRowTotal = confusion.TruePositive + confusion.FalseNegative;
                positiveColumnTotal = confusion.TruePositive + confusion.FalsePositive;
                negativeRowTotal = confusion.FalsePositive + confusion.TrueNegative;
                negativeColumnTotal = confusion.FalseNegative + confusion.TrueNegative;
                truePositiveRowTotal = Math.Round((double)confusion.TruePositive / positiveRowTotal, 2);
                truePositiveColumnTotal = Math.Round((double)confusion.TruePositive / positiveColumnTotal, 2);
                truePositiveTableTotal = Math.Round((double)confusion.TruePositive / tableTotal, 2);
                falsePositiveRowTotal = Math.Round((double)confusion.FalsePositive / negativeRowTotal, 2);
                falsePositiveColumnTotal = Math.Round((double)confusion.FalsePositive / positiveColumnTotal, 2);
                falsePositiveTableTotal = Math.Round((double)confusion.FalsePositive / tableTotal, 2);
                falseNegativeRowTotal = Math.Round((double)confusion.FalsePositive / positiveRowTotal, 2);
                falseNegativeColumnTotal = Math.Round((double)confusion.FalsePositive / negativeColumnTotal, 2);
                falseNegativeTableTotal = Math.Round((double)confusion.FalsePositive / tableTotal, 2);
                trueNegativeRowTotal = Math.Round((double)confusion.TrueNegative / negativeRowTotal, 2);
                trueNegativeColumnTotal = Math.Round((double)confusion.TrueNegative / negativeColumnTotal, 2);
                trueNegativeTableTotal = Math.Round((double)confusion.TrueNegative / tableTotal, 2);
                negativeColumnTableTotal = Math.Round((double)negativeColumnTotal / tableTotal, 2);
                positiveColumnTableTotal = Math.Round((double)positiveColumnTotal / tableTotal, 2);
                positiveRowTableTotal = Math.Round((double)positiveRowTotal / tableTotal, 2);
                negativeRowTableTotal = Math.Round((double)negativeRowTotal / tableTotal, 2);
            }
            else
            {
                confusion = new Dto();
            }

            confusion.TableTotal = tableTotal;
            confusion.PositiveRowTotal = positiveRowTotal;
            confusion.PositiveColumnTotal = positiveColumnTotal;
            confusion.NegativeRowTotal = negativeRowTotal;
            confusion.NegativeColumnTotal = negativeColumnTotal;
            confusion.PositiveRowTableTotal = positiveRowTableTotal;
            confusion.PositiveColumnTableTotal = positiveColumnTableTotal;
            confusion.NegativeRowTableTotal = negativeRowTableTotal;
            confusion.NegativeColumnTableTotal = negativeColumnTableTotal;
            confusion.TruePositiveRowTotal = truePositiveRowTotal;
            confusion.TruePositiveColumnTotal = truePositiveColumnTotal;
            confusion.TruePositiveTableTotal = truePositiveTableTotal;
            confusion.FalsePositiveRowTotal = falsePositiveRowTotal;
            confusion.FalsePositiveColumnTotal = falsePositiveColumnTotal;
            confusion.FalsePositiveTableTotal = falsePositiveTableTotal;
            confusion.FalseNegativeRowTotal = falseNegativeRowTotal;
            confusion.FalseNegativeColumnTotal = falseNegativeColumnTotal;
            confusion.FalseNegativeTableTotal = falseNegativeTableTotal;
            confusion.TrueNegativeRowTotal = trueNegativeRowTotal;
            confusion.TrueNegativeColumnTotal = trueNegativeColumnTotal;
            confusion.TrueNegativeTableTotal = trueNegativeTableTotal;

            return confusion;
        }

        public class Dto
        {
            public int Id { get; set; }
            public double Score { get; set; }
            public int FalsePositive { get; set; }
            public int TruePositive { get; set; }
            public int FalseNegative { get; set; }
            public int TrueNegative { get; set; }
            public int TableTotal { get; set; }
            public int PositiveRowTotal { get; set; }
            public int PositiveColumnTotal { get; set; }
            public int NegativeRowTotal { get; set; }
            public int NegativeColumnTotal { get; set; }
            public double PositiveRowTableTotal { get; set; }
            public double PositiveColumnTableTotal { get; set; }
            public double NegativeRowTableTotal { get; set; }
            public double NegativeColumnTableTotal { get; set; }
            public double TruePositiveRowTotal { get; set; }
            public double TruePositiveColumnTotal { get; set; }
            public double TruePositiveTableTotal { get; set; }
            public double FalsePositiveRowTotal { get; set; }
            public double FalsePositiveColumnTotal { get; set; }
            public double FalsePositiveTableTotal { get; set; }
            public double FalseNegativeRowTotal { get; set; }
            public double FalseNegativeColumnTotal { get; set; }
            public double FalseNegativeTableTotal { get; set; }
            public double TrueNegativeRowTotal { get; set; }
            public double TrueNegativeColumnTotal { get; set; }
            public double TrueNegativeTableTotal { get; set; }
        }
    }
}
