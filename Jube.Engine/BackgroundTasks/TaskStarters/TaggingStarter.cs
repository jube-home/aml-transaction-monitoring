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

namespace Jube.Engine.BackgroundTasks.TaskStarters
{
    using System;
    using System.Threading.Tasks;
    using Context;
    using Data.Context;
    using Data.Repository;

    public class TaggingStarter(Context context)
    {
        public async Task StartAsync()
        {
            try
            {
                var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                    context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);

                var repository = new ArchiveRepository(dbContext);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Tagging: Created db context and repository. Entering loop to look for new tags.");
                }

                while (true)
                {
                    if (context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested && context.ConcurrentQueues.PendingTagging.IsEmpty)
                    {
                        context.Services.Log.Info("Tagging: Cancellation requested and queue empty. Exiting.");
                        break;
                    }

                    if (context.ConcurrentQueues.PendingTagging.TryDequeue(out var tag))
                    {
                        try
                        {
                            if (context.Services.Log.IsInfoEnabled)
                            {
                                context.Services.Log.Info($"Tagging: Found {tag.EntityAnalysisModelInstanceEntryGuid} with Tags {tag.Tag}. Fetching record.");
                            }

                            var archive = await repository
                                .UpdateTagsByEntityAnalysisModelInstanceEntryGuidAsync(
                                    tag.EntityAnalysisModelInstanceEntryGuid,
                                    tag.Tag,
                                    context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                            var caseRepository = new CaseRepository(dbContext);
                            await caseRepository.UpdateArchiveJsonAsync(tag.EntityAnalysisModelInstanceEntryGuid, archive.Json, context.Services.TaskCoordinator.CancellationToken);

                            var archiveTagRepository = new ArchiveTagRepository(dbContext, tag.UserName);
                            await archiveTagRepository.MergeTagsAsync(tag.EntityAnalysisModelInstanceEntryGuid, tag.Tag, context.Services.TaskCoordinator.CancellationToken);

                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error($"Tagging: Error updating {tag.EntityAnalysisModelInstanceEntryGuid} with {ex}");
                        }
                    }
                    else
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug("Tagging: No pending tags. Waiting before trying again.");
                        }

                        await Task.Delay(1000, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"TaggingAsync: has produced an error {ex}");
            }
        }
    }
}
