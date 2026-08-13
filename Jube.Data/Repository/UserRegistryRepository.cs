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

        public UserRegistryRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public Task<UserRegistry> GetByNameAsync(string name, CancellationToken token = default)
        {
            return dbContext.UserRegistry
                .FirstOrDefaultAsync(f =>
                    f.RoleRegistry.TenantRegistryId == tenantRegistryId
                    && (f.Deleted == 0 || f.Deleted == null)
                    && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<UserRegistry>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.UserRegistry
                .Where(w => w.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                .ToListAsync(token);
        }

        public Task<bool> AnyAsync(CancellationToken token = default)
        {
            return dbContext.UserRegistry.AnyAsync(w => w.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue, token);
        }

        public async Task<IEnumerable<UserRegistry>> GetByRoleRegistryGuidAsync(Guid roleRegistryGuid, CancellationToken token = default)
        {
            return await dbContext.UserRegistry
                .Where(w => w.RoleRegistry.TenantRegistryId == tenantRegistryId
                            && w.RoleRegistryGuid == roleRegistryGuid && (w.Deleted == 0 || w.Deleted == null)
                            && (w.RoleRegistry.Deleted == 0 || w.RoleRegistry.Deleted == null))
                .ToListAsync(token);
        }

        public Task<UserRegistry> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.UserRegistry.FirstOrDefaultAsync(w =>
                (w.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Id == id && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<UserRegistry> GetByUserNameAsync(string userNameInTenant, CancellationToken token = default)
        {
            return dbContext.UserRegistry.FirstOrDefaultAsync(w =>
                (w.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Name == userNameInTenant && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<UserRegistry> InsertAsync(UserRegistry model, CancellationToken token = default)
        {
            if (!tenantRegistryId.HasValue)
            {
                throw new KeyNotFoundException();
            }

            model.CreatedUser = userName;
            model.Version = 1;
            model.CreatedDate = DateTime.UtcNow;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            var userInTenant = new UserInTenant
            {
                User = model.Name,
                TenantRegistryId = tenantRegistryId.Value,
                SwitchedUser = userName,
                SwitchedDate = DateTime.UtcNow
            };

            await dbContext.InsertAsync(userInTenant, token: token);

            return model;
        }

        public async Task<UserRegistry> UpdateAsync(UserRegistry model, CancellationToken token = default)
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
            model.Password = existing.Password;
            model.PasswordExpiryDate = existing.PasswordExpiryDate;
            model.PasswordCreatedDate = existing.PasswordCreatedDate;
            model.CreatedDate = DateTime.UtcNow;

            await dbContext.UpdateAsync(model, token: token);

            var mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UserRegistry, UserRegistryVersion>();
            }, NullLoggerFactory.Instance));

            var audit = mapper.Map<UserRegistryVersion>(existing);
            audit.UserRegistryId = existing.Id;

            await dbContext.InsertAsync(audit, token: token);

            return model;
        }

        public async Task ResetFailedPasswordCountAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.UserRegistry
                .Where(d => (d.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && d.Id == id
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.FailedPasswordCount, 0)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public async Task SetPasswordAsync(int id, string password, DateTime? expiryDate, bool wirePasswordHash = true, CancellationToken token = default)
        {
            var records = await dbContext.UserRegistry
                .Where(d => (d.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && d.Id == id
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Password, password)
                .Set(s => s.PasswordLocked, (byte)0)
                .Set(s => s.FailedPasswordCount, 0)
                .Set(s => s.WirePasswordHash, wirePasswordHash ? (byte)1 : (byte)0)
                .Set(s => s.PasswordExpiryDate, expiryDate)
                .Set(s => s.PasswordCreatedDate, DateTime.UtcNow)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public async Task SetLockedAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.UserRegistry
                .Where(d => (d.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && d.Id == id
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.PasswordLocked, (byte)1)
                .Set(s => s.PasswordLockedDate, DateTime.UtcNow)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public Task IncrementFailedPasswordAsync(int id, CancellationToken token = default)
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

            return dbContext.UpdateAsync(existing, token: token);
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.UserRegistry
                .Where(d => (d.RoleRegistry.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
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

        public async Task RecoverOrphanedUsersAsync(CancellationToken token = default)
        {
            if (!tenantRegistryId.HasValue)
            {
                throw new KeyNotFoundException();
            }

            var namesInTenant = await dbContext.UserInTenant
                .Where(w => w.TenantRegistryId == tenantRegistryId)
                .Select(s => s.User)
                .ToListAsync(token);

            var orphanedUsers = await dbContext.UserRegistry
                .Where(w => namesInTenant.Contains(w.Name)
                            && (w.Deleted == 0 || w.Deleted == null)
                            && !dbContext.RoleRegistry.Any(r =>
                                r.Guid == w.RoleRegistryGuid && (r.Deleted == 0 || r.Deleted == null)))
                .ToListAsync(token);

            if (orphanedUsers.Count == 0)
            {
                return;
            }

            var roleRegistryRepository = new RoleRegistryRepository(dbContext, userName);
            var roleRegistry = await roleRegistryRepository.InsertAsync(new RoleRegistry
            {
                Name = "Orphaned Preservation Import",
                Active = 1
            }, token);

            var permissionSpecifications = await new PermissionSpecificationRepository(dbContext).GetAsync(token);

            var roleRegistryPermissionRepository = new RoleRegistryPermissionRepository(dbContext, userName);
            foreach (var permissionSpecification in permissionSpecifications)
            {
                await roleRegistryPermissionRepository.InsertAsync(new RoleRegistryPermission
                {
                    RoleRegistryId = roleRegistry.Id,
                    PermissionSpecificationId = permissionSpecification.Id,
                    Active = 1
                }, token);
            }

            foreach (var orphanedUser in orphanedUsers)
            {
                orphanedUser.RoleRegistryGuid = roleRegistry.Guid;

                orphanedUser.Active = orphanedUser.Name == userName
                    ? (byte)1
                    : (byte)0;

                await UpdateAsync(orphanedUser, token);
            }
        }
    }
}
