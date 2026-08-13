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
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Poco;

    public enum SanctionEntryUpsertOutcome
    {
        Inserted,
        Revived,
        Unchanged
    }

    public class SanctionsEntryRepository(DbContext dbContext)
    {
        public async Task<IEnumerable<SanctionEntry>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.SanctionEntry
                .Where(w => w.Deleted == 0 || w.Deleted == null)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<SanctionEntry>> GetActiveBySanctionEntrySourceIdAsync(
            int sanctionEntrySourceId, CancellationToken token = default)
        {
            return await dbContext.SanctionEntry
                .Where(w => w.SanctionEntrySourceId == sanctionEntrySourceId
                            && (w.Deleted == 0 || w.Deleted == null))
                .ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<(SanctionEntry Entry, SanctionEntryUpsertOutcome Outcome)> UpsertAsync(SanctionEntry model,
            CancellationToken token = default)
        {
            var existing =
                await dbContext.SanctionEntry.FirstOrDefaultAsync(w =>
                    w.SanctionEntryHash == model.SanctionEntryHash
                    && w.SanctionEntrySourceId == model.SanctionEntrySourceId, token);

            if (existing != null)
            {
                if (existing.Deleted != 1)
                {
                    return (existing, SanctionEntryUpsertOutcome.Unchanged);
                }

                await dbContext.SanctionEntry
                    .Where(w => w.Id == existing.Id)
                    .Set(s => s.Deleted, (byte?)null)
                    .Set(s => s.DeletedDate, (DateTime?)null)
                    .Set(s => s.DeletedUser, (string)null)
                    .UpdateAsync(token).ConfigureAwait(false);

                existing.Deleted = null;
                existing.DeletedDate = null;
                existing.DeletedUser = null;

                return (existing, SanctionEntryUpsertOutcome.Revived);

            }

            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return (model, SanctionEntryUpsertOutcome.Inserted);
        }

        public Task DeleteAsync(int id, string userName, CancellationToken token = default)
        {
            return dbContext.SanctionEntry
                .Where(w => w.Id == id)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);
        }
    }
}
