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

    public class SanctionsEntryRepository(DbContext dbContext)
    {

        public IEnumerable<SanctionEntry> Get()
        {
            return dbContext.SanctionEntry;
        }

        public SanctionEntry Upsert(SanctionEntry model)
        {
            var existing =
                dbContext.SanctionEntry.FirstOrDefault(w =>
                    w.SanctionEntryHash == model.SanctionEntryHash
                    && w.SanctionEntrySourceId == model.SanctionEntrySourceId);

            if (existing != null)
            {
                return existing;
            }

            model.Id = dbContext.InsertWithInt32Identity(model);
            return model;
        }
    }
}
