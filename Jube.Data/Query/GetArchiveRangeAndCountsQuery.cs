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
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;

namespace Jube.Data.Query
{
    public class GetArchiveRangeAndCountsQuery(DbContext dbContext)
    {
        public async Task<Dto> Execute(Guid entityAnalysisModelGuid)
        {
            return await (from archive in dbContext.Archive
                join model in dbContext.EntityAnalysisModel on archive.EntityAnalysisModelId equals model.Id
                where model.Guid == entityAnalysisModelGuid
                group archive by new { archive.EntityAnalysisModelId }
                into g
                select new Dto
                {
                    Count = g.Count(),
                    Min = g.Min(q => q.ReferenceDate),
                    Max = g.Max(q => q.ReferenceDate)
                }).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public class Dto
        {
            public long Count { get; set; }
            public DateTime? Min { get; set; }
            public DateTime? Max { get; set; }
        }
    }
}