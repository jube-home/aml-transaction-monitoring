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

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.TtlCounterAdministration
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cache.Redis;
    using Cache.Redis.Interfaces;
    using Cache.Redis.Models;
    using EntityAnalysisModel;
    using EntityAnalysisModel.Models.Models;

    public class TtlCounterAdministrationCacheService(EntityAnalysisModel entityAnalysisModel)
    {
        public async Task<DateTime?> CacheServiceGetReferenceDateAsync()
        {
            var referenceDate = await entityAnalysisModel.Services.CacheService.CacheReferenceDateRepository.GetReferenceDatePreferReplicaAsync(entityAnalysisModel.Instance.TenantRegistryId, entityAnalysisModel.Instance.Guid).ConfigureAwait(false);
            return referenceDate;
        }

        public async Task<double> CacheServiceDecrementTtlCounterAsync(EntityAnalysisModelTtlCounter ttlCounter,
            string key, double decrement)
        {
            var value = 0d;
            try
            {
                value = await entityAnalysisModel.Services.CacheService.CacheTtlCounterRepository.DecrementTtlCounterCacheAsync(entityAnalysisModel.Instance.TenantRegistryId, entityAnalysisModel.Instance.Guid, ttlCounter.Guid,
                    ttlCounter.TtlCounterDataName, key, decrement).ConfigureAwait(false);

                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"TTL Counter Administration: has finished aggregation for {ttlCounter.Name} and Data Name {ttlCounter.TtlCounterDataName} and has also decremented value {value} by {decrement} in the TTL counter cache.  Will now use the same date criteria to delete the records from the entries table.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"CacheServiceGetReferenceDateAsync: has produced an error {ex}");
            }
            return value;
        }

        public async Task<List<ExpiredTtlCounterEntry>>
            GetAllExpiredByTtlCounterAsync(
                CacheTtlCounterEntryRepository cacheTtlCounterEntryRepository,
                EntityAnalysisModelTtlCounter ttlCounter,
                DateTime adjustedTtlCounterDate,
                int limit)
        {
            List<ExpiredTtlCounterEntry> values = null;
            try
            {
                values = await cacheTtlCounterEntryRepository.GetAllExpiredByTtlCounterPreferReplicaAsync(
                    entityAnalysisModel.Instance.TenantRegistryId, entityAnalysisModel.Instance.Guid,
                    ttlCounter.Guid, ttlCounter.TtlCounterDataName, adjustedTtlCounterDate, limit).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"GetAllByTtlCounterAsync: has produced an error {ex}");
            }

            return values;
        }

        public async Task CacheServiceDeleteTtlCounterEntryAsync(
            ICacheTtlCounterEntryRepository cacheTtlCounterEntryRepository,
            EntityAnalysisModelTtlCounter ttlCounter,
            string dataValue,
            DateTime referenceDate)
        {
            try
            {
                await cacheTtlCounterEntryRepository.DeleteAsync(entityAnalysisModel.Instance.TenantRegistryId, entityAnalysisModel.Instance.Guid,
                    ttlCounter.Guid, ttlCounter.TtlCounterDataName, dataValue, referenceDate).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"CacheServiceDeleteTtlCounterEntryAsync: has produced an error {ex}");
            }
        }

        public DateTime GetAdjustedTtlCounterDate(EntityAnalysisModelTtlCounter ttlCounter, DateTime referenceDate)
        {
            try
            {
                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"TTL Counter Administration: has found a reference date of {referenceDate} for {ttlCounter.Name} and Data Name {ttlCounter.TtlCounterDataName}.");
                }

                return ttlCounter.TtlCounterInterval switch
                {
                    "d" => referenceDate.AddDays(ttlCounter.TtlCounterValue * -1),
                    "h" => referenceDate.AddHours(ttlCounter.TtlCounterValue * -1),
                    "n" => referenceDate.AddMinutes(ttlCounter.TtlCounterValue * -1),
                    "s" => referenceDate.AddSeconds(ttlCounter.TtlCounterValue * -1),
                    "m" => referenceDate.AddMonths(ttlCounter.TtlCounterValue * -1),
                    "y" => referenceDate.AddYears(ttlCounter.TtlCounterValue * -1),
                    _ => referenceDate.AddDays(ttlCounter.TtlCounterValue * -1)
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"TTL Counter Administration: has found a reference date of {referenceDate}" +
                        $" for {ttlCounter.Name} and Data Name {ttlCounter.TtlCounterDataName}." +
                        $" Error of {ex} returning reference date as default.");
                }

                return referenceDate;
            }
        }
    }
}
