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

namespace Jube.Migrations.Branches.GitHubIssueBranch139
{
    using FluentMigrator;
    using FluentMigrator.Postgres;

    [Migration(20260528121500)]
    public class ArchiveTagTableAndIndex : Migration
    {
        public override void Up()
        {
            Create.Table("ArchiveTag")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("EntityAnalysisModelInstanceEntryGuid").AsGuid().NotNullable()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Version").AsInt32().NotNullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable();

            Create.Index().OnTable("ArchiveTag")
                .OnColumn("EntityAnalysisModelInstanceEntryGuid").Ascending()
                .OnColumn("Deleted").Ascending()
                .WithOptions().Include("Name");

            Create.Table("ArchiveTagVersion")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("ArchiveTagId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelInstanceEntryGuid").AsGuid().NotNullable()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Version").AsInt32().NotNullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("ArchiveTagVersion")
                .ForeignColumn("ArchiveTagId")
                .ToTable("ArchiveTag").PrimaryColumn("Id");

            Create.Index().OnTable("ArchiveTagVersion")
                .OnColumn("EntityAnalysisModelInstanceEntryGuid").Ascending()
                .OnColumn("Name").Ascending();
        }

        public override void Down()
        {

        }
    }
}
