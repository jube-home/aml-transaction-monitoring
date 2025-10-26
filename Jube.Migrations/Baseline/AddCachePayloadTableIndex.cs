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
    [Migration(20220529090400)]
    public class AddCachePayloadTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("CachePayload")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelId").AsInt64().Nullable()
                .WithColumn("Json").AsCustom("jsonb").Nullable()
                .WithColumn("EntityAnalysisModelInstanceEntryGuid").AsGuid().Nullable()
                .WithColumn("ReferenceDate").AsDateTime2().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable();

            Create.Index().OnTable("CachePayload").OnColumn("EntityAnalysisModelId").Ascending().OnColumn("CreatedDate")
                .Descending();

            Create.Index().OnTable("CachePayload").OnColumn("EntityAnalysisModelInstanceEntryGuid").Unique();
        }

        public override void Down()
        {
            Delete.Table("CachePayload");
        }
    }
}