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

namespace Jube.Migrations.Branches
{
    [Migration(2024050107041100)]
    public class GitHubIssueBranch32 : Migration
    {
        public override void Up()
        {
            Alter.Table("CacheTtlCounterEntry")
                .AddColumn("Value")
                .AsInt32();

            Alter.Table("CacheTtlCounterEntry")
                .AddColumn("UpdatedDate")
                .AsDateTime2();

            Alter.Table("CacheAbstraction")
                .AddColumn("UpdatedDate")
                .AsDateTime2();

            Create.Index("IX_CacheTtlCounterEntry_Truncated_ReferenceDate_Value").OnTable("CacheTtlCounterEntry")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("EntityAnalysisModelTtlCounterId").Ascending()
                .OnColumn("DataName").Ascending()
                .OnColumn("DataValue").Ascending()
                .OnColumn("ReferenceDate")
                .Ascending()
                .WithOptions().Unique().Include("Value");

            Create.Table("CacheReferenceDate")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelId").AsInt64().Nullable()
                .WithColumn("ReferenceDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable();

            Create.Index().OnTable("CacheReferenceDate")
                .OnColumn("EntityAnalysisModelId").Ascending().OnColumn("EntityAnalysisModelId")
                .Ascending().WithOptions().Unique().Include("ReferenceDate");

            Alter.Table("EntityAnalysisModelRequestXpath").AddColumn("SearchKeyTtlInterval").AsString().Nullable();
            Update.Table("EntityAnalysisModelRequestXpath").Set(new {SearchKeyTtlInterval = "d"}).AllRows();

            Alter.Table("EntityAnalysisModelRequestXpath").AddColumn("SearchKeyTtlIntervalValue").AsInt32().Nullable();
            Update.Table("EntityAnalysisModelRequestXpath").Set(new {SearchKeyTtlIntervalValue = 1}).AllRows();

            Alter.Table("EntityAnalysisModelRequestXpath").AddColumn("SearchKeyFetchLimit").AsInt32().Nullable();
            Update.Table("EntityAnalysisModelRequestXpath").Set(new {SearchKeyFetchLimit = 100}).AllRows();

            Alter.Table("EntityAnalysisModel").AddColumn("CacheTtlInterval").AsString().Nullable();
            Update.Table("EntityAnalysisModel").Set(new {CacheTtlInterval = "d"}).AllRows();

            Alter.Table("EntityAnalysisModel").AddColumn("CacheTtlIntervalValue").AsInt32().Nullable();
            Update.Table("EntityAnalysisModel").Set(new {CacheTtlIntervalValue = 1}).AllRows();
        }

        public override void Down()
        {
            Delete.Column("Value")
                .FromTable("CacheTtlCounterEntry");

            Delete.Index("IX_CacheTtlCounterEntry_Truncated_ReferenceDate_Value");
        }
    }
}