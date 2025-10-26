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
    [Migration(20220429124944)]
    public class AddEntityAnalysisModelSearchKeyCalculationInstanceTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelSearchKeyCalculationInstance")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("SearchKey").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Completed").AsByte().Nullable()
                .WithColumn("CompletedDate").AsDateTime().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Priority").AsByte().Nullable()
                .WithColumn("DistinctValuesCount").AsInt32().Nullable()
                .WithColumn("DistinctValuesUpdatedDate").AsDateTime().Nullable()
                .WithColumn("DistinctValuesProcessedValuesCount").AsInt32().Nullable()
                .WithColumn("DistinctValuesProcessedValuesUpdatedDate").AsDateTime().Nullable()
                .WithColumn("DistinctFetchToDate").AsDateTime().Nullable()
                .WithColumn("ExpiredSearchKeyCacheDate").AsDateTime().Nullable()
                .WithColumn("ExpiredSearchKeyCacheCount").AsInt32().Nullable();

            Create.Index().OnTable("EntityAnalysisModelSearchKeyCalculationInstance")
                .OnColumn("EntityAnalysisModelId").Ascending();
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelSearchKeyCalculationInstance");
        }
    }
}