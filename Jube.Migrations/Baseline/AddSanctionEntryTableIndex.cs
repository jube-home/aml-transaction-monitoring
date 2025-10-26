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
    [Migration(20220429125015)]
    public class AddSanctionEntryTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("SanctionEntry")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("SanctionEntryElementValue").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("SanctionEntrySourceId").AsInt32().Nullable()
                .WithColumn("SanctionEntryReference").AsString().Nullable()
                .WithColumn("SanctionPayload").AsString().Nullable()
                .WithColumn("SanctionEntryHash").AsString().Nullable();

            Create.Index().OnTable("SanctionEntry")
                .OnColumn("SanctionEntrySourceId").Ascending();

            Create.Index().OnTable("SanctionEntry")
                .OnColumn("SanctionEntrySourceId").Ascending()
                .OnColumn("SanctionEntryHash").Ascending();

            Insert.IntoTable("SanctionEntry").Row(new
            {
                SanctionEntryElementValue = "Robert Mugabe",
                CreatedDate = DateTime.Now,
                SanctionEntrySourceId = 1,
                SanctionEntryReference = "Testing",
                SanctionPayload = "Robert Mugabe",
                SanctionEntryHash = "34700ceb0a3814d567351268e741f1eb"
            });
        }

        public override void Down()
        {
            Delete.Table("SanctionEntry");
        }
    }
}