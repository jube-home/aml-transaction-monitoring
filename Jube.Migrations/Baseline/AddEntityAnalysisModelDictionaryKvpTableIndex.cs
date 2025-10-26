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
    [Migration(20220429124929)]
    public class AddEntityAnalysisModelDictionaryKvpTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelDictionaryKvp")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelDictionaryId").AsInt32().Nullable()
                .WithColumn("KvpKey").AsString().Nullable()
                .WithColumn("KvpValue").AsDouble().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable();

            Create.Index().OnTable("EntityAnalysisModelDictionaryKvp")
                .OnColumn("EntityAnalysisModelDictionaryId").Ascending()
                .OnColumn("Deleted").Ascending();

            Insert.IntoTable("EntityAnalysisModelDictionaryKvp").Row(new
            {
                EntityAnalysisModelDictionaryId = 1,
                KvpKey = "Test1",
                KvpValue = 1000,
                Version = 1,
                CreatedUser = "Administrator",
                CreatedDate = DateTime.Now,
                Active = 1
            });
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelDictionaryKvp");
        }
    }
}