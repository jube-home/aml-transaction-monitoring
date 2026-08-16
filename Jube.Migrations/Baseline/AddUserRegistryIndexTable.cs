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

namespace Jube.Migrations.Baseline
{
    using System;
    using Data.Security;
    using DynamicEnvironment;
    using FluentMigrator;

    [Migration(20220705082300)]
    public class AddUserRegistryTableIndex(DynamicEnvironment dynamicEnvironment) : Migration
    {
        public override void Up()
        {
            Create.Table("UserRegistry")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("RoleRegistryId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Email").AsString().Nullable()
                .WithColumn("Password").AsString().Nullable()
                .WithColumn("PasswordExpiryDate").AsDateTime().Nullable()
                .WithColumn("PasswordCreatedDate").AsDateTime().Nullable()
                .WithColumn("FailedPasswordCount").AsInt32().Nullable()
                .WithColumn("LastLoginDate").AsDateTime().Nullable()
                .WithColumn("PasswordLocked").AsByte().Nullable()
                .WithColumn("PasswordLockedDate").AsDateTime().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable();

            Create.Index().OnTable("UserRegistry")
                .OnColumn("Name").Ascending()
                .OnColumn("Active").Ascending();

            Create.Index().OnTable("UserRegistry")
                .OnColumn("Name").Ascending();

            var userRegistryEntry = new
            {
                RoleRegistryId = 1,
                Name = "Administrator",
                Email = "sink@jube.io",
                Password = HashPassword.Argon2("Administrator", dynamicEnvironment.AppSettings("PasswordHashingKey")),
                PasswordExpiryDate = DateTime.UtcNow,
                Active = 1,
                CreatedUser = "Administrator",
                CreatedDate = DateTime.UtcNow,
                Version = 1,
                PasswordCreatedDate = DateTime.UtcNow
            };

            Insert.IntoTable("UserRegistry").Row(userRegistryEntry);
        }

        public override void Down()
        {
            Delete.Table("UserRegistry");
        }
    }
}
