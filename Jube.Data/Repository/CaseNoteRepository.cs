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

    public class CaseNoteRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public CaseNoteRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseNoteRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public CaseNoteRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<CaseEvent>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.CaseEvent.Where(w =>
                w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                !tenantRegistryId.HasValue).ToListAsync(token);
        }

        public async Task<IEnumerable<CaseNote>> GetByCaseKeyValueAsync(string key, string value, CancellationToken token = default)
        {
            return await dbContext.CaseNote.Where(w
                    => (w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                        !tenantRegistryId.HasValue)
                       && (w.Case.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                           w.Case.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                       && w.CaseKey == key && w.CaseKeyValue == value)
                .OrderByDescending(o => o.Id).ToListAsync(token);
        }

        public async Task<CaseNote> InsertAsync(CaseNote model, CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }
    }
}
