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
using System.IO;
using System.Threading.Tasks;
using Jube.Data.Cache.Dto;
using Jube.Data.Cache.Interfaces;
using Jube.Data.Cache.Redis.MessagePack;
using log4net;
using MessagePack;
using StackExchange.Redis;

namespace Jube.Data.Cache.Redis;

public class CacheSanctionRepository(
    IDatabaseAsync redisDatabase,
    ILog log,
    CommandFlags commandFlag = CommandFlags.FireAndForget) : ICacheSanctionRepository
{
    public async Task<CacheSanctionDto> GetByMultiPartStringDistanceThresholdAsync(int tenantRegistryId,
        Guid entityAnalysisModelGuid, string multiPartString,
        int distanceThreshold)
    {
        try
        {
            var redisKey = $"Sanction:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
            var redisHSetKey = $"{multiPartString}:{distanceThreshold}";

            var hashValue = await redisDatabase.HashGetAsync(redisKey, redisHSetKey).ConfigureAwait(false);

            if (!hashValue.HasValue) return null;

            var sanction = MessagePackSerializer
                .Deserialize<Sanction>(hashValue,
                    MessagePackSerializerOptionsHelper.StandardMessagePackSerializerWithCompressionOptions(false));

            return new CacheSanctionDto
            {
                CreatedDate = sanction.CreatedDate,
                Value = sanction.Value
            };
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }

        return null;
    }

    public async Task InsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string multiPartString,
        int distanceThreshold,
        double? value)
    {
        try
        {
            var redisKey = $"Sanction:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
            var redisHSetKey = $"{multiPartString}:{distanceThreshold}";

            var sanction = new Sanction
            {
                Value = value,
                CreatedDate = DateTime.Now
            };

            var ms = new MemoryStream();
            await MessagePackSerializer.SerializeAsync(ms, sanction,
                MessagePackSerializerOptionsHelper.StandardMessagePackSerializerWithCompressionOptions(false)).ConfigureAwait(false);
            await redisDatabase.HashSetAsync(redisKey, redisHSetKey, ms.ToArray(),
                When.Always, commandFlag).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }

    public async Task UpdateAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string multiPartString,
        int distanceThreshold,
        double? value)
    {
        try
        {
            await InsertAsync(tenantRegistryId, entityAnalysisModelGuid, multiPartString, distanceThreshold, value).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }
}