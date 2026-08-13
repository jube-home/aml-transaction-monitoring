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
    using Poco;

    public class ArchiveKeyRepository(DbContext dbContext)
    {
        public Task DeleteWhereNotInListAsync(List<ArchiveKey> archiveKeys, int? entityAnalysisModelsReprocessingRuleInstanceId)
        {
            return dbContext.ArchiveKey
                .Where(d => !archiveKeys.Any(x =>
                    x.ProcessingTypeId == d.ProcessingTypeId &&
                    x.Key == d.Key &&
                    x.EntityAnalysisModelInstanceEntryGuid == d.EntityAnalysisModelInstanceEntryGuid))
                .Set(x => x.Deleted, (byte)1)
                .Set(x => x.DeletedDate, DateTime.UtcNow)
                .Set(x => x.EntityAnalysisModelsReprocessingRuleInstanceId, entityAnalysisModelsReprocessingRuleInstanceId)
                .UpdateAsync();
        }

        public async Task UpsertAsync(ArchiveKey model, CancellationToken token = default)
        {
            var existing = await dbContext.ArchiveKey
                .FirstOrDefaultAsync(f => f.EntityAnalysisModelInstanceEntryGuid == model.EntityAnalysisModelInstanceEntryGuid
                                          && f.Key == model.Key
                                          && f.ProcessingTypeId == model.ProcessingTypeId, token);

            if (existing == null)
            {
                await dbContext.InsertAsync(model, token: token);
            }
            else
            {
                model.Version = existing.Version + 1;
                model.Id = existing.Id;

                await dbContext.UpdateAsync(model, token: token);

                var audit = new ArchiveKeyVersion
                {
                    ArchiveKeyId = existing.Id,
                    EntityAnalysisModelInstanceEntryGuid = existing.EntityAnalysisModelInstanceEntryGuid,
                    ProcessingTypeId = existing.ProcessingTypeId,
                    Key = existing.Key,
                    KeyValueString = existing.KeyValueString,
                    KeyValueInteger = existing.KeyValueInteger,
                    KeyValueFloat = existing.KeyValueFloat,
                    KeyValueBoolean = existing.KeyValueBoolean,
                    KeyValueDate = existing.KeyValueDate,
                    KeyValueLong = existing.KeyValueLong,
                    Version = existing.Version,
                    EntityAnalysisModelsReprocessingRuleInstanceId = existing.EntityAnalysisModelsReprocessingRuleInstanceId
                };

                await dbContext.InsertAsync(audit, token: token);
            }
        }

        public Task BulkCopyAsync(List<ArchiveKey> models, CancellationToken token = default)
        {
            return dbContext.BulkCopyAsync(models, token);
        }
    }
}
