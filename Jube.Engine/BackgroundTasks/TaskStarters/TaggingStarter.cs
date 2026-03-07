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
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Context;
    using Data.Context;
    using Data.Repository;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class TaggingStarter(Context context)
    {
        public async Task StartAsync()
        {
            try
            {
                var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                    context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);

                var repository = new ArchiveRepository(dbContext);

                var serializer = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = context.Services.ContractResolver
                };

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
                                context.Services.Log.Info($"Tagging: Found {tag.EntityAnalysisModelInstanceEntryGuid} with Name {tag.Name} and Value {tag.Value}. Fetching record.");
                            }

                            var archive = await repository
                                .GetByEntityAnalysisModelInstanceEntryGuidAndEntityAnalysisModelIdAsync(
                                    tag.EntityAnalysisModelInstanceEntryGuid, tag.EntityAnalysisModelId, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                            if (archive != null)
                            {
                                var jObject = JObject.Parse(archive.Json);

                                if (jObject.TryGetValue("tag", out var jToken))
                                {
                                    jToken[tag.Name] = tag.Value;

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info($"Tagging: Added tag {tag.Name}={tag.Value} to record {archive.Id}. Updating json.");
                                    }

                                    var stream = new MemoryStream();
                                    var streamWriter = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
                                    await using (streamWriter.ConfigureAwait(false))
                                    {
                                        var jsonWriter = new JsonTextWriter(streamWriter);
                                        await using (jsonWriter.ConfigureAwait(false))
                                        {
                                            serializer.Serialize(jsonWriter, jObject);
                                            await jsonWriter.FlushAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                                            await streamWriter.FlushAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                                        }
                                    }

                                    stream.Seek(0, SeekOrigin.Begin);
                                    archive.Json = Encoding.UTF8.GetString(stream.ToArray());
                                    await repository.UpdateAsync(archive, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info($"Tagging: Updated json for record {archive.Id}.");
                                    }
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Info($"Tagging: No record found for {tag.EntityAnalysisModelInstanceEntryGuid} and model {tag.EntityAnalysisModelId}.");
                                }
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error($"Tagging: Error updating {tag.EntityAnalysisModelInstanceEntryGuid} for model {tag.EntityAnalysisModelId}: {ex}");
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
