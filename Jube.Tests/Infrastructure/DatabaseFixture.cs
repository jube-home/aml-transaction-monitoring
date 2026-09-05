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

using System;
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;
using Xunit;

namespace Jube.Test.Infrastructure
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class DatabaseFixture : IAsyncLifetime
    {
        public const string Prefix = "ZzTest";

        private string ConnectionString { get; } =
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        public SeedData Seed { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await using (var probe = GetDbContext())
            {
                try
                {
                    _ = await probe.TenantRegistry.Take(1).ToListAsync().ConfigureAwait(false);
                    _ = await probe.EntityAnalysisModel.Take(1).ToListAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "DatabaseFixture: could not read from a running, migrated Jube schema at the configured " +
                        "connection string. Point JubeTestConnectionString at a running, migrated Jube instance " +
                        "-- the service-layer suite does not provision one.", ex);
                }
            }

            await using var dbContext = GetDbContext();
            Seed = await SeedAsync(dbContext).ConfigureAwait(false);
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = GetDbContext();

            await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.EntityAnalysisModel
                .Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .DeleteAsync().ConfigureAwait(false);

            var prefixedRoleRegistryIds = dbContext.RoleRegistry
                .Where(r => r.Name != null && r.Name.StartsWith(Prefix))
                .Select(r => (int?)r.Id);

            await dbContext.RoleRegistryPermission
                .Where(w => prefixedRoleRegistryIds.Contains(w.RoleRegistryId))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.RoleRegistry
                .Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.UserInTenant
                .Where(w => w.User != null && w.User.StartsWith(Prefix))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.UserRegistry
                .Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.TenantRegistry
                .Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .DeleteAsync().ConfigureAwait(false);
        }

        public DbContext GetDbContext()
        {
            return DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);
        }

        private static async Task<SeedData> SeedAsync(DbContext dbContext)
        {
            int[] readWriteSpecs = [2, 6, 7, 8, 12, 13];

            var suffix = Guid.NewGuid().ToString("N")[..8];
            var tenantAId = await InsertTenantAsync(dbContext, $"{Prefix}TenantA{suffix}", false)
                .ConfigureAwait(false);
            var tenantBId = await InsertTenantAsync(dbContext, $"{Prefix}TenantB{suffix}", false)
                .ConfigureAwait(false);
            var landlordTenantId = await InsertTenantAsync(dbContext, $"{Prefix}TenantL{suffix}", true)
                .ConfigureAwait(false);
            var roleWithPermissionAId =
                await InsertRoleAsync(dbContext, $"{Prefix}RoleWithPermissionA{suffix}", tenantAId,
                    readWriteSpecs).ConfigureAwait(false);
            var roleWithoutPermissionAId =
                await InsertRoleAsync(dbContext, $"{Prefix}RoleWithoutPermissionA{suffix}", tenantAId, [])
                    .ConfigureAwait(false);
            var roleWithPermissionBId =
                await InsertRoleAsync(dbContext, $"{Prefix}RoleWithPermissionB{suffix}", tenantBId,
                    readWriteSpecs).ConfigureAwait(false);
            var landlordRoleId = await InsertRoleAsync(dbContext, $"{Prefix}RoleLandlord{suffix}", landlordTenantId, [])
                .ConfigureAwait(false);
            var userWithPermission = $"{Prefix}UserWithPermission{suffix}";
            var userWithoutPermission = $"{Prefix}UserWithoutPermission{suffix}";
            var userTenantB = $"{Prefix}UserTenantB{suffix}";
            var landlordUser = $"{Prefix}UserLandlord{suffix}";
            var userNoTenant = $"{Prefix}UserNoTenant{suffix}";
            var userBothTenants = $"{Prefix}UserBothTenants{suffix}";
            var roleWithPermissionAGuid = await InsertUserAsync(dbContext, userWithPermission, roleWithPermissionAId)
                .ConfigureAwait(false);
            var roleWithoutPermissionAGuid =
                await InsertUserAsync(dbContext, userWithoutPermission, roleWithoutPermissionAId).ConfigureAwait(false);
            var roleWithPermissionBGuid =
                await InsertUserAsync(dbContext, userTenantB, roleWithPermissionBId).ConfigureAwait(false);
            var landlordRoleGuid = await InsertUserAsync(dbContext, landlordUser, landlordRoleId).ConfigureAwait(false);
            var noTenantRoleGuid =
                await InsertUserAsync(dbContext, userNoTenant, roleWithPermissionAId).ConfigureAwait(false);
            var bothTenantsRoleGuid = await InsertUserAsync(dbContext, userBothTenants, roleWithPermissionAId)
                .ConfigureAwait(false);

            await dbContext.InsertAsync(new UserInTenant { User = userWithPermission, TenantRegistryId = tenantAId })
                .ConfigureAwait(false);
            await dbContext.InsertAsync(new UserInTenant { User = userWithoutPermission, TenantRegistryId = tenantAId })
                .ConfigureAwait(false);
            await dbContext.InsertAsync(new UserInTenant { User = userTenantB, TenantRegistryId = tenantBId })
                .ConfigureAwait(false);
            await dbContext.InsertAsync(new UserInTenant { User = landlordUser, TenantRegistryId = landlordTenantId })
                .ConfigureAwait(false);
            await dbContext.InsertAsync(new UserInTenant { User = userBothTenants, TenantRegistryId = tenantAId })
                .ConfigureAwait(false);
            await dbContext.InsertAsync(new UserInTenant { User = userBothTenants, TenantRegistryId = tenantBId })
                .ConfigureAwait(false);

            _ = roleWithPermissionAGuid;
            _ = roleWithoutPermissionAGuid;
            _ = roleWithPermissionBGuid;
            _ = landlordRoleGuid;
            _ = noTenantRoleGuid;
            _ = bothTenantsRoleGuid;

            return new SeedData(
                userWithPermission, userWithoutPermission, userTenantB, landlordUser, userNoTenant, userBothTenants,
                $"{Prefix}UnknownUser{suffix}");
        }

        private static Task<int> InsertTenantAsync(DbContext dbContext, string name, bool landlord)
        {
            return dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = name,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Landlord = (byte)(landlord ? 1 : 0),
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = Prefix
            });
        }

        private static async Task<int> InsertRoleAsync(DbContext dbContext, string name, int tenantRegistryId,
            int[] specs)
        {
            var guid = Guid.NewGuid();
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = guid,
                Name = name,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantRegistryId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = Prefix
            }).ConfigureAwait(false);

            foreach (var spec in specs)
                await dbContext.InsertAsync(new RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(),
                    PermissionSpecificationId = spec,
                    RoleRegistryId = roleId,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = Prefix
                }).ConfigureAwait(false);

            return roleId;
        }

        private static async Task<Guid> InsertUserAsync(DbContext dbContext, string userName, int roleRegistryId)
        {
            var role = await dbContext.RoleRegistry.FirstAsync(f => f.Id == roleRegistryId).ConfigureAwait(false);

            await dbContext.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = role.Guid,
                Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = Prefix
            }).ConfigureAwait(false);

            return role.Guid;
        }
    }

    [CollectionDefinition("Database")]
    public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>;

    public sealed record SeedData(
        string UserWithPermission,
        string UserWithoutPermission,
        string UserTenantB,
        string LandlordUser,
        string UserNoTenant,
        string UserBothTenants,
        string UnknownUser);
}