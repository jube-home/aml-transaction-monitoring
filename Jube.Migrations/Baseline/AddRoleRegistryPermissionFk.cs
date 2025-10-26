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

using FluentMigrator;

namespace Jube.Migrations.Baseline
{
    [Migration(20220430125453)]
    public class AddRoleRegistryPermissionFk : Migration
    {
        public override void Up()
        {
            Create.ForeignKey().FromTable("RoleRegistryPermission").ForeignColumn("PermissionSpecificationId")
                .ToTable("PermissionSpecification").PrimaryColumn("Id");

            Create.ForeignKey().FromTable("RoleRegistryPermission").ForeignColumn("RoleRegistryId")
                .ToTable("RoleRegistry").PrimaryColumn("Id");
        }

        public override void Down()
        {
            Delete.ForeignKey().FromTable("RoleRegistryPermission").ForeignColumn("PermissionSpecificationId")
                .ToTable("PermissionSpecification").PrimaryColumn("Id");

            Delete.ForeignKey().FromTable("RoleRegistryPermission").ForeignColumn("RoleRegistryId")
                .ToTable("RoleRegistry").PrimaryColumn("Id");
        }
    }
}