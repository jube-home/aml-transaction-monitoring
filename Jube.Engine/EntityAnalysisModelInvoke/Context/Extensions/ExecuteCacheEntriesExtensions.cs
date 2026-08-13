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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using Cache;
    using Dictionary;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using TaskCancellation.TaskHelper;

    public static class ExecuteCacheEntriesExtensions
    {
        public static Context ExecuteCacheDbStorage(this Context context, CacheService cacheService,
            Dictionary<string, DistinctSearchKey> distinctSearchKeys)
        {
            InsertOrReplaceCacheEntries(context, cacheService);
            UpsertCachePayloadLatest(context, cacheService, distinctSearchKeys);

            return context;
        }

        private static void UpsertCachePayloadLatest(Context context, CacheService cacheService,
            Dictionary<string, DistinctSearchKey> distinctSearchKeys)
        {
            foreach (var (key, _) in distinctSearchKeys)
            {
                context.EntityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(key, out var searchKeyValue);

                context.PendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.CachePayloadLatestUpsertAsync, async () => await cacheService.CachePayloadLatestRepository.UpsertAsync(
                    context.EntityAnalysisModel.Instance.TenantRegistryId,
                    context.EntityAnalysisModel.Instance.Guid,
                    context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate,
                    context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid,
                    key, searchKeyValue.AsString()).ConfigureAwait(false)));
            }
        }

        private static void InsertOrReplaceCacheEntries(Context context, CacheService cacheService)
        {
            if (context.EntityAnalysisModel.Flags.EnableCache)
            {
                var reducedInternedPayload = ParseToReducedInternedPayload(context);

                if (context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
                {
                    context.PendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.CachePayloadUpsertAsync, async () => await cacheService.CachePayloadRepository.UpsertAsync(
                        context.EntityAnalysisModel.Instance.TenantRegistryId,
                        context.EntityAnalysisModel.Instance.Guid,
                        reducedInternedPayload,
                        context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate,
                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid).ConfigureAwait(false)));

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has replaced the entity into the cache db serially.");
                    }
                }
                else
                {
                    context.PendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.CachePayloadInsertAsync, async () => await cacheService.CachePayloadRepository.InsertAsync(
                        context.EntityAnalysisModel.Instance.TenantRegistryId,
                        context.EntityAnalysisModel.Instance.Guid,
                        reducedInternedPayload,
                        context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate,
                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid).ConfigureAwait(false)));

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has inserted the entity into the cache db serially.");
                    }
                }
            }
            else
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} does not allow entity storage in the cache.");
                }
            }
        }

        private static DictionaryNoBoxing<int> ParseToReducedInternedPayload(Context context)
        {
            var reducedInternedPayload = new DictionaryNoBoxing<int>();
            reducedInternedPayload.Add(-1, context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate);

            foreach (var entityAnalysisModelInlineScriptPropertyAttribute in context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts.SelectMany(entityAnalysisModelInlineScript => entityAnalysisModelInlineScript.EntityAnalysisModelInlineScriptPropertyAttributes))
            {
                if (entityAnalysisModelInlineScriptPropertyAttribute.Value.CacheIndexId is not {} cacheId)
                {
                    continue;
                }

                if (context.EntityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(entityAnalysisModelInlineScriptPropertyAttribute.Key, out var value))
                {
                    reducedInternedPayload.Add(cacheId, value);
                }
            }

            foreach (var entityAnalysisModelRequestXPath in context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Where(w => w.Cache))
            {
                if (context.EntityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(entityAnalysisModelRequestXPath.Name, out var value))
                {
                    reducedInternedPayload.Add(entityAnalysisModelRequestXPath.CacheIndexId, value);
                }
            }

            return reducedInternedPayload;
        }
    }
}
