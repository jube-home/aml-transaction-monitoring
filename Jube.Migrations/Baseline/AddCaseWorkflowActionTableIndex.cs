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
    [Migration(20220611123500)]
    public class AddCaseWorkflowActionTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("CaseWorkflowAction")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("EnableHttpEndpoint").AsByte().Nullable()
                .WithColumn("HttpEndpoint").AsString().Nullable()
                .WithColumn("HttpEndpointTypeId").AsByte().Nullable()
                .WithColumn("EnableNotification").AsByte().Nullable()
                .WithColumn("NotificationTypeId").AsByte().Nullable()
                .WithColumn("NotificationDestination").AsString().Nullable()
                .WithColumn("NotificationSubject").AsString().Nullable()
                .WithColumn("NotificationBody").AsString().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowAction")
                .OnColumn("CaseWorkflowId").Ascending()
                .OnColumn("Deleted").Ascending();

            Insert.IntoTable("CaseWorkflowAction").Row(new
            {
                Name = "Call Customer",
                CaseWorkflowId = 1,
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1
            });

            Insert.IntoTable("CaseWorkflowAction").Row(new
            {
                Name = "Email Customer",
                CaseWorkflowId = 1,
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1
            });

            Insert.IntoTable("CaseWorkflowAction").Row(new
            {
                Name = "Escalation",
                CaseWorkflowId = 1,
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1
            });

            Insert.IntoTable("CaseWorkflowAction").Row(new
            {
                Name = "General Action",
                CaseWorkflowId = 1,
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1
            });
        }

        public override void Down()
        {
            Delete.Table("CaseWorkflowAction");
        }
    }
}