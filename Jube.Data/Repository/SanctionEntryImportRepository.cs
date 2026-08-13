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
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Poco;

    public class SanctionEntryImportRepository(DbContext dbContext)
    {
        public async Task<IEnumerable<SanctionEntryImport>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.SanctionEntryImport.ToListAsync(token).ConfigureAwait(false);
        }

        public Task<SanctionEntryImport> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.SanctionEntryImport.Where(w => w.Id == id).FirstOrDefaultAsync(token);
        }

        public async Task<IEnumerable<SanctionEntryImport>> GetBySanctionEntrySourceIdAsync(
            int sanctionEntrySourceId, CancellationToken token = default)
        {
            return await dbContext.SanctionEntryImport
                .Where(w => w.SanctionEntrySourceId == sanctionEntrySourceId)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<SanctionEntryImport> InsertAsync(SanctionEntryImport model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        public Task UpdateAsync(SanctionEntryImport model, CancellationToken token = default)
        {
            return dbContext.UpdateAsync(model, token: token);
        }
    }
}
