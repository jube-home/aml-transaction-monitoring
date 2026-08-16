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

    public class ExhaustiveSearchInstancePromotedTrialInstanceRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;

        public ExhaustiveSearchInstancePromotedTrialInstanceRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public ExhaustiveSearchInstancePromotedTrialInstanceRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public ExhaustiveSearchInstancePromotedTrialInstanceRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public async Task UpdateActiveAsync(int id, bool active, CancellationToken token = default)
        {
            var records = await dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(d =>
                    (d.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                        .EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id)
                .Set(s => s.Active, (byte)(active ? 1 : 0))
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public async Task<ExhaustiveSearchInstancePromotedTrialInstance> InsertAsync(ExhaustiveSearchInstancePromotedTrialInstance model, CancellationToken token = default)
        {
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        public async Task<IEnumerable<ExhaustiveSearchInstancePromotedTrialInstance>>
            GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                int exhaustiveSearchInstanceTrialInstanceId, CancellationToken token = default)
        {
            return await dbContext.ExhaustiveSearchInstancePromotedTrialInstance.Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstanceId == exhaustiveSearchInstanceTrialInstanceId)
                .OrderBy(o => o.Id).ToListAsync(token).ConfigureAwait(false);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(d =>
                    d.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.EntityAnalysisModel
                        .TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
