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
    using AutoMapper;
    using Context;
    using LinqToDB;
    using Microsoft.Extensions.Logging.Abstractions;
    using Poco;

    public class ArchiveTagRepository(DbContext dbContext, string userName)
    {
        public async Task MergeTagsAsync(Guid entityAnalysisModelInstanceEntryGuid, string[] tags, CancellationToken token = default)
        {
            await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);
            try
            {
                var archiveTags = new List<ArchiveTag>();
                foreach (var tag in tags.Distinct())
                {
                    var archiveTag = new ArchiveTag
                    {
                        EntityAnalysisModelInstanceEntryGuid = entityAnalysisModelInstanceEntryGuid,
                        Name = tag,
                        CreatedUser = userName,
                        CreatedDate = DateTime.UtcNow
                    };
                    archiveTags.Add(archiveTag);
                    await UpsertAsync(archiveTag, token).ConfigureAwait(false);
                }

                await DeleteWhereNotInListAsync(entityAnalysisModelInstanceEntryGuid, archiveTags, token).ConfigureAwait(false);

                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
            }
            catch
            {
                await dbContext.RollbackTransactionAsync(token).ConfigureAwait(false);
                throw;
            }
        }

        private Task DeleteWhereNotInListAsync(Guid entityAnalysisModelInstanceEntryGuid,
            List<ArchiveTag> archiveTags, CancellationToken token = default)
        {
            var ids = archiveTags
                .Select(x => x.Id)
                .ToList();

            return dbContext.ArchiveTag
                .Where(d => d.EntityAnalysisModelInstanceEntryGuid == entityAnalysisModelInstanceEntryGuid
                            && !ids.Contains(d.Id)
                            && (d.Deleted == null || d.Deleted == 0))
                .Set(x => x.Deleted, (byte)1)
                .Set(x => x.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        private async Task UpsertAsync(ArchiveTag model, CancellationToken token = default)
        {
            var existing = await dbContext.ArchiveTag
                .FirstOrDefaultAsync(f => f.EntityAnalysisModelInstanceEntryGuid == model.EntityAnalysisModelInstanceEntryGuid
                                          && f.Name == model.Name, token);

            if (existing == null)
            {
                model.Version = 1;
                model.Id = await dbContext.InsertWithInt64IdentityAsync(model, token: token);
            }
            else
            {
                model.Version = existing.Version + 1;
                model.Id = existing.Id;

                await dbContext.UpdateAsync(model, token: token);

                var mapper = new Mapper(new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<ArchiveTag, ArchiveTagVersion>();
                }, NullLoggerFactory.Instance));

                var audit = mapper.Map<ArchiveTagVersion>(existing);
                audit.ArchiveTagId = existing.Id;

                await dbContext.InsertAsync(audit, token: token);
            }
        }
    }
}
