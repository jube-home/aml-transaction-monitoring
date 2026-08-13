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

namespace Jube.Migrations.Branches.GitHubIssueBranch139
{
    using System.Linq;
    using Cache;
    using Data.Context;
    using DynamicEnvironment;
    using FluentMigrator;
    using log4net;

    [Migration(20260706133100)]
    public class RenameLatestCountInRedisToReferenceDateLatest(
        CacheService cacheService,
        DynamicEnvironment dynamicEnvironment,
        ILog log) : Migration
    {
        public override void Up()
        {
            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), log);
            var entityAnalysisModels = dbContext.EntityAnalysisModel.Select(s => new
            {
                s.TenantRegistryId,
                s.Guid
            });

            foreach (var entityAnalysisModel in entityAnalysisModels)
            {
                var before = $"LatestCount:{entityAnalysisModel.TenantRegistryId}:{entityAnalysisModel.Guid:N}";
                var after = $"ReferenceDateLatest:{entityAnalysisModel.TenantRegistryId}:{entityAnalysisModel.Guid:N}";

                if (cacheService.ResilientRedisResilientRedisDatabase.KeyExists(before))
                {
                    cacheService.ResilientRedisResilientRedisDatabase.KeyRename(before, after);
                }
            }
        }
        public override void Down()
        {

        }
    }
}
