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

using System;
using System.Collections.Generic;
using System.Linq;
using Jube.Data.Context;

namespace Jube.Data.Query
{
    public class GetEntityAnalysisModelAsynchronousQueueBalancesQuery(DbContext dbContext)
    {
        public IEnumerable<Dto> Execute(int limit)
        {
            var query = dbContext.EntityAnalysisModelAsynchronousQueueBalance
                .OrderByDescending(o => o.Id)
                .Take(limit)
                .Select(s => new Dto
                {
                    Name = s.EntityAnalysisModel.Name,
                    CreatedDate = s.CreatedDate,
                    Instance = s.Instance,
                    Archive = s.Archive,
                    EntityAnalysisModelGuid = s.EntityAnalysisModelGuid,
                    ActivationWatcher = s.ActivationWatcher
                });

            return query;
        }

        public class Dto
        {
            public string Name { get; set; }
            public Guid EntityAnalysisModelGuid { get; set; }
            public DateTime? CreatedDate { get; set; }
            public int? Archive { get; set; }
            public int? ActivationWatcher { get; set; }
            public string Instance { get; set; }
        }
    }
}