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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jube.Data.Cache.Postgres;

namespace Jube.Data.Cache.Interfaces;

public interface ICacheTtlCounterEntryRepository
{
    Task<List<CacheTtlCounterEntryRepository.ExpiredTtlCounterEntryDto>> GetExpiredTtlCounterCacheCountsAsync(
        int tenantRegistryId,
        int entityAnalysisModelId,
        int entityAnalysisModelTtlCounterId,
        string dataName, DateTime referenceDate);

    Task<int> GetAsync(int tenantRegistryId,
        int entityAnalysisModelId, int entityAnalysisModelTtlCounterId,
        string dataName, string dataValue,
        DateTime referenceDateFrom, DateTime referenceDateTo);

    Task UpsertAsync(int tenantRegistryId,
        int entityAnalysisModelId, string dataName, string dataValue,
        int entityAnalysisModelTtlCounterId,
        DateTime referenceDate, int increment);

    Task DeleteAsync(int tenantRegistryId,
        int entityAnalysisModelId, int entityAnalysisModelTtlCounterId,
        string dataName, string dataValue,
        DateTime referenceDate);
}