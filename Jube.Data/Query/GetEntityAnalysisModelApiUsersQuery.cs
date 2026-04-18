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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;

    public class GetEntityAnalysisModelApiUsersQuery(DbContext dbContext)
    {
        public async Task<IEnumerable<Dto>> ExecuteAsync(int id, CancellationToken token = default)
        {
            return await dbContext.UserRegistry
                .Where(u => u.Active == 1)// Added
                .SelectMany(u => dbContext.UserRegistryApiKey
                        .Where(k => k.UserRegistryId == u.Id
                                    && (k.Deleted == 0 || k.Deleted == null)),
                    (u, k) => new
                    {
                        u,
                        k
                    })
                .SelectMany(c => dbContext.RoleRegistry
                        .Where(rr => rr.Guid == c.u.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null)),
                    (c, rr) => new
                    {
                        c.u,
                        rr
                    })
                .SelectMany(c => dbContext.EntityAnalysisModelRole
                        .Where(r => r.RoleRegistryGuid == c.rr.Guid && (r.Deleted == 0 || r.Deleted == null)),
                    (c, r) => new
                    {
                        c.u,
                        r
                    })
                .SelectMany(c => dbContext.EntityAnalysisModel
                    .Where(w => w.Guid == c.r.EntityAnalysisModelGuid
                                && w.Active == 1
                                && w.Id == id
                                && (w.Deleted == 0 || w.Deleted == null))
                    .Select(s => new Dto
                    {
                        Username = c.u.Name
                    })
                ).ToListAsync(token);
        }

        public class Dto
        {
            public string Username { get; set; }
        }
    }
}
