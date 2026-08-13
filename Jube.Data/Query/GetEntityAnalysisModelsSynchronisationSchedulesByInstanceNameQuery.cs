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

namespace Jube.Data.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;

    public class GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery(DbContext dbContext)
    {
        public async Task<List<Dto>>
            ExecuteAsync(string instance, CancellationToken token = default)
        {
            var tenants = await (from e
                        in dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry
                    from t
                        in dbContext.TenantRegistry.RightJoin
                        (t => t.Id == e.TenantRegistryId &&
                              e.Instance == instance)
                    select new
                    {
                        t.Id,
                        SynchronisedDate = e.SynchronisedDate ?? default(DateTime)
                    })
                .ToDictionaryAsync(s => s.Id, s => s.SynchronisedDate, token).ConfigureAwait(false);

            return await (from y in dbContext.EntityAnalysisModelSynchronisationSchedule
                    join m in from t in dbContext.EntityAnalysisModelSynchronisationSchedule
                        group t by t.TenantRegistryId
                        into g
                        select new
                        {
                            TenantRegistryId = g.Key,
                            EntityAnalysisModelSyncronisationScheduleId =
                                (from t2 in g select t2.Id).Max()
                        } on y.Id equals m
                            .EntityAnalysisModelSyncronisationScheduleId
                    select
                        new Dto
                        {
                            SynchronisationPending = y.ScheduleDate > tenants[y.TenantRegistryId.Value]
                                                     && DateTime.UtcNow > y.ScheduleDate,
                            TenantRegistryId = y.TenantRegistryId.Value
                        }
                ).ToListAsync(token).ConfigureAwait(false);
        }

        public class Dto
        {
            public bool SynchronisationPending { get; set; }
            public int TenantRegistryId { get; set; }
        }
    }
}
