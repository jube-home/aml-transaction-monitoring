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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Poco;

    public class EntityAnalysisModelSearchKeyCalculationInstanceRepository(DbContext dbContext)
    {
        public async Task<EntityAnalysisModelSearchKeyCalculationInstance> InsertAsync(
            EntityAnalysisModelSearchKeyCalculationInstance model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        public Task UpdateDistinctValuesCountAsync(int id,
            int distinctValuesCount, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelSearchKeyCalculationInstance
                .Where(d => d.Id == id)
                .Set(s => s.DistinctValuesCount, distinctValuesCount)
                .Set(s => s.DistinctValuesUpdatedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        public Task UpdateExpiredSearchKeyCacheCountAsync(int id,
            int expiredSearchKeyCacheCount, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelSearchKeyCalculationInstance
                .Where(d => d.Id == id)
                .Set(s => s.ExpiredSearchKeyCacheCount, expiredSearchKeyCacheCount)
                .Set(s => s.ExpiredSearchKeyCacheDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        public Task UpdateDistinctValuesProcessedValuesCountAsync(int id,
            int distinctValuesProcessedValuesCount, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelSearchKeyCalculationInstance
                .Where(d => d.Id == id)
                .Set(s => s.DistinctValuesProcessedValuesCount, distinctValuesProcessedValuesCount)
                .Set(s => s.DistinctValuesProcessedValuesUpdatedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        public Task UpdateCompletedAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelSearchKeyCalculationInstance
                .Where(d => d.Id == id)
                .Set(s => s.Completed, (byte)1)
                .Set(s => s.CompletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
