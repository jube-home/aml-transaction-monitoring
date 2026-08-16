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
    using LinqToDB.Data;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Poco;

    public class ArchiveRepository(DbContext dbContext)
    {
        public async Task<Archive> UpdateTagsByEntityAnalysisModelInstanceEntryGuidAsync(
            Guid entityAnalysisModelInstanceEntryGuid,
            string[] tags,
            CancellationToken token = default)
        {
            var archive = await dbContext.Archive.FirstOrDefaultAsync(w =>
                              w.EntityAnalysisModelInstanceEntryGuid == entityAnalysisModelInstanceEntryGuid, token)
                          ?? throw new InvalidOperationException(
                              $"No archive record found for {entityAnalysisModelInstanceEntryGuid}.");

            var jObject = JObject.Parse(archive.Json);
            jObject["tag"] = new JArray([..tags]);
            archive.Json = jObject.ToString(Formatting.None);

            await dbContext.UpdateAsync(archive, token: token);

            return archive;
        }

        public async Task UpdateAsync(Archive model, CancellationToken token = default)
        {
            var existing = await dbContext.Archive
                .Where(w => w.EntityAnalysisModelInstanceEntryGuid == model.EntityAnalysisModelInstanceEntryGuid)
                .FirstOrDefaultAsync(token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Id = existing.Id;
            model.Version = existing.Version + 1;
            model.CreatedDate = DateTime.UtcNow;

            await dbContext.UpdateAsync(existing, token: token);

            var audit = new ArchiveVersion
            {
                ArchiveId = existing.Id,
                Json = existing.Json,
                EntityAnalysisModelInstanceEntryGuid = existing.EntityAnalysisModelInstanceEntryGuid,
                EntryKeyValue = existing.EntryKeyValue,
                ResponseElevation = existing.ResponseElevation,
                EntityAnalysisModelActivationRuleId = existing.EntityAnalysisModelActivationRuleId,
                EntityAnalysisModelId = existing.EntityAnalysisModelId,
                ActivationRuleCount = existing.ActivationRuleCount,
                CreatedDate = existing.CreatedDate,
                ReferenceDate = existing.ReferenceDate,
                Version = existing.Version
            };

            await dbContext.InsertAsync(audit, token: token);
        }

        public async Task<long> GetCountsByReferenceDateAsync(Guid entityAnalysisModelGuid, DateTime referenceDate, CancellationToken token = default)
        {
            return await dbContext.Archive
                .CountAsync(w => w.EntityAnalysisModel.Guid == entityAnalysisModelGuid
                                 && w.ReferenceDate >= referenceDate, token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<string>> GetJsonByEntityAnalysisModelIdRandomLimitAsync(int entityAnalysisModelId, int limit, CancellationToken token = default)
        {
            return await dbContext.Archive
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId)
                .OrderBy(o => o.EntityAnalysisModelInstanceEntryGuid).Select(s => s.Json)
                .Take(limit).ToListAsync(token).ConfigureAwait(false);
        }

        public Task BulkCopyAsync(List<Archive> models, CancellationToken token = default)
        {
            return dbContext.BulkCopyAsync(models, token);
        }
    }
}
