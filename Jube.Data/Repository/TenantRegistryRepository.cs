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
    using AutoMapper;
    using Context;
    using LinqToDB;
    using Microsoft.Extensions.Logging.Abstractions;
    using Poco;

    public class TenantRegistryRepository(DbContext dbContext, string userName)
    {
        public Task<TenantRegistry> GetByNameAsync(string name, CancellationToken token = default)
        {
            return dbContext.TenantRegistry
                .FirstOrDefaultAsync(f =>
                    (f.Deleted == 0 || f.Deleted == null)
                    && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<TenantRegistry>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.TenantRegistry.Where(w => w.Deleted == 0 || w.Deleted == null).ToListAsync(token);
        }

        public Task<TenantRegistry> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.TenantRegistry.FirstOrDefaultAsync(w => w.Id == id
                                                                     && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<IEnumerable<TenantRegistry>> GetByFilterAsync(string filter, CancellationToken token = default)
        {
            return await dbContext.TenantRegistry.Where(w => w.Name.ToLower().Contains(filter)
                                                             && (w.Deleted == 0 || w.Deleted == null)).ToListAsync(token);
        }

        public async Task<TenantRegistry> InsertAsync(TenantRegistry model, CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.Version = 1;
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<TenantRegistry> UpdateAsync(TenantRegistry model, CancellationToken token = default)
        {
            var existing = await dbContext.TenantRegistry
                .FirstOrDefaultAsync(w => w.Id
                                          == model.Id
                                          && (w.Deleted == 0 || w.Deleted == null)
                                          && (w.Locked == 0 || w.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = existing.Version + 1;
            model.CreatedUser = userName;

            await dbContext.UpdateAsync(model, token: token);

            var mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<TenantRegistry, TenantRegistryVersion>();
            }, NullLoggerFactory.Instance));

            var audit = mapper.Map<TenantRegistryVersion>(existing);
            audit.TenantRegistryId = existing.Id;

            await dbContext.InsertAsync(audit, token: token);

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.TenantRegistry
                .Where(d => d.Id == id
                            && (d.Locked == 0 || d.Locked == null)
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
    }
}
