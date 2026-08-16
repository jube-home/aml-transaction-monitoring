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
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;

    public class GetNextExhaustiveSearchInstanceQuery(DbContext dbContext)
    {
        public async Task<Dto> ExecuteAsync(CancellationToken token = default)
        {
            await dbContext.BeginTransactionAsync(IsolationLevel.Serializable, token).ConfigureAwait(false);
            try
            {
                var query = await dbContext.ExhaustiveSearchInstance
                    .Where(w => w.StatusId == 0
                                && (w.Deleted == 0 || w.Deleted == null))
                    .OrderBy(o => o.Id)
                    .Select(s =>
                        new Dto
                        {
                            Id = s.Id,
                            EntityAnalysisModelId = s.EntityAnalysisModelId.Value,
                            TenantRegistryId = s.EntityAnalysisModel.TenantRegistryId.Value,
                            FilterJson = s.FilterJson,
                            FilterTokens = s.FilterTokens,
                            FilterSql = s.FilterSql,
                            Anomaly = s.Anomaly == 1,
                            AnomalyProbability = s.AnomalyProbability,
                            Filter = s.Filter == 1
                        }
                    )
                    .FirstOrDefaultAsync(token).ConfigureAwait(false);

                if (query != null)
                {
                    await dbContext.ExhaustiveSearchInstance
                        .Where(d =>
                            d.Id ==
                            query.Id)
                        .Set(s => s.StatusId, Convert.ToByte(1))
                        .Set(s => s.UpdatedDate, DateTime.UtcNow)
                        .UpdateAsync(token).ConfigureAwait(false);
                }

                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);

                return query;
            }
            catch
            {
                await dbContext.RollbackTransactionAsync(token).ConfigureAwait(false);
                throw;
            }
        }

        public class Dto
        {
            public int Id { get; set; }
            public int EntityAnalysisModelId { get; set; }
            public int TenantRegistryId { get; set; }
            public string FilterJson { get; set; }
            public string FilterTokens { get; set; }
            public string FilterSql { get; set; }
            public bool Anomaly { get; set; }
            public double AnomalyProbability { get; set; }
            public bool Filter { get; set; }
        }
    }
}
