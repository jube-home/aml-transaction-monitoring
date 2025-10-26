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
using FluentMigrator.Postgres;

namespace Jube.Migrations.Baseline
{
    [Migration(20220429124907)]
    public class AddCaseTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("Case")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelInstanceEntryGuid").AsGuid().Nullable()
                .WithColumn("DiaryDate").AsDateTime2().Nullable()
                .WithColumn("CaseWorkflowId").AsInt32().Nullable()
                .WithColumn("CaseWorkflowStatusId").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("LockedUser").AsString().Nullable()
                .WithColumn("LockedDate").AsDateTime2().Nullable()
                .WithColumn("ClosedStatusId").AsByte().Nullable()
                .WithColumn("ClosedDate").AsDateTime2().Nullable()
                .WithColumn("ClosedUser").AsString().Nullable()
                .WithColumn("CaseKey").AsString().Nullable()
                .WithColumn("Diary").AsByte().Nullable()
                .WithColumn("DiaryUser").AsString().Nullable()
                .WithColumn("Json").AsCustom("jsonb").Nullable()
                .WithColumn("Rating").AsByte().Nullable()
                .WithColumn("CaseKeyValue").AsString().Nullable()
                .WithColumn("LastClosedStatus").AsByte().Nullable()
                .WithColumn("ClosedStatusMigrationDate").AsDateTime2().Nullable();

            Create.Index().OnTable("Case")
                .OnColumn("CaseKey").Ascending()
                .OnColumn("CaseKeyValue").Ascending()
                .OnColumn("CaseWorkflowId").Ascending()
                .OnColumn("ClosedStatusId").Ascending();

            Create.Index()
                .OnTable("Case")
                .OnColumn("Json").Ascending()
                .WithOptions()
                .UsingGin();
        }

        public override void Down()
        {
            Delete.Table("Case");
        }
    }
}