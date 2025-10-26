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
    [Migration(20220429124910)]
    public class AddCaseWorkflowDisplayTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("CaseWorkflowDisplay")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
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
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Html").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowDisplay")
                .OnColumn("CaseWorkflowId").Ascending()
                .OnColumn("Deleted").Ascending();

            Insert.IntoTable("CaseWorkflowDisplay").Row(new
            {
                CaseWorkflowId = 1,
                Name = "ExampleDisplayCurrencyAmount",
                Active = 1,
                CreatedUser = "Administrator",
                CreatedDate = DateTime.Now,
                Version = 1,
                Html = "This is an example Cases Workflow Display:" + Environment.NewLine +
                       "The transaction amount is:" + Environment.NewLine +
                       "<br/>" + Environment.NewLine +
                       "<br/>" + Environment.NewLine +
                       "<div style='font-size:30px'>[@CurrencyAmount@]</div>" + Environment.NewLine +
                       "<br/>" + Environment.NewLine +
                       "The tokens are taken from the Cases Workflows XPath and can be laid out in HTML."
            });
        }

        public override void Down()
        {
            Delete.Table("CaseWorkflowDisplay");
        }
    }
}