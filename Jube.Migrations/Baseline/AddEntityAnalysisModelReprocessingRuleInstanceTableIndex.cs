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
    [Migration(20220429124939)]
    public class AddEntityAnalysisModelReprocessingRuleInstanceTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelReprocessingRuleInstance")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelReprocessingRuleId").AsInt32().Nullable()
                .WithColumn("StatusId").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("StartedDate").AsDateTime().Nullable()
                .WithColumn("AvailableCount").AsInt64().Nullable()
                .WithColumn("SampledCount").AsInt64().Nullable()
                .WithColumn("MatchedCount").AsInt64().Nullable()
                .WithColumn("ProcessedCount").AsInt64().Nullable()
                .WithColumn("CompletedDate").AsDateTime().Nullable()
                .WithColumn("ErrorCount").AsInt64().Nullable()
                .WithColumn("ReferenceDate").AsDateTime().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable();

            Create.Index().OnTable("EntityAnalysisModelReprocessingRuleInstance")
                .OnColumn("EntityAnalysisModelReprocessingRuleId").Ascending()
                .OnColumn("Deleted").Ascending();
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelReprocessingRuleInstance");
        }
    }
}