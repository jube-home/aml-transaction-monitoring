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

    public class GetEntityAnalysisModelsSearchKeyCalculationInstancesLastSearchKeyDates(DbContext dbContext)
    {

        public IEnumerable<Dto> Execute(Guid entityAnalysisModelGuid)
        {
            return from c
                    in dbContext.EntityAnalysisModelSearchKeyCalculationInstance
                where c.EntityAnalysisModelGuid == entityAnalysisModelGuid
                group c by c.SearchKey
                into g
                select new Dto
                {
                    SearchKey = g.Key,
                    DistinctFetchToDate = g.Max(s => s.DistinctFetchToDate)
                };
        }

        public class Dto
        {
            public string SearchKey { get; set; }
            public DateTime? DistinctFetchToDate { get; set; }
        }
    }
}
