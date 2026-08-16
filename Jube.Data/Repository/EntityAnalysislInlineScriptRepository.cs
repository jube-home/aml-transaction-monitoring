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

    public class EntityAnalysisInlineScriptRepository(DbContext dbContext)
    {
        public async Task<IEnumerable<EntityAnalysisInlineScript>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisInlineScript.ToListAsync(token).ConfigureAwait(false);
        }

        public Task<EntityAnalysisInlineScript> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisInlineScript.FirstOrDefaultAsync(w => w.Id == id, token);
        }

        public async Task<EntityAnalysisInlineScript> InsertAsync(EntityAnalysisInlineScript model, CancellationToken token = default)
        {
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public Task UpdateCompileStatusAsync(int id, bool compiled, string compileError, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisInlineScript
                .Where(d => d.Id == id)
                .Set(s => s.Compiled, Convert.ToByte(compiled ? 1 : 0))
                .Set(s => s.CompileError, compileError)
                .UpdateAsync(token);
        }
    }
}
