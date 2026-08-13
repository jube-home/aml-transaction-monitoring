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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Data.Repository;
    using Data.SyntaxTree;
    using Helpers;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;

    public static class SyncEntityAnalysisModelInlineScriptsExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelInlineScriptsAsync(this Context context)
        {
            var shadowEntityAnalysisModelInlineScriptProperties = new Dictionary<string, int>();
            try
            {
                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose determining adding Inline Scripts.");
                    }

                    var repository = new EntityAnalysisModelInlineScriptRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelInlineScriptRepository.GetByEntityAnalysisModelId and entity model key of {key}.");
                    }

                    var records = await repository.GetByEntityAnalysisModelIdOrderByIdAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug("Returned all inline scripts from the database.");
                    }

                    var shadowEntityAnalysisModelInlineScripts = new List<EntityAnalysisModelInlineScript>();
                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId} returned for model {key}.");
                            }

                            if (record.Active != 1)
                            {
                                continue;
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId} returned for model {key} is Active.");
                            }

                            foreach (var inlineScript in context.EntityAnalysisModels.EntityAnalysisModelInlineScripts)
                            {
                                context.Services.CancellationToken.ThrowIfCancellationRequested();

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Inline Script ID {record.EntityAnalysisInlineScriptId} returned for model {key} checking inline script {inlineScript.Id}.");
                                }

                                if (!record.EntityAnalysisInlineScriptId.HasValue)
                                {
                                    continue;
                                }

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {inlineScript.Id} checking to see if matched to this model.");
                                }

                                if (inlineScript.Id !=
                                    record.EntityAnalysisInlineScriptId.Value)
                                {
                                    continue;
                                }

                                foreach (var publicProperty in SyntaxTreeHelpers.GetPublicProperties(inlineScript.InlineScriptCode, inlineScript.LanguageId == 2))
                                {
                                    shadowEntityAnalysisModelInlineScriptProperties.TryAdd(publicProperty.Key, publicProperty.Value.DataTypeId);

                                    var databaseType = publicProperty.Value.DataTypeId switch
                                    {
                                        2 => "::int",
                                        3 => "::float8",
                                        4 => "::timestamp",
                                        5 => "::boolean",
                                        6 => "::float8",
                                        7 => "::float8",
                                        _ => ""
                                    };

                                    value.References.ArchivePayloadSqlSelect +=
                                        $",(a.\"Json\" -> 'payload' ->> '{publicProperty.Key}'){databaseType} AS \"{publicProperty.Key}\"";
                                }

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {inlineScript.Id} is matched to this model.  Will now check if there are grouping keys for this inline script that need to be attached to the model.");
                                }

                                foreach (var searchKey in inlineScript.GroupingKeys)
                                {
                                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {inlineScript.Id} grouping ket {searchKey.SearchKey}.");
                                    }

                                    if (value.Collections.DistinctSearchKeys.TryAdd(searchKey.SearchKey, searchKey))
                                    {
                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {inlineScript.Id} grouping key {searchKey.SearchKey} has been matched.");
                                        }
                                    }
                                    else
                                    {
                                        value.Collections.DistinctSearchKeys[searchKey.SearchKey] = searchKey;
                                    }
                                }

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {inlineScript.Id} is in the cache.");
                                }

                                if (inlineScript == null)
                                {
                                    continue;
                                }

                                shadowEntityAnalysisModelInlineScripts.Add(inlineScript);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} checking inline script {inlineScript.Id} is in the cache and has been added to a shadow list of inline scripts for this model.");
                                }
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Inline Script ID ID {record.EntityAnalysisInlineScriptId.Value} returned for model {key} has created an error as {ex}.");
                        }
                    }

                    context.Services.Parser.EntityAnalysisModelInlineScriptProperties = shadowEntityAnalysisModelInlineScriptProperties;
                    value.Collections.EntityAnalysisModelInlineScripts = shadowEntityAnalysisModelInlineScripts;
                    value.References.PayloadInitialSize = DictionaryNoBoxingHelpers.CalculateInitialSize(value);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            "Entity Start: Inline Scripts have overwritten the main copy with the shadow copy for model {ModelKVP.Key}.");
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: Completed adding Inline Scripts to entity models.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelInlineScriptsAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.InlineScripts, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
