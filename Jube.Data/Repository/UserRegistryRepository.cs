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

    public class UserRegistryRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public UserRegistryRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public UserRegistryRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public UserRegistryRepository(DbContext dbContext, RoleRegistry roleRegistry)
        {
            this.dbContext = dbContext;
            tenantRegistryId = roleRegistry.TenantRegistryId;
        }

        public IEnumerable<UserRegistry> Get()
        {
            return dbContext.UserRegistry
                .Where(w => w.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue);
        }

        public IEnumerable<UserRegistry> GetByRoleRegistryId(int roleRegistryId)
        {
            return dbContext.UserRegistry
                .Where(w => (w.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && w.RoleRegistryId == roleRegistryId && (w.Deleted == 0 || w.Deleted == null));
        }

        public UserRegistry GetById(int id)
        {
            return dbContext.UserRegistry.FirstOrDefault(w =>
                (w.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Id == id && (w.Deleted == 0 || w.Deleted == null));
        }

        public UserRegistry GetByUserName(string userNameInTenant)
        {
            return dbContext.UserRegistry.FirstOrDefault(w =>
                (w.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Name == userNameInTenant && (w.Deleted == 0 || w.Deleted == null));
        }

        public UserRegistry Insert(UserRegistry model)
        {
            if (!tenantRegistryId.HasValue)
            {
                throw new KeyNotFoundException();
            }

            model.CreatedUser = userName;
            model.Version = 1;
            model.CreatedDate = DateTime.Now;
            model.Guid = Guid.NewGuid();
            model.Id = dbContext.InsertWithInt32Identity(model);

            var userInTenant = new UserInTenant
            {
                User = model.Name,
                TenantRegistryId = tenantRegistryId.Value,
                SwitchedUser = userName,
                SwitchedDate = DateTime.Now
            };

            dbContext.Insert(userInTenant);

            return model;
        }

        public UserRegistry Update(UserRegistry model)
        {
            var existing = dbContext.UserRegistry
                .FirstOrDefault(w => w.Id
                                     == model.Id
                                     && (w.Deleted == 0 || w.Deleted == null));

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.Now;

            dbContext.Update(model);

            var config = new MapperConfiguration(cfg => { cfg.CreateMap<UserRegistry, UserRegistryVersion>(); });
            var mapper = new Mapper(config);

            var audit = mapper.Map<UserRegistryVersion>(existing);
            audit.UserRegistryId = existing.Id;

            dbContext.Insert(audit);

            return model;
        }

        public void ResetFailedPasswordCount(int id)
        {
            var records = dbContext.UserRegistry
                .Where(d => (d.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && d.Id == id
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.FailedPasswordCount, 0)
                .Update();

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public void SetPassword(int id, string password, DateTime expiryDate)
        {
            var records = dbContext.UserRegistry
                .Where(d => (d.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && d.Id == id
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Password, password)
                .Set(s => s.PasswordLocked, (byte)0)
                .Set(s => s.FailedPasswordCount, 0)
                .Set(s => s.PasswordExpiryDate, expiryDate)
                .Set(s => s.PasswordCreatedDate, DateTime.Now)
                .Update();

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public void SetLocked(int id)
        {
            var records = dbContext.UserRegistry
                .Where(d => (d.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && d.Id == id
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.PasswordLocked, (byte)1)
                .Set(s => s.PasswordLockedDate, DateTime.Now)
                .Update();

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public void IncrementFailedPassword(int id)
        {
            var existing = dbContext.UserRegistry
                .FirstOrDefault(w => w.Id
                                     == id
                                     && (w.Deleted == 0 || w.Deleted == null));

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            existing.FailedPasswordCount += 1;

            dbContext.Update(existing);
        }

        public void Delete(int id)
        {
            var records = dbContext.UserRegistry
                .Where(d => (d.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && d.Id == id
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
