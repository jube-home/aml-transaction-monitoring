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

    public class RoleRegistryPermissionRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public RoleRegistryPermissionRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public IEnumerable<RoleRegistryPermission> Get()
        {
            return dbContext.RoleRegistryPermission.Where(w => w.RoleRegistry.TenantRegistryId == tenantRegistryId
                                                               && (w.Deleted == 0 || w.Deleted == null));
        }

        public RoleRegistryPermission GetById(int id)
        {
            return dbContext.RoleRegistryPermission.FirstOrDefault(w => w.Id == id
                                                                        && w.RoleRegistry.TenantRegistryId ==
                                                                        tenantRegistryId
                                                                        && (w.Deleted == 0 || w.Deleted == null));
        }

        public RoleRegistryPermission Insert(RoleRegistryPermission model)
        {
            model.CreatedUser = userName;
            model.Version = 1;
            model.CreatedDate = DateTime.Now;
            model.Guid = Guid.NewGuid();
            model.Id = dbContext.InsertWithInt32Identity(model);
            return model;
        }

        public RoleRegistryPermission Update(RoleRegistryPermission model)
        {
            var existing = dbContext.RoleRegistryPermission
                .FirstOrDefault(u => u.RoleRegistry.TenantRegistryId == tenantRegistryId
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

            dbContext.Update(model);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RoleRegistryPermission, RoleRegistryPermissionVersion>();
            });
            var mapper = new Mapper(config);

            var audit = mapper.Map<RoleRegistryPermissionVersion>(existing);
            audit.RoleRegistryPermissionId = existing.Id;

            dbContext.Insert(audit);

            return model;
        }

        public void Delete(int id)
        {
            var records = dbContext.RoleRegistryPermission
                .Where(d =>
                    d.RoleRegistry.TenantRegistryId == tenantRegistryId
                    && d.Id == id
                    && (d.Locked == 0 || d.Locked == null)
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Set(s => s.DeletedUser, userName)
                .Update();

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }
    }
}
