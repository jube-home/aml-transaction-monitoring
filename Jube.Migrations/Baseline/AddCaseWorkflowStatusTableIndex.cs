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
    [Migration(20220429124916)]
    public class AddCaseWorkflowStatusTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("CaseWorkflowStatus")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowId").AsInt32().Nullable()
                .WithColumn("Priority").AsByte().Nullable()
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
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("ForeColor").AsString().Nullable()
                .WithColumn("BackColor").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("EnableHttpEndpoint").AsByte().Nullable()
                .WithColumn("HttpEndpoint").AsString().Nullable()
                .WithColumn("HttpEndpointTypeId").AsByte().Nullable()
                .WithColumn("EnableNotification").AsByte().Nullable()
                .WithColumn("NotificationTypeId").AsByte().Nullable()
                .WithColumn("NotificationDestination").AsString().Nullable()
                .WithColumn("NotificationSubject").AsString().Nullable()
                .WithColumn("NotificationBody").AsString().Nullable();

            Create.Index().OnTable("CaseWorkflowStatus")
                .OnColumn("CaseWorkflowId").Ascending()
                .OnColumn("Deleted").Ascending();

            Insert.IntoTable("CaseWorkflowStatus").Row(new
            {
                CaseWorkflowId = 1,
                Name = "First Line Review",
                Active = 1,
                Priority = 5,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                ForeColor = "#260080",
                BackColor = "#abc8f7"
            });

            Insert.IntoTable("CaseWorkflowStatus").Row(new
            {
                CaseWorkflowId = 1,
                Name = "Supervisor Review",
                Active = 1,
                Priority = 4,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                ForeColor = "#260080",
                BackColor = "#fafcb1"
            });

            Insert.IntoTable("CaseWorkflowStatus").Row(new
            {
                CaseWorkflowId = 1,
                Name = "Restricted",
                Active = 1,
                Priority = 3,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                ForeColor = "#260080",
                BackColor = "#fce9c5"
            });

            Insert.IntoTable("CaseWorkflowStatus").Row(new
            {
                CaseWorkflowId = 1,
                Name = "Fraudulent",
                Active = 1,
                Priority = 4,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                ForeColor = "#260080",
                BackColor = "#facaf4"
            });

            Insert.IntoTable("CaseWorkflowStatus").Row(new
            {
                CaseWorkflowId = 1,
                Name = "Reported",
                Active = 1,
                Priority = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                ForeColor = "#260080",
                BackColor = "#f77781"
            });
        }

        public override void Down()
        {
            Delete.Table("CaseWorkflowStatus");
        }
    }
}