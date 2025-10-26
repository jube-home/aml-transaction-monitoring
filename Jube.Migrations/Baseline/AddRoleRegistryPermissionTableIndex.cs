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
using FluentMigrator;

namespace Jube.Migrations.Baseline
{
    [Migration(20220429125012)]
    public class AddRoleRegistryPermissionTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("RoleRegistryPermission")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("RoleRegistryId").AsInt32().Nullable()
                .WithColumn("PermissionSpecificationId").AsInt32().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Version").AsInt32().Nullable();

            Create.Index().OnTable("RoleRegistryPermission")
                .OnColumn("RoleRegistryId").Ascending();

            const int numberOfUserRoleRegistryPermissionSpecifications = 37;
            for (var i = 1; i <= numberOfUserRoleRegistryPermissionSpecifications; i++)
            {
                var roleRegistryPermissionEntry = new
                {
                    RoleRegistryId = 1,
                    PermissionSpecificationId = i,
                    Active = 1,
                    CreatedDate = DateTime.Now,
                    CreatedUser = "Administrator",
                    Version = 1
                };

                Insert.IntoTable("RoleRegistryPermission").Row(roleRegistryPermissionEntry);
            }
        }

        public override void Down()
        {
            Delete.Table("RoleRegistryPermission");
        }
    }
}