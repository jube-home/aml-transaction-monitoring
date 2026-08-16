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
    using Interface;
    using LinqToDB;
    using Poco;

    public class ExhaustiveSearchInstanceVariableRepository(DbContext dbContext) : IGenericRepository
    {
        public Task<int> InsertAsync(object arg, CancellationToken token = default)
        {
            return dbContext.InsertWithInt32IdentityAsync((ExhaustiveSearchInstanceVariable)arg, token: token);
        }

        public Task UpdateCorrelationAsync(int id, double correlation, int rank, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstanceVariable
                .Where(d => d.Id == id)
                .Set(s => s.Correlation, correlation)
                .Set(s => s.CorrelationAbsRank, rank)
                .UpdateAsync(token);
        }

        public async Task<IEnumerable<ExhaustiveSearchInstanceVariable>> GetByExhaustiveSearchInstanceIdOrderByIdAsync(
            int exhaustiveSearchInstanceId, CancellationToken token = default)
        {
            return await dbContext.ExhaustiveSearchInstanceVariable.Where(w =>
                    w.ExhaustiveSearchInstanceId == exhaustiveSearchInstanceId)
                .OrderBy(o => o.Id).ToListAsync(token).ConfigureAwait(false);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstanceVariable
                .Where(d =>
                    d.ExhaustiveSearchInstance.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
