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

    [Migration(20263107130000)]
    public class SanctionEntryImportAndRejectionTables : Migration
    {
        public override void Up()
        {
            Create.Table("SanctionEntryImport")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("SanctionEntrySourceId").AsInt32().Nullable()
                .WithColumn("StartDate").AsDateTime2().Nullable()
                .WithColumn("EndDate").AsDateTime2().Nullable()
                .WithColumn("TotalRows").AsInt32().Nullable()
                .WithColumn("InsertedCount").AsInt32().Nullable()
                .WithColumn("RevivedCount").AsInt32().Nullable()
                .WithColumn("UnchangedCount").AsInt32().Nullable()
                .WithColumn("RemovedCount").AsInt32().Nullable()
                .WithColumn("RejectedCount").AsInt32().Nullable()
                .WithColumn("Successful").AsByte().Nullable()
                .WithColumn("ErrorMessage").AsString().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable();

            Create.ForeignKey().FromTable("SanctionEntryImport").ForeignColumn("SanctionEntrySourceId")
                .ToTable("SanctionEntrySource").PrimaryColumn("Id");

            Create.Index().OnTable("SanctionEntryImport")
                .OnColumn("SanctionEntrySourceId").Ascending();

            Create.Table("SanctionEntryRejection")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("SanctionEntryImportId").AsInt32().Nullable()
                .WithColumn("SanctionEntrySourceId").AsInt32().Nullable()
                .WithColumn("RowNumber").AsInt32().Nullable()
                .WithColumn("RawData").AsString().Nullable()
                .WithColumn("Reason").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable();

            Create.ForeignKey().FromTable("SanctionEntryRejection").ForeignColumn("SanctionEntryImportId")
                .ToTable("SanctionEntryImport").PrimaryColumn("Id");

            Create.ForeignKey().FromTable("SanctionEntryRejection").ForeignColumn("SanctionEntrySourceId")
                .ToTable("SanctionEntrySource").PrimaryColumn("Id");

            Create.Index().OnTable("SanctionEntryRejection")
                .OnColumn("SanctionEntryImportId").Ascending();
        }

        public override void Down()
        {
            Delete.Table("SanctionEntryRejection");
            Delete.Table("SanctionEntryImport");
        }
    }
}
