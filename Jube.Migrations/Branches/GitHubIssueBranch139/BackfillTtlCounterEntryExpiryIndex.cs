/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.Migrations.Branches.GitHubIssueBranch139
{
    using System;
    using System.Linq;
    using Cache;
    using FluentMigrator;
    using log4net;

    [Migration(20263107220000)]
    public class BackfillTtlCounterEntryExpiryIndex(
        CacheService cacheService,
        ILog log) : Migration
    {
        public override void Up()
        {
            var redisServers = cacheService.ConnectionMultiplexer?.GetEndPoints()
                .Select(redisEndpoint => cacheService.ConnectionMultiplexer.GetServer(redisEndpoint)).ToList();

            if (redisServers == null)
            {
                return;
            }

            foreach (var key in redisServers.SelectMany(redisServer => redisServer.Keys(pattern: "TtlCounterEntry:*")))
            {
                try
                {
                    BackfillKey(key.ToString());
                }
                catch (Exception ex)
                {
                    log.Error($"BackfillTtlCounterEntryExpiryIndex: could not backfill key {key} for exception {ex}.");
                }
            }
        }

        private void BackfillKey(string redisKey)
        {
            var parts = redisKey.Split(':', 6);
            if (parts.Length != 6 || parts[0] != "TtlCounterEntry")
            {
                return;
            }

            var tenantRegistryId = parts[1];
            var entityAnalysisModelGuid = parts[2];
            var entityAnalysisModelTtlCounterGuid = parts[3];
            var dataName = parts[4];
            var dataValue = parts[5];

            var redisKeyExpiryIndex =
                $"TtlCounterEntryExpiry:{tenantRegistryId}:{entityAnalysisModelGuid}:{entityAnalysisModelTtlCounterGuid}:{dataName}";

            foreach (var hashEntry in cacheService.ResilientRedisResilientRedisDatabase.HashScan(redisKey))
            {
                if (!Int64.TryParse(hashEntry.Name, out var timestamp))
                {
                    continue;
                }

                var member = $"{timestamp}:{dataValue}";

#pragma warning disable VSTHRD002
                cacheService.ResilientRedisResilientRedisDatabase
                    .SortedSetAddAsync(redisKeyExpiryIndex, member, timestamp)
                    .GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
            }
        }

        public override void Down()
        {
        }
    }
}
