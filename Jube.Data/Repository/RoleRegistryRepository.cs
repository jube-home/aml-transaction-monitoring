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
    using AutoMapper;
    using Context;
    using LinqToDB;
    using Poco;

    public class RoleRegistryRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public RoleRegistryRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public RoleRegistryRepository(DbContext dbContext, TenantRegistry tenantRegistry)
        {
            this.dbContext = dbContext;
            tenantRegistryId = tenantRegistry.Id;
        }

        public IEnumerable<RoleRegistry> Get()
        {
            return dbContext.RoleRegistry.Where(w => w.TenantRegistryId == tenantRegistryId
                                                     && (w.Deleted == 0 || w.Deleted == null));
        }

        public RoleRegistry GetById(int id)
        {
            return dbContext.RoleRegistry.FirstOrDefault(w => w.Id == id
                                                              && w.TenantRegistryId == tenantRegistryId
                                                              && (w.Deleted == 0 || w.Deleted == null));
        }

        public RoleRegistry Insert(RoleRegistry model)
        {
            model.CreatedUser = userName;
            model.TenantRegistryId = tenantRegistryId;
            model.Version = 1;
            model.CreatedDate = DateTime.Now;
            model.Guid = Guid.NewGuid();
            model.Id = dbContext.InsertWithInt32Identity(model);
            return model;
        }

        public RoleRegistry Update(RoleRegistry model)
        {
            var existing = dbContext.RoleRegistry
                .FirstOrDefault(u => u.TenantRegistryId == tenantRegistryId
                                     && u.Id == model.Id
                                     && (u.Deleted == 0 || u.Deleted == null)
                                     && (u.Locked == 0 || u.Locked == null));

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.Now;
            model.TenantRegistryId = tenantRegistryId;

            dbContext.Update(model);

            var config = new MapperConfiguration(cfg => { cfg.CreateMap<RoleRegistry, RoleRegistryVersion>(); });
            var mapper = new Mapper(config);

            var audit = mapper.Map<RoleRegistryVersion>(existing);
            audit.RoleRegistryId = existing.Id;

            dbContext.Insert(audit);

            return model;
        }

        public void Delete(int id)
        {
            var records = dbContext.RoleRegistry
                .Where(d => d.Id == id
                            && d.TenantRegistryId == tenantRegistryId
                            && (d.Deleted == 0 || d.Deleted == null)
                            && (d.Locked == 0 || d.Locked == null)).Delete();

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }
    }
}
