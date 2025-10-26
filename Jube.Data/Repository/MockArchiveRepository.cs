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
    using System.Collections.Generic;
    using System.Linq;
    using Context;
    using LinqToDB;
    using Poco;

    public class MockArchiveRepository(DbContext dbContext)
    {

        public void Insert(MockArchive model)
        {
            dbContext.Insert(model);
        }

        public IEnumerable<string> GetJsonByEntityAnalysisModelIdRandomLimit(int entityAnalysisModelId, int limit)
        {
            return dbContext.MockArchive
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId)
                .OrderBy(o => o.EntityAnalysisModelInstanceEntryGuid).Select(s => s.Json).Take(limit);
        }

        public void Delete()
        {
            dbContext.MockArchive.Delete();
        }
    }
}
