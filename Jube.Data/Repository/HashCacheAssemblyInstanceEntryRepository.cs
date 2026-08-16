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

    public class HashCacheAssemblyInstanceEntryRepository(DbContext dbContext)
    {
        public Task UpsertAsync(HashCacheAssemblyInstanceEntry model, CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            return dbContext.HashCacheAssemblyInstanceEntry.InsertOrUpdateAsync(
                () => new HashCacheAssemblyInstanceEntry
                {
                    HashCacheAssemblyInstanceId = model.HashCacheAssemblyInstanceId,
                    ScriptHash = model.ScriptHash,
                    Bytes = model.Bytes,
                    Code = model.Code,
                    Binary = model.Binary,
                    CreatedDate = now,
                    LastSeenDate = now
                },
                _ => new HashCacheAssemblyInstanceEntry
                {
                    LastSeenDate = now
                },
                () => new HashCacheAssemblyInstanceEntry
                {
                    ScriptHash = model.ScriptHash
                },
                token);
        }

        public async Task<HashSet<string>> GetAllScriptHashesAsync(CancellationToken token = default)
        {
            var scriptHashes = await dbContext.HashCacheAssemblyInstanceEntry
                .Select(e => e.ScriptHash)
                .ToListAsync(token);

            return new HashSet<string>(scriptHashes);
        }
    }
}
