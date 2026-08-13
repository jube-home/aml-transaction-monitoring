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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Poco;

    public class EntityAnalysisModelSynchronisationScheduleRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelSynchronisationScheduleRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelSynchronisationScheduleRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<EntityAnalysisModelSynchronisationSchedule>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelSynchronisationSchedule
                .Where(w => w.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue).ToListAsync(token);
        }

        public Task<EntityAnalysisModelSynchronisationSchedule> GetCurrentAsync(CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelSynchronisationSchedule.Where(w
                    => w.TenantRegistryId == tenantRegistryId)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync(token);
        }

        public async Task<EntityAnalysisModelSynchronisationSchedule> InsertAsync(EntityAnalysisModelSynchronisationSchedule model, CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.TenantRegistryId = tenantRegistryId;
            model.CreatedDate = DateTime.UtcNow;

            if (model.ScheduleDate == default(DateTime) || model.ScheduleDate == null)
            {
                model.ScheduleDate = model.CreatedDate;
            }

            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }
    }
}
