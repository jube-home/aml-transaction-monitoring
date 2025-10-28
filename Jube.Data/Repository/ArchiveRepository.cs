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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Context;
    using LinqToDB;
    using LinqToDB.Data;
    using Poco;

    public class ArchiveRepository(DbContext dbContext)
    {

        public Archive GetByEntityAnalysisModelInstanceEntryGuidAndEntityAnalysisModelId(Guid guid,
            int entityAnalysisModelId)
        {
            return dbContext.Archive.FirstOrDefault(w =>
                w.EntityAnalysisModelInstanceEntryGuid == guid
                && w.EntityAnalysisModelId == entityAnalysisModelId);
        }

        public void Update(Archive model)
        {
            dbContext.Update(model);
        }

        public long GetCountsByReferenceDate(Guid entityAnalysisModelGuid,DateTime referenceDate)
        {
            return dbContext.Archive.Count(w => w.EntityAnalysisModel.Guid == entityAnalysisModelGuid && w.ReferenceDate >= referenceDate);
        }
        
        public void Insert(Archive model)
        {
            dbContext.Insert(model);
        }

        public IEnumerable<string> GetJsonByEntityAnalysisModelIdRandomLimit(int entityAnalysisModelId, int limit)
        {
            return dbContext.Archive
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId)
                .OrderBy(o => o.EntityAnalysisModelInstanceEntryGuid).Select(s => s.Json).Take(limit);
        }

        public void BulkCopy(List<Archive> models)
        {
            dbContext.BulkCopy(models);
        }
    }
}
