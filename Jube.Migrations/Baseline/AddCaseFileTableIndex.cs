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
    [Migration(20220615103900)]
    public class AddCaseFileTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("CaseFile")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseId").AsInt32()
                .WithColumn("CaseKey").AsString().Nullable()
                .WithColumn("CaseKeyValue").AsString().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("ContentType").AsString().Nullable()
                .WithColumn("Extension").AsString().Nullable()
                .WithColumn("Size").AsInt64().Nullable()
                .WithColumn("Object").AsBinary().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable();

            Create.Index().OnTable("CaseFile")
                .OnColumn("CaseKey").Ascending()
                .OnColumn("CaseKeyValue").Ascending();
        }

        public override void Down()
        {
            Delete.Table("CaseFile");
        }
    }
}