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
namespace Jube.Data.Security
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;
    using Context;
    using Extension;
    using log4net;
    using ResilientNpgsqlConnection;

    public class PermissionValidation
    {
        public async Task<PermissionValidationDto> GetPermissionsAsync(
            string connectionString, string userName, ILog log)
        {
            await using var connection = new ResilientNpgsqlConnection(connectionString, log);
            await connection.OpenAsync().ConfigureAwait(false);
            return await GetPermissionsFromDatabaseAsync(connection, userName).ConfigureAwait(false);
        }

        public Task<PermissionValidationDto> GetPermissionsAsync(
            DbContext dbContext, string userName, ILog log)
        {
            var connection = dbContext.Connection as DbConnection
                             ?? throw new InvalidOperationException(
                                 $"Expected a DbConnection but got {dbContext.Connection.GetType().Name}. " +
                                 "Only providers based on DbConnection (Npgsql, SqlClient, etc.) are supported.");

            return GetPermissionsFromDatabaseAsync(connection, userName);
        }

        private async Task<PermissionValidationDto> GetPermissionsFromDatabaseAsync(
            DbConnection connection, string userName)
        {
            var dto = new PermissionValidationDto
            {
                Landlord = await IsLandlordAsync(connection, userName).ConfigureAwait(false)
            };

            await using var command = connection.CreateCommand();

            if (dto.Landlord)
            {
                command.CommandText =
                    "select \"Id\" " +
                    "from \"PermissionSpecification\"";
            }
            else
            {
                command.CommandText =
                    "select rrp.\"PermissionSpecificationId\" " +
                    "from \"RoleRegistryPermission\" rrp " +
                    "inner join \"RoleRegistry\" rr on rrp.\"RoleRegistryId\" = rr.\"Id\" " +
                    "inner join \"UserRegistry\" ur on ur.\"RoleRegistryGuid\" = rr.\"Guid\" " +
                    "where ur.\"Active\" = 1 " +
                    "and rr.\"Active\" = 1 " +
                    "and rrp.\"Active\" = 1 " +
                    "and (rr.\"Deleted\" = 0 or rr.\"Deleted\" IS NULL) " +
                    "and (rrp.\"Deleted\" = 0 or rrp.\"Deleted\" IS NULL) " +
                    "and (ur.\"PasswordLocked\" = 0 or ur.\"PasswordLocked\" IS NULL) " +
                    "and ur.\"Name\" = @userName";

                var p = command.CreateParameter();
                p.ParameterName = "userName";
                p.Value = userName;
                command.Parameters.Add(p);
            }

            await command.PrepareAsync().ConfigureAwait(false);

            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                dto.Permissions.Add(reader.GetValue(0).AsInt());
            }

            return dto;
        }

        private async Task<bool> IsLandlordAsync(DbConnection connection, string userName)
        {
            const string sql =
                "select tr.\"Landlord\" " +
                "from \"RoleRegistry\" rr " +
                "inner join \"TenantRegistry\" tr on rr.\"TenantRegistryId\" = tr.\"Id\" " +
                "inner join \"UserRegistry\" ur on ur.\"RoleRegistryGuid\" = rr.\"Guid\" " +
                "where ur.\"Name\" = @userName " +
                "and ur.\"Active\" = 1 " +
                "and (rr.\"Deleted\" = 0 or rr.\"Deleted\" IS NULL) " +
                "and tr.\"Active\" = 1 " +
                "order by tr.\"Id\"";

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            var p = command.CreateParameter();
            p.ParameterName = "userName";
            p.Value = userName;
            command.Parameters.Add(p);

            await command.PrepareAsync().ConfigureAwait(false);

            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            if (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (!await reader.IsDBNullAsync(0).ConfigureAwait(false))
                {
                    return reader.GetValue(0).AsShort() == 1;
                }
            }

            return false;
        }
    }
}
