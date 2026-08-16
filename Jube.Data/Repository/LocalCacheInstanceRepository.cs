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

    public class LocalCacheInstanceRepository(DbContext dbContext)
    {
        public async Task<LocalCacheInstance> InsertAsync(LocalCacheInstance model, CancellationToken token = default)
        {
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public Task UpdateCountAndBytesAsync(long id, long count, long bytes, long heapSizeBytes, long totalCommittedBytes, CancellationToken token = default)
        {
            return dbContext.LocalCacheInstance
                .Where(d => d.Id == id)
                .Set(s => s.Count, count)
                .Set(s => s.Bytes, bytes)
                .Set(s => s.HeapSizeBytes, heapSizeBytes)
                .Set(s => s.TotalCommittedBytes, totalCommittedBytes)
                .Set(s => s.UpdatedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        public Task StartFillAsync(long id, CancellationToken token = default)
        {
            return dbContext.LocalCacheInstance
                .Where(d => d.Id == id)
                .Set(s => s.FillStartedDate, DateTime.UtcNow)
                .Set(s => s.Fill, (byte)1)
                .UpdateAsync(token);
        }

        public Task FinishFillAsync(long id, int count, long bytes, long heapSizeBytes, long totalCommittedBytes, CancellationToken token = default)
        {
            return dbContext.LocalCacheInstance
                .Where(d => d.Id == id)
                .Set(s => s.FillEndedDate, DateTime.UtcNow)
                .Set(s => s.Filled, (byte)1)
                .Set(s => s.FillBytes, s => bytes)
                .Set(s => s.FillCount, s => count)
                .Set(s => s.HeapSizeBytes, heapSizeBytes)
                .Set(s => s.TotalCommittedBytes, totalCommittedBytes)
                .Set(s => s.UpdatedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        public Task FinishFillAsync(long id, long fillBytes, long fillCount, long bytes, long count, long heapSizeBytes, long totalCommittedBytes, CancellationToken token = default)
        {
            return dbContext.LocalCacheInstance
                .Where(d => d.Id == id)
                .Set(s => s.FillEndedDate, DateTime.UtcNow)
                .Set(s => s.Filled, (byte)0)
                .Set(s => s.FillBytes, s => fillBytes)
                .Set(s => s.FillCount, s => fillCount)
                .Set(s => s.Count, count)
                .Set(s => s.Bytes, bytes)
                .Set(s => s.HeapSizeBytes, heapSizeBytes)
                .Set(s => s.TotalCommittedBytes, totalCommittedBytes)
                .Set(s => s.UpdatedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        public Task UpdateFillAsync(long id, long fillBytes, long fillCount, int count, long bytes, long heapSizeBytes, long totalCommittedBytes, CancellationToken token = default)
        {
            return dbContext.LocalCacheInstance
                .Where(d => d.Id == id)
                .Set(s => s.FillBytes, s => fillBytes)
                .Set(s => s.FillCount, s => fillCount)
                .Set(s => s.UpdatedDate, DateTime.UtcNow)
                .Set(s => s.Count, count)
                .Set(s => s.Bytes, bytes)
                .Set(s => s.HeapSizeBytes, heapSizeBytes)
                .Set(s => s.TotalCommittedBytes, totalCommittedBytes)
                .Set(s => s.UpdatedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
