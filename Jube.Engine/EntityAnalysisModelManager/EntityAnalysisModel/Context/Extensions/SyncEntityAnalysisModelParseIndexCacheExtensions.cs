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

    public static class SyncEntityAnalysisModelParseIndexCacheExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelParseIndexCacheAsync(this Context context)
        {
            try
            {
                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Building ParseIndexCache for model {key}.");
                    }

                    var parseIndexCache = new Dictionary<int, string>();

                    foreach (var xpath in value.Collections.EntityAnalysisModelRequestXPaths)
                    {
                        if (!xpath.Cache)
                        {
                            continue;
                        }
                        parseIndexCache[xpath.CacheIndexId] = xpath.Name;
                    }

                    foreach (var script in value.Collections.EntityAnalysisModelInlineScripts)
                    foreach (var (attributeName, attribute) in script.EntityAnalysisModelInlineScriptPropertyAttributes)
                    {
                        if (attribute.CacheIndexId is not null)
                        {
                            parseIndexCache.TryAdd(attribute.CacheIndexId.Value, attributeName);
                        }
                    }

                    value.Collections.ParseIndexCache = parseIndexCache;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: ParseIndexCache for model {key} built with {parseIndexCache.Count} entries.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelParseIndexCacheAsync: has produced an error {ex}.");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.ParseIndexCache, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
