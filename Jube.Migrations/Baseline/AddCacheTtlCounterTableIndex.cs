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
    [Migration(20220531133200)]
    public class AddCacheTtlCounterTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("CacheTtlCounter")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelId").AsInt64().Nullable()
                .WithColumn("DataName").AsString().Nullable()
                .WithColumn("DataValue").AsString().Nullable()
                .WithColumn("EntityAnalysisModelTtlCounterId").AsInt32().Nullable()
                .WithColumn("Value").AsInt32().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("ReferenceDate").AsDateTime2().Nullable();

            Create.Index("IX_Truncated_CacheTtlCounter_Access_Processing_ReferenceDate").OnTable("CacheTtlCounter")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("EntityAnalysisModelTtlCounterId").Ascending()
                .OnColumn("DataName").Ascending()
                .OnColumn("ReferenceDate").Descending();

            Create.Index("IX_Truncated_CacheTtlCounter_Access_Realtime").OnTable("CacheTtlCounter")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("EntityAnalysisModelTtlCounterId").Ascending()
                .OnColumn("DataName").Ascending()
                .OnColumn("DataValue").Ascending()
                .WithOptions().Unique()
                .Include("Value");
        }

        public override void Down()
        {
            Delete.Table("CacheTtlCounter");
        }
    }
}