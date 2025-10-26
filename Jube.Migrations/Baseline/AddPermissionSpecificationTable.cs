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
    [Migration(20220429125011)]
    public class AddPermissionSpecificationTable : Migration
    {
        public override void Up()
        {
            Create.Table("PermissionSpecification")
                .WithColumn("Id").AsInt32().PrimaryKey()
                .WithColumn("Name").AsString().Nullable();

            Insert.IntoTable("PermissionSpecification").Row(new { Id = 1, Name = "Read Write Case" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 2, Name = "Read Write Suppression" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 3, Name = "Read Write List" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 4, Name = "Read Write Dictionary" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 5, Name = "Allow Synchronisation" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 6, Name = "Read Write Model" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 7, Name = "Read Write Request XPath" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 8, Name = "Read Write Inline Function" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 9, Name = "Read Write Inline Script" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 10, Name = "Read Write Gateway Rule" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 11, Name = "Read Write Sanction" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 12, Name = "Read Write TTL Counter" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 13, Name = "Read Write Abstraction Rule" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 14, Name = "Read Write Abstraction Calculation" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 15, Name = "Read Write HTTP Adaptation" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 16, Name = "Read Write Exhaustive" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 17, Name = "Read Write Activation Rule" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 18, Name = "Read Write Case Workflow" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 19, Name = "Read Write Cases Workflows Status" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 20, Name = "Read Write Case Workflow XPath" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 21, Name = "Read Write Cases Workflow Form" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 22, Name = "Read Write Cases Workflow Action" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 23, Name = "Read Write Cases Workflow Display" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 24, Name = "Read Write Cases Workflow Macro" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 25, Name = "Read Write Cases Workflow Filter" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 26, Name = "Read Write Reprocessing" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 27, Name = "View Counter and Balance" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 28, Name = "View Visualisation Directory" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 29, Name = "View Sanction" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 30, Name = "View Watcher" });
            Insert.IntoTable("PermissionSpecification")
                .Row(new { Id = 31, Name = "Read Write Visualisation Administration" });
            Insert.IntoTable("PermissionSpecification")
                .Row(new { Id = 32, Name = "Read Write Visualisation Parameter Administration" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 33, Name = "Read Write Visualisation Datasource" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 34, Name = "Read Write Security Role" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 35, Name = "Read Write Security User" });
            Insert.IntoTable("PermissionSpecification")
                .Row(new { Id = 36, Name = "Read Write Security Role User Permission" });
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 37, Name = "Read Write Tags" });
        }

        public override void Down()
        {
            Delete.Table("PermissionSpecification");
        }
    }
}