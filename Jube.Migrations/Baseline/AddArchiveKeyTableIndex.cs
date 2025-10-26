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
    [Migration(20220429124901)]
    public class AddArchiveKeyTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("ArchiveKey")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("ProcessingTypeId").AsByte().Nullable()
                .WithColumn("Key").AsString().Nullable()
                .WithColumn("KeyValueString").AsString().Nullable()
                .WithColumn("KeyValueInteger").AsInt32().Nullable()
                .WithColumn("KeyValueFloat").AsDouble().Nullable()
                .WithColumn("KeyValueBoolean").AsByte().Nullable()
                .WithColumn("KeyValueDate").AsDateTime2().Nullable()
                .WithColumn("EntityAnalysisModelInstanceEntryGuid").AsGuid();

            Create.Index().OnTable("ArchiveKey")
                .OnColumn("Key").Ascending()
                .OnColumn("KeyValueString").Ascending();

            Create.Index().OnTable("ArchiveKey")
                .OnColumn("Key").Ascending()
                .OnColumn("KeyValueInteger").Ascending();

            Create.Index().OnTable("ArchiveKey")
                .OnColumn("Key").Ascending()
                .OnColumn("KeyValueFloat").Ascending();

            Create.Index().OnTable("ArchiveKey")
                .OnColumn("Key").Ascending()
                .OnColumn("KeyValueBoolean").Ascending();

            Create.Index().OnTable("ArchiveKey")
                .OnColumn("Key").Ascending()
                .OnColumn("KeyValueDate").Ascending();
        }

        public override void Down()
        {
            Delete.Table("ArchiveKey");
        }
    }
}