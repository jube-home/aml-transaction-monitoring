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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Microsoft.VisualBasic.FileIO;
    using Sanctions;
    using Sanctions.Models;
    using static System.Int32;
    using SanctionEntry=Sanctions.Models.SanctionEntry;

    public class SanctionsTaskStarter(Context context)
    {
        private static readonly HttpClient Client = new HttpClient();
        public async Task StartAsync()
        {
            try
            {
                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                        context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);

                    try
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                "Sanctions Cache Loader: Has opened the database connection for retrieving the Sanctions Cache and Stop Tokens.");
                        }

                        await LoadSanctionsStopTokensAsync(context, dbContext).ConfigureAwait(false);
                        await LoadSanctionsEntriesAsync(context, dbContext).ConfigureAwait(false);
                        await LoadSanctionsFromFilesAsync(context, dbContext).ConfigureAwait(false);
                        context.Sanctions.SanctionsLoadedForStartup = true;

                        await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                        if (context.Services.Log.IsInfoEnabled)
                        {
                            context.Services.Log.Info(
                                "Sanctions Cache Loader: Has finished entries load,  close the database connection and is waiting.");
                        }

                        await Task.Delay(Parse(context.Services.DynamicEnvironment.AppSettings("SanctionLoaderWait")), context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                        throw;
                    }
                    catch (Exception ex)
                    {
                        await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                        context.Services.Log.Error($"Sanctions Cache Loader: Error {ex}");

                        await Task.Delay(Parse(context.Services.DynamicEnvironment.AppSettings("SanctionLoaderWait")), context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation SanctionsAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"SanctionsAsync: has produced an error {ex}");
            }
        }

        private static async Task LoadSanctionsEntriesAsync(Context context, DbContext dbContext)
        {
            try
            {
                var repository = new SanctionsEntryRepository(dbContext);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        "Sanctions Cache Loader: Has instantiated the command object to return all Entries from the Sanctions Cache.");
                }

                var records = await repository.GetAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        "Sanctions Cache Loader: Has executed a reader to return all entries from the Sanctions Cache.");
                }

                foreach (var record in records)
                {
                    context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        if (context.Sanctions.SanctionsEntries.ContainsKey(record.Id))
                        {
                            continue;
                        }

                        var sanctionEntry = new SanctionEntry
                        {
                            SanctionEntrySourceId = record.SanctionEntrySourceId ?? 0,
                            SanctionEntryReference = record.SanctionEntryReference ?? "NA"
                        };

                        var sanctionPayloadStrings = record.SanctionEntryElementValue
                            .Split([" "], StringSplitOptions.RemoveEmptyEntries)
                            .Select(SanctionEntryFileImporter.NormalizeElementValue)
                            .ToArray();

                        sanctionEntry.SanctionElementValue = sanctionPayloadStrings;

                        sanctionEntry.SanctionEntryId = record.Id;

                        context.Sanctions.SanctionsEntries.TryAdd(sanctionEntry.SanctionEntryId, sanctionEntry);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Services.Log.Error($"Sanctions Cache Loader: Error loading a hash value {ex}");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"LoadSanctionsEntriesAsync: has produced an error {ex}");
            }
        }

        private static async Task LoadSanctionsFromFilesAsync(Context context, DbContext dbContext)
        {
            try
            {
                var sanctionEntriesSources = await GetSanctionsSourcesAsync(context, dbContext).ConfigureAwait(false);

                if (context.Services.DynamicEnvironment.AppSettings("EnableSanctionLoader").Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    var processSanctionEntriesSources = sanctionEntriesSources.ToList();
                    foreach (var processSanctionEntriesSource in processSanctionEntriesSources)
                    {
                        context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (processSanctionEntriesSource.EnableHttpLocation)
                            {
                                var import = await StartImportAsync(dbContext,
                                    processSanctionEntriesSource.SanctionEntrySourceId,
                                    context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                try
                                {
                                    using var response = await Client.GetAsync(processSanctionEntriesSource.HttpLocation, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                                    response.EnsureSuccessStatusCode();

                                    var stream = await response.Content.ReadAsStreamAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                                    await using var stream1 = stream.ConfigureAwait(false);

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info($"Sanctions Loader: HTTP request successful for {processSanctionEntriesSource.HttpLocation}.");
                                    }

                                    using var tfp = new TextFieldParser(stream);
                                    tfp.Delimiters =
                                    [
                                        processSanctionEntriesSource.Delimiter
                                    ];

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info("Sanctions Loader: Connection established, data downloaded, and opened with TextFieldParser.");
                                    }

                                    var (result, inserted, revived, unchanged) = await ProcessTextFieldParserAsync(
                                        context, dbContext, tfp, processSanctionEntriesSource,
                                        processSanctionEntriesSource.Skip, import.Id).ConfigureAwait(false);

                                    var removedCount = await ReconcileSourceAsync(context, dbContext,
                                        processSanctionEntriesSource.SanctionEntrySourceId, result.Hashes).ConfigureAwait(false);

                                    await CompleteImportAsync(dbContext, import, result, inserted, revived, unchanged,
                                        removedCount, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info(
                                            $"Sanctions Loader: Has made a connection to {processSanctionEntriesSource.HttpLocation} has finished using the Text Field Parser.");
                                    }
                                }
                                catch (Exception ex) when (ex is not OperationCanceledException)
                                {
                                    await FailImportAsync(dbContext, import, ex,
                                        context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                    throw;
                                }
                            }
                            else
                            {
                                if (Directory.Exists(processSanctionEntriesSource.DirectoryLocation)
                                    && processSanctionEntriesSource.EnableDirectoryLocation)
                                {
                                    var files = Directory.GetFiles(processSanctionEntriesSource.DirectoryLocation);

                                    if (files.Length > 0)
                                    {
                                        var import = await StartImportAsync(dbContext,
                                            processSanctionEntriesSource.SanctionEntrySourceId,
                                            context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                        try
                                        {
                                            var seenHashes = new HashSet<string>();
                                            var totalRows = 0;
                                            var rejectedRows = 0;
                                            var inserted = 0;
                                            var revived = 0;
                                            var unchanged = 0;

                                            foreach (var fileWithinLoop in files)
                                            {
                                                context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                                                try
                                                {
                                                    if (context.Services.Log.IsInfoEnabled)
                                                    {
                                                        context.Services.Log.Info(
                                                            "Sanctions Loader: Has loaded the database connection. Will now try and open it using the Text Field Parser.");
                                                    }

                                                    var tfp = new TextFieldParser(fileWithinLoop)
                                                    {
                                                        Delimiters = [processSanctionEntriesSource.Delimiter]
                                                    };

                                                    var (result, fileInserted, fileRevived, fileUnchanged) =
                                                        await ProcessTextFieldParserAsync(context, dbContext, tfp,
                                                            processSanctionEntriesSource,
                                                            processSanctionEntriesSource.Skip, import.Id).ConfigureAwait(false);

                                                    seenHashes.UnionWith(result.Hashes);
                                                    totalRows += result.TotalRows;
                                                    rejectedRows += result.RejectedRows;
                                                    inserted += fileInserted;
                                                    revived += fileRevived;
                                                    unchanged += fileUnchanged;

                                                    if (context.Services.Log.IsInfoEnabled)
                                                    {
                                                        context.Services.Log.Info(
                                                            "Sanctions Loader: Has finished looping through the Sanctions and has closed the database connection and the file.");
                                                    }

                                                    if (context.Services.Log.IsInfoEnabled)
                                                    {
                                                        context.Services.Log.Info($"Sanctions Loader: Is about to delete {fileWithinLoop}.");
                                                    }

                                                    File.Delete(fileWithinLoop);

                                                    if (context.Services.Log.IsInfoEnabled)
                                                    {
                                                        context.Services.Log.Info($"Sanctions Loader: Has deleted {fileWithinLoop}.");
                                                    }
                                                }
                                                catch (Exception ex) when (ex is not OperationCanceledException)
                                                {
                                                    if (context.Services.Log.IsInfoEnabled)
                                                    {
                                                        context.Services.Log.Info($"Sanctions Loader: Error loading record {ex}");
                                                    }
                                                }
                                            }

                                            var removedCount = await ReconcileSourceAsync(context, dbContext,
                                                processSanctionEntriesSource.SanctionEntrySourceId, seenHashes).ConfigureAwait(false);

                                            await CompleteImportAsync(dbContext, import,
                                                new SanctionEntryFileImportResult(seenHashes, totalRows, rejectedRows),
                                                inserted, revived, unchanged, removedCount,
                                                context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                                        }
                                        catch (Exception ex) when (ex is not OperationCanceledException)
                                        {
                                            await FailImportAsync(dbContext, import, ex,
                                                context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                            throw;
                                        }
                                    }
                                }
                                else
                                {
                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info(
                                            $"Sanctions Loader: Directory does not exist {processSanctionEntriesSource.DirectoryLocation} for {processSanctionEntriesSource.SanctionEntrySourceId}.");
                                    }
                                }
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            if (context.Services.Log.IsInfoEnabled)
                            {
                                context.Services.Log.Info(
                                    $"Sanctions Loader: Has made a connection to {processSanctionEntriesSource.HttpLocation} has created an error as {ex}.");
                            }
                        }
                    }
                }
                else
                {
                    if (context.Services.Log.IsInfoEnabled)
                    {
                        context.Services.Log.Info("Sanctions Loader: Sanctions loading is disabled on this server.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"LoadSanctionsFromFilesAsync: has produced an error {ex}");
            }
        }

        private static async Task<IEnumerable<SanctionEntriesSource>> GetSanctionsSourcesAsync(Context context, DbContext dbContext)
        {
            var sanctionEntriesSources = new List<SanctionEntriesSource>();
            try
            {
                var repository = new SanctionEntrySourceRepository(dbContext);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        "Sanctions Cache Loader: Has instantiated the command object to return all Sources for the Sanctions Cache.");
                }

                var records = await repository.GetAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        "Sanctions Cache Loader: Has executed a reader to return all Sources for the Sanctions Cache.");
                }

                foreach (var record in records)
                {
                    context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        SanctionEntriesSource sanctionEntriesSource;

                        if (!context.Sanctions.SanctionsSources.TryGetValue(record.Id, out var source))
                        {
                            sanctionEntriesSource = new SanctionEntriesSource
                            {
                                SanctionEntrySourceId = record.Id
                            };
                            context.Sanctions.SanctionsSources.TryAdd(record.Id, sanctionEntriesSource);
                        }
                        else
                        {
                            sanctionEntriesSource = source;
                        }

                        sanctionEntriesSource.Name = record.Name ?? "";

                        if (record.Severity.HasValue)
                        {
                        }

                        if (record.EnableHttpLocation != null)
                        {
                            sanctionEntriesSource.EnableHttpLocation = record.EnableHttpLocation == 1;
                        }
                        else
                        {
                            sanctionEntriesSource.EnableHttpLocation = false;
                        }

                        if (record.EnableDirectoryLocation.HasValue)
                        {
                            sanctionEntriesSource.EnableDirectoryLocation = record.EnableDirectoryLocation == 1;
                        }
                        else
                        {
                            sanctionEntriesSource.EnableDirectoryLocation = false;
                        }

                        if (record.DirectoryLocation != null)
                        {
                            sanctionEntriesSource.DirectoryLocation = record.DirectoryLocation;
                        }

                        if (record.HttpLocation != null)
                        {
                            sanctionEntriesSource.HttpLocation = record.HttpLocation;
                        }

                        sanctionEntriesSource.Delimiter =
                            record.Delimiter.HasValue ? record.Delimiter.Value.ToString() : ",";

                        sanctionEntriesSource.Skip = record.Skip ?? 0;

                        if (record.MultiPartStringIndex != null)
                        {
                            sanctionEntriesSource.MultiPartStringIndex = record.MultiPartStringIndex;
                        }

                        if (record.ReferenceIndex.HasValue)
                        {
                            sanctionEntriesSource.ReferenceIndex = record.ReferenceIndex.Value;
                        }

                        sanctionEntriesSource.SanctionEntrySourceId = record.Id;

                        sanctionEntriesSources.Add(sanctionEntriesSource);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Services.Log.Error($"Sanctions Cache Loader: has created an error as {ex}.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"GetSanctionsSourcesAsync: has produced an error {ex}");
            }

            return sanctionEntriesSources;
        }

        private static async Task LoadSanctionsStopTokensAsync(Context context, DbContext dbContext)
        {
            try
            {
                var repository = new SanctionStopTokenRepository(dbContext);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        "Sanctions Cache Loader: Has instantiated the command object to return all Stop Tokens for the Sanctions Cache.");
                }

                var records = await repository.GetAsync(context.Services.TaskCoordinator.CancellationToken)
                    .ConfigureAwait(false);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        "Sanctions Cache Loader: Has executed a reader to return all Stop Tokens for the Sanctions Cache.");
                }

                foreach (var record in records)
                {
                    context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        if (String.IsNullOrWhiteSpace(record.Token))
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Sanctions Cache Loader: Stop Token id {record.Id} is null or whitespace and has been skipped.");
                            }

                            continue;
                        }

                        context.Sanctions.SanctionsStopTokens.TryAdd(record.Token, record.CategoryId ?? 1);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Services.Log.Error($"Sanctions Cache Loader: has created an error as {ex}.");
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Sanctions Cache Loader: Has loaded {context.Sanctions.SanctionsStopTokens} Stop Tokens for the Sanctions Cache.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"GetSanctionsStopTokensAsync: has produced an error {ex}");
            }
        }

        private static async Task<int> ReconcileSourceAsync(Context context, DbContext dbContext,
            int sanctionEntrySourceId, HashSet<string> seenHashes)
        {
            var repository = new SanctionsEntryRepository(dbContext);

            var removed = await SanctionEntryFileImporter.ReconcileRemovedAsync(repository, sanctionEntrySourceId,
                seenHashes, "Sanctions Loader", context.Services.Log,
                context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

            foreach (var entry in removed)
            {
                context.Sanctions.SanctionsEntries.TryRemove(entry.Id, out _);
            }

            return removed.Count;
        }

        private static Task<SanctionEntryImport> StartImportAsync(DbContext dbContext,
            int sanctionEntrySourceId, CancellationToken token)
        {
            var repository = new SanctionEntryImportRepository(dbContext);

            return repository.InsertAsync(new SanctionEntryImport
            {
                SanctionEntrySourceId = sanctionEntrySourceId,
                StartDate = DateTime.UtcNow,
                CreatedUser = "Sanctions Loader",
                CreatedDate = DateTime.UtcNow
            }, token);
        }

        private static Task CompleteImportAsync(DbContext dbContext, SanctionEntryImport import,
            SanctionEntryFileImportResult result, int inserted, int revived, int unchanged, int removed,
            CancellationToken token)
        {
            import.EndDate = DateTime.UtcNow;
            import.TotalRows = result.TotalRows;
            import.InsertedCount = inserted;
            import.RevivedCount = revived;
            import.UnchangedCount = unchanged;
            import.RemovedCount = removed;
            import.RejectedCount = result.RejectedRows;
            import.Successful = 1;

            return new SanctionEntryImportRepository(dbContext).UpdateAsync(import, token);
        }

        private static Task FailImportAsync(DbContext dbContext, SanctionEntryImport import, Exception ex,
            CancellationToken token)
        {
            import.EndDate = DateTime.UtcNow;
            import.Successful = 0;
            import.ErrorMessage = ex.Message;

            return new SanctionEntryImportRepository(dbContext).UpdateAsync(import, token);
        }

        private static async Task<(SanctionEntryFileImportResult Result, int Inserted, int Revived, int Unchanged)>
            ProcessTextFieldParserAsync(Context context, DbContext dbContext, TextFieldParser tfp,
                SanctionEntriesSource processSanctionEntriesSource, int skip, int sanctionEntryImportId)
        {
            var repository = new SanctionsEntryRepository(dbContext);
            var rejectionRepository = new SanctionEntryRejectionRepository(dbContext);

            var inserted = 0;
            var revived = 0;
            var unchanged = 0;

            var result = await SanctionEntryFileImporter.ImportAsync(tfp,
                processSanctionEntriesSource.SanctionEntrySourceId,
                processSanctionEntriesSource.MultiPartStringIndex, processSanctionEntriesSource.ReferenceIndex, skip,
                async (record, token) =>
                {
                    var insert = new Data.Poco.SanctionEntry
                    {
                        SanctionEntryElementValue = record.ElementValue,
                        SanctionEntrySourceId = processSanctionEntriesSource.SanctionEntrySourceId,
                        SanctionPayload = record.Payload,
                        SanctionEntryReference = record.Reference,
                        SanctionEntryHash = record.Hash
                    };

                    var (persisted, outcome) = await repository.UpsertAsync(insert, token).ConfigureAwait(false);

                    switch (outcome)
                    {
                        case SanctionEntryUpsertOutcome.Inserted:
                            inserted++;
                            break;
                        case SanctionEntryUpsertOutcome.Revived:
                            revived++;
                            break;
                        default:
                            unchanged++;
                            break;
                    }

                    var sanctionEntry = new SanctionEntry
                    {
                        SanctionEntrySourceId = processSanctionEntriesSource.SanctionEntrySourceId,
                        SanctionEntryReference = !String.IsNullOrEmpty(record.Reference) ? record.Reference : "NA",
                        SanctionElementValue = record.ElementValue
                            .Split([" "], StringSplitOptions.RemoveEmptyEntries)
                            .Select(SanctionEntryFileImporter.NormalizeElementValue)
                            .ToArray(),
                        SanctionEntryId = persisted.Id
                    };

                    if (context.Sanctions.SanctionsEntries.TryAdd(persisted.Id, sanctionEntry))
                    {
                        if (context.Services.Log.IsInfoEnabled)
                        {
                            context.Services.Log.Info(
                                $"Sanctions Loader: Has loaded records with value of {record.ElementValue} for source {processSanctionEntriesSource.SanctionEntrySourceId} with reference of {record.Reference} and a hash value of {record.Hash}.");
                        }
                    }
                    else if (context.Services.Log.IsInfoEnabled)
                    {
                        context.Services.Log.Info(
                            $"Sanctions Loader: Has not reloaded records with value of {record.ElementValue} for source {processSanctionEntriesSource.SanctionEntrySourceId} with reference of {record.Reference} and a hash value of {record.Hash} as already exists.");
                    }
                },
                async (rejection, token) =>
                {
                    await rejectionRepository.InsertAsync(new SanctionEntryRejection
                    {
                        SanctionEntryImportId = sanctionEntryImportId,
                        SanctionEntrySourceId = processSanctionEntriesSource.SanctionEntrySourceId,
                        RowNumber = rejection.RowNumber,
                        RawData = rejection.RawData,
                        ReasonId = (int)rejection.ReasonId,
                        CreatedDate = DateTime.UtcNow
                    }, token).ConfigureAwait(false);
                },
                context.Services.Log,
                context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

            return (result, inserted, revived, unchanged);
        }
    }
}
