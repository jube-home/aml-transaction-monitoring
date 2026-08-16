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

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Context;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;

    public class HashCacheAssemblyObservabilityTaskStarter(Context context)
    {
        private readonly HashSet<string> recordedScriptHashes = new HashSet<string>();
        private long hashCacheAssemblyInstanceId;

        public async Task StartAsync()
        {
            try
            {
                if (!await TryEstablishInstanceAsync().ConfigureAwait(false))
                {
                    return;
                }

                var waitHashCacheAssemblyObservability =
                    Int32.Parse(context.Services.DynamicEnvironment.AppSettings("WaitHashCacheAssemblyObservability"));

                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    await SnapshotAsync().ConfigureAwait(false);

                    await Task.Delay(waitHashCacheAssemblyObservability, context.Services.TaskCoordinator.CancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation HashCacheAssemblyObservability: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"HashCacheAssemblyObservability: Has produced an error {ex}");
            }
        }

        private async Task<bool> TryEstablishInstanceAsync()
        {
            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);

            try
            {
                var repository = new HashCacheAssemblyInstanceRepository(dbContext);

                var model = await repository.InsertAsync(new HashCacheAssemblyInstance
                {
                    Instance = Dns.GetHostName(),
                    Guid = Guid.NewGuid()
                }, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                hashCacheAssemblyInstanceId = model.Id;

                var entryRepository = new HashCacheAssemblyInstanceEntryRepository(dbContext);
                var existingScriptHashes = await entryRepository.GetAllScriptHashesAsync(
                    context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                recordedScriptHashes.UnionWith(existingScriptHashes);

                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"HashCacheAssemblyObservability: Could not establish instance row {ex}");
                return false;
            }
            finally
            {
                await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SnapshotAsync()
        {
            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);

            try
            {
                var scriptHashes = context.Caching.HashCacheAssembly.Keys.ToList();
                var totalBytes = context.Caching.HashCacheAssemblyMetadata.Values.Sum(metadata => metadata.Bytes);

                var instanceRepository = new HashCacheAssemblyInstanceRepository(dbContext);
                await instanceRepository.UpdateCountAndBytesAsync(hashCacheAssemblyInstanceId, scriptHashes.Count, totalBytes,
                    context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                var journalRepository = new HashCacheAssemblyInstanceJournalRepository(dbContext);
                await journalRepository.InsertAsync(new HashCacheAssemblyInstanceJournal
                {
                    HashCacheAssemblyInstanceId = hashCacheAssemblyInstanceId,
                    Count = scriptHashes.Count,
                    Bytes = totalBytes
                }, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                var newScriptHashes = scriptHashes.Where(scriptHash => !recordedScriptHashes.Contains(scriptHash)).ToList();

                if (newScriptHashes.Count == 0)
                {
                    return;
                }

                var entryRepository = new HashCacheAssemblyInstanceEntryRepository(dbContext);

                foreach (var scriptHash in newScriptHashes)
                {
                    context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                    context.Caching.HashCacheAssemblyMetadata.TryGetValue(scriptHash, out var metadata);

                    await entryRepository.UpsertAsync(new HashCacheAssemblyInstanceEntry
                    {
                        HashCacheAssemblyInstanceId = hashCacheAssemblyInstanceId,
                        ScriptHash = scriptHash,
                        Bytes = metadata?.Bytes,
                        Code = metadata?.Code,
                        Binary = metadata?.Binary
                    }, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                    recordedScriptHashes.Add(scriptHash);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"HashCacheAssemblyObservability: Snapshot failed {ex}");
            }
            finally
            {
                await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
            }
        }
    }
}
