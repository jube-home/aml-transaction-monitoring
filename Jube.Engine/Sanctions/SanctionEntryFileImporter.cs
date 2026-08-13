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

namespace Jube.Engine.Sanctions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Repository;
    using EntityAnalysisModelManager.Helpers;
    using log4net;
    using Microsoft.VisualBasic.FileIO;
    using Models;
    using SanctionEntry=Data.Poco.SanctionEntry;

    public static class SanctionEntryFileImporter
    {
        public static async Task<SanctionEntryFileImportResult> ImportAsync(TextFieldParser tfp,
            int sanctionEntrySourceId, string multiPartStringIndex, int? referenceIndex, int skip,
            Func<SanctionEntryFileRecord, CancellationToken, Task> onRecordAsync,
            Func<SanctionEntryFileRejection, CancellationToken, Task> onRejectedAsync, ILog log,
            CancellationToken token = default)
        {
            var seenHashes = new HashSet<string>();
            var totalRows = 0;
            var rejectedRows = 0;

            try
            {
                tfp.TextFieldType = FieldType.Delimited;
                var i = 1;
                while (!tfp.EndOfData)
                {
                    var isDataRow = i > skip;
                    var rejected = false;

                    try
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Sanctions Loader: Has loaded the database connection.  Is processing record {i}.  Will now build the SQL Command object.");
                        }

                        var data = tfp.ReadFields();

                        if (isDataRow)
                        {
                            if (data is { Length: > 1 })
                            {
                                var record = BuildRecord(data, sanctionEntrySourceId, multiPartStringIndex,
                                    referenceIndex);

                                if (record != null)
                                {
                                    seenHashes.Add(record.Hash);
                                    await onRecordAsync(record, token).ConfigureAwait(false);
                                }
                                else
                                {
                                    rejected = true;
                                    await RejectAsync(onRejectedAsync, i, data,
                                            SanctionEntryRejectionReason.NoReferenceIndexConfigured, token)
                                        .ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                rejected = true;
                                await RejectAsync(onRejectedAsync, i, data,
                                    SanctionEntryRejectionReason.InsufficientFields, token).ConfigureAwait(false);

                                if (log.IsInfoEnabled)
                                {
                                    log.Info($"Sanctions Loader: record {i} has no data.");
                                }
                            }
                        }
                        else if (log.IsInfoEnabled)
                        {
                            log.Info($"Sanctions Loader: Skipped header row {i}");
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        if (isDataRow && !rejected)
                        {
                            rejected = true;
                            await RejectAsync(onRejectedAsync, i, null, SanctionEntryRejectionReason.ParseError,
                                token).ConfigureAwait(false);
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info($"Sanctions Loader: Error loading record {ex}");
                        }
                    }
                    finally
                    {
                        if (isDataRow)
                        {
                            totalRows += 1;
                            if (rejected)
                            {
                                rejectedRows += 1;
                            }
                        }

                        i += 1;
                        if (log.IsInfoEnabled)
                        {
                            log.Info($"Sanctions Loader: Moving to record {i}.");
                        }
                    }
                }

                tfp.Close();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"ProcessTextFieldParser: has produced an error {ex}");
            }

            return new SanctionEntryFileImportResult(seenHashes, totalRows, rejectedRows);
        }

        private static Task RejectAsync(Func<SanctionEntryFileRejection, CancellationToken, Task> onRejectedAsync,
            int rowNumber, string[] data, SanctionEntryRejectionReason reasonId, CancellationToken token)
        {
            if (onRejectedAsync == null)
            {
                return Task.CompletedTask;
            }

            var rawData = data != null ? String.Join(',', data) : null;
            return onRejectedAsync(new SanctionEntryFileRejection(rowNumber, rawData, reasonId), token);
        }

        public static async Task<IReadOnlyCollection<SanctionEntry>> ReconcileRemovedAsync(
            SanctionsEntryRepository repository, int sanctionEntrySourceId,
            IReadOnlyCollection<string> currentHashes, string userName, ILog log,
            CancellationToken token = default)
        {
            var removed = new List<SanctionEntry>();

            if (currentHashes.Count == 0)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Sanctions Loader: Skipped removal reconciliation for source {sanctionEntrySourceId} as no records were parsed from the file this run.");
                }

                return removed;
            }

            try
            {
                var active = await repository
                    .GetActiveBySanctionEntrySourceIdAsync(sanctionEntrySourceId, token)
                    .ConfigureAwait(false);

                foreach (var entry in active)
                {
                    token.ThrowIfCancellationRequested();

                    if (currentHashes.Contains(entry.SanctionEntryHash))
                    {
                        continue;
                    }

                    await repository.DeleteAsync(entry.Id, userName, token).ConfigureAwait(false);
                    removed.Add(entry);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Sanctions Loader: Has removed record {entry.Id} for source {sanctionEntrySourceId} as it is no longer present in the source file.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"ReconcileRemovedAsync: has produced an error {ex}");
            }

            return removed;
        }

        private static SanctionEntryFileRecord BuildRecord(string[] data, int sanctionEntrySourceId,
            string multiPartStringIndex, int? referenceIndex)
        {
            if (referenceIndex == null)
            {
                return null;
            }

            var sb = new StringBuilder();
            var first = true;
            foreach (var location in multiPartStringIndex.Split(','))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(' ');
                }

                sb.Append(Int32.TryParse(location, out var parsedIndex) ? data[parsedIndex] : data[0]);
            }

            var elementValue = sb.ToString();
            var reference = data[referenceIndex.Value];
            var hash = HashHelper.GetHash(sanctionEntrySourceId + elementValue + reference);

            return new SanctionEntryFileRecord(elementValue, reference, hash, String.Join(',', data));
        }

        public static string NormalizeElementValue(string raw)
        {
            return raw
                .Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .Aggregate(new StringBuilder(), (sb, c) => sb.Append(c))
                .ToString()
                .ToLowerInvariant()
                .Replace("-", "")
                .Replace(",", "")
                .Replace(".", "")
                .Replace("'", "");
        }
    }
}
