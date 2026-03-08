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
    using System.Text;
    using System.Threading.Tasks;
    using Context;
    using Data.Context;
    using Data.Repository;
    using EntityAnalysisModelManager.Helpers;
    using Microsoft.VisualBasic.FileIO;
    using Sanctions;
    using Sanctions.Models;
    using static System.Int32;
    using SanctionEntry=Sanctions.Models.SanctionEntry;

    public class SanctionsTaskStarter(Context context)
    {
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
                                "Sanctions Cache Loader: Has opened the database connection for retrieving the Sanctions Cache.");
                        }

                        await LoadSanctionsEntriesAsync(context, dbContext).ConfigureAwait(false);

                        context.Sanctions.SanctionsLoadedForStartup = true;

                        await LoadSanctionsFromFilesAsync(context, dbContext).ConfigureAwait(false);

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

                        var sanctionPayloadStrings =
                            record.SanctionEntryElementValue
                                .Split([" "], StringSplitOptions.RemoveEmptyEntries);

                        for (var i = 0; i < sanctionPayloadStrings.Length; i++)
                        {
                            sanctionPayloadStrings[i] =
                                LevenshteinDistance.Clean(sanctionPayloadStrings[i]);
                        }

                        sanctionEntry.SanctionElementValue = sanctionPayloadStrings;

                        sanctionEntry.SanctionEntryId = record.Id;

                        context.Sanctions.SanctionsEntries.Add(sanctionEntry.SanctionEntryId, sanctionEntry);
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
                if (context.Services.DynamicEnvironment.AppSettings("EnableSanctionLoader").Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    var sanctionEntriesSources = await GetSanctionsSourcesAsync(context, dbContext).ConfigureAwait(false);

                    var processSanctionEntriesSources = sanctionEntriesSources.ToList();
                    foreach (var processSanctionEntriesSource in processSanctionEntriesSources)
                    {
                        context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (processSanctionEntriesSource.EnableHttpLocation)
                            {
                                using var client = new HttpClient();

                                using var response = await client.GetAsync(processSanctionEntriesSource.HttpLocation, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
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

                                await ProcessTextFieldParserAsync(context, dbContext, tfp, processSanctionEntriesSource,
                                    processSanctionEntriesSource.Skip).ConfigureAwait(false);

                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Info(
                                        $"Sanctions Loader: Has made a connection to {processSanctionEntriesSource.HttpLocation} has finished using the Text Field Parser.");
                                }
                            }
                            else
                            {
                                if (Directory.Exists(processSanctionEntriesSource.DirectoryLocation)
                                    && processSanctionEntriesSource.EnableDirectoryLocation)
                                {
                                    var files = Directory.GetFiles(processSanctionEntriesSource.DirectoryLocation);
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

                                            await ProcessTextFieldParserAsync(context, dbContext, tfp, processSanctionEntriesSource,
                                                processSanctionEntriesSource.Skip).ConfigureAwait(false);

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
                var repository = new SanctionsEntriesSourcesRepository(dbContext);

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
                            context.Sanctions.SanctionsSources.Add(record.Id, sanctionEntriesSource);
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

        private static async Task ProcessTextFieldParserAsync(Context context, DbContext dbContext, TextFieldParser tfp,
            SanctionEntriesSource processSanctionEntriesSource, int skip)
        {
            try
            {
                tfp.TextFieldType = FieldType.Delimited;
                var i = 1;
                while (!tfp.EndOfData)
                {
                    try
                    {
                        if (context.Services.Log.IsInfoEnabled)
                        {
                            context.Services.Log.Info(
                                $"Sanctions Loader: Has loaded the database connection.  Is processing record {i}.  Will now build the SQL Command object.");
                        }

                        var data = tfp.ReadFields();

                        if (i > skip)
                        {
                            if (data.Length > 1)
                            {
                                var repository = new SanctionsEntryRepository(dbContext);

                                var sanctionEntry = new SanctionEntry();
                                var insert = new Data.Poco.SanctionEntry();

                                var sb = new StringBuilder();
                                var first = true;
                                foreach (var sanctionSourceElementLocation in
                                         processSanctionEntriesSource.MultiPartStringIndex.Split(
                                             ",".ToCharArray()))
                                {
                                    if (first)
                                    {
                                        first = false;
                                    }
                                    else
                                    {
                                        sb.Append(' ');
                                    }

                                    sb.Append(TryParse(sanctionSourceElementLocation, out var parsedInt) ? data[parsedInt] : data[0]);
                                }

                                insert.SanctionEntryElementValue = sb.ToString();
                                insert.SanctionEntrySourceId = processSanctionEntriesSource.SanctionEntrySourceId;
                                insert.SanctionPayload = String.Join(',', data);
                                insert.SanctionEntryReference = data[processSanctionEntriesSource.ReferenceIndex];

                                var hashValue = HashHelper.GetHash(
                                    processSanctionEntriesSource.SanctionEntrySourceId +
                                    sb.ToString() +
                                    data[processSanctionEntriesSource.ReferenceIndex]);

                                insert.SanctionEntryHash = hashValue;
                                insert.SanctionEntrySourceId = processSanctionEntriesSource.SanctionEntrySourceId;

                                insert = await repository.UpsertAsync(insert, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                if (context.Sanctions.SanctionsEntries.TryAdd(insert.Id, sanctionEntry))
                                {
                                    var sanctionPayloadStrings =
                                        sb.ToString()
                                            .Split([" "], StringSplitOptions.RemoveEmptyEntries);

                                    for (var j = 0; j < sanctionPayloadStrings.Length; j++)
                                    {
                                        sanctionPayloadStrings[j] =
                                            LevenshteinDistance.Clean(sanctionPayloadStrings[j]);
                                    }

                                    sanctionEntry.SanctionEntrySourceId =
                                        processSanctionEntriesSource.SanctionEntrySourceId;

                                    sanctionEntry.SanctionEntryReference =
                                        !String.IsNullOrEmpty(data[processSanctionEntriesSource.ReferenceIndex])
                                            ? data[processSanctionEntriesSource.ReferenceIndex]
                                            : "NA";

                                    sanctionEntry.SanctionElementValue = sanctionPayloadStrings;
                                    sanctionEntry.SanctionEntryId = insert.Id;

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info(
                                            $"Sanctions Loader: Has loaded records with value of {sb} for source {processSanctionEntriesSource.SanctionEntrySourceId} with reference of {data[processSanctionEntriesSource.ReferenceIndex]} and a hash value of {hashValue}.");
                                    }
                                }
                                else
                                {
                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info(
                                            $"Sanctions Loader: Has not reloaded records with value of {sb} for source {processSanctionEntriesSource.SanctionEntrySourceId} with reference of {data[processSanctionEntriesSource.ReferenceIndex]} and a hash value of {hashValue} as already exists.");
                                    }
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Info($"Sanctions Loader: record {i} has no data.");
                                }
                            }
                        }
                        else
                        {
                            if (context.Services.Log.IsInfoEnabled)
                            {
                                context.Services.Log.Info(
                                    $"Sanctions Loader: Skipped header row {i}");
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        if (context.Services.Log.IsInfoEnabled)
                        {
                            context.Services.Log.Info($"Sanctions Loader: Error loading record {ex}");
                        }
                    }
                    finally
                    {
                        i += 1;
                        if (context.Services.Log.IsInfoEnabled)
                        {
                            context.Services.Log.Info($"Sanctions Loader: Moving to record {i}.");
                        }
                    }
                }

                tfp.Close();

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Sanctions Loader: Has loaded the database connection.  Has set the delimiter to {processSanctionEntriesSource.Delimiter}.  Is about to start processing the records.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"ProcessTextFieldParser: has produced an error {ex}");
            }
        }
    }
}
