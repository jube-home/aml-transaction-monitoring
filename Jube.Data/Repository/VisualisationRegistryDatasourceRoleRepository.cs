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

    public class VisualisationRegistryDatasourceRoleRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public VisualisationRegistryDatasourceRoleRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public VisualisationRegistryDatasourceRoleRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public async Task<IEnumerable<VisualisationRegistryDatasourceRole>> GetAllDescAsync(CancellationToken token = default)
        {
            return await dbContext.VisualisationRegistryDatasourceRole
                .Where(w => w.VisualisationRegistryDatasource.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public Task<List<VisualisationRegistryDatasourceRole>> GetByVisualisationRegistryDatasourceGuidAsync(Guid visualisationRegistryDatasourceGuid, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistryDatasourceRole.Where(w =>
                w.VisualisationRegistryDatasource.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                && w.VisualisationRegistryDatasourceGuid == visualisationRegistryDatasourceGuid
                && (w.Deleted == 0 || w.Deleted == null)).ToListAsync(token);
        }

        public async Task<VisualisationRegistryDatasourceRole> InsertAsync(VisualisationRegistryDatasourceRole model, CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.VisualisationRegistryDatasourceRole
                .Where(d => d.VisualisationRegistryDatasource.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                            && d.Id == id
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId, CancellationToken token = default)
        {
            return dbContext.VisualisationRegistryDatasourceRole
                .Where(d =>
                    d.VisualisationRegistryDatasource.VisualisationRegistry.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
