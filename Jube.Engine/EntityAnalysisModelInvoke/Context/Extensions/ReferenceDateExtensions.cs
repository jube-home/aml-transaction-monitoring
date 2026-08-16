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
    using System;
    using System.Threading.Tasks;
    using Cache;
    using Exceptions;
    using TaskCancellation.TaskHelper;

    public static class ReferenceDateExtensions
    {
        public static async Task<Context> CheckIntegrityAndUpsertAsync(this Context context, CacheService cacheService)
        {
            var referenceDate = await cacheService.CacheReferenceDateRepository.GetReferenceDateAsync(context.EntityAnalysisModel.Instance.Id, context.EntityAnalysisModel.Instance.Guid).ConfigureAwait(false);

            if (context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate > DateTime.UtcNow)
            {
                throw new ReferenceDateInFutureException();
            }

            if (context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate < referenceDate)
            {
                return context;
            }

            context.PendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.UpsertReferenceDateAsync,
                async () => await
                    cacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                        context.EntityAnalysisModel.Instance.Guid, context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate)));

            return context;
        }
    }
}
