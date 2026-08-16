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

    public class ExhaustiveSearchInstanceTrialInstanceVariableRepository(DbContext dbContext)
    {
        public async Task<ExhaustiveSearchInstanceTrialInstanceVariable> InsertAsync(ExhaustiveSearchInstanceTrialInstanceVariable model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        public Task DeleteAllByExhaustiveSearchInstanceTrialInstanceIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstanceTrialInstanceVariable
                .Where(d => d.Id == id && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        public async Task UpdateAsRemovedByExhaustiveSearchInstanceVariableIdAsync(int exhaustiveSearchInstanceVariableId,
            int exhaustiveSearchInstanceTrialInstanceId, CancellationToken token = default)
        {
            var records = await dbContext.ExhaustiveSearchInstanceTrialInstanceVariable
                .Where(u =>
                    u.ExhaustiveSearchInstanceVariableId == exhaustiveSearchInstanceVariableId
                    && u.ExhaustiveSearchInstanceTrialInstanceId == exhaustiveSearchInstanceTrialInstanceId)
                .Set(s => s.Removed, 1)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public async Task<IEnumerable<ExhaustiveSearchInstanceTrialInstanceVariable>>
            GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                int exhaustiveSearchInstanceTrialInstanceId, CancellationToken token = default)
        {
            return await dbContext.ExhaustiveSearchInstanceTrialInstanceVariable.Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstanceId == exhaustiveSearchInstanceTrialInstanceId
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstanceTrialInstanceVariable
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
