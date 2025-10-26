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
    [Migration(20220429124940)]
    public class AddEntityAnalysisModelReprocessingRuleTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelReprocessingRule")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Priority").AsByte().Nullable()
                .WithColumn("BuilderRuleScript").AsString().Nullable()
                .WithColumn("Json").AsCustom("jsonb").Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("Version").AsByte().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("ReprocessingSample").AsDouble().Nullable()
                .WithColumn("CoderRuleScript").AsString().Nullable()
                .WithColumn("RuleScriptTypeId").AsByte().Nullable()
                .WithColumn("ReprocessingValue").AsInt32().Nullable()
                .WithColumn("ReprocessingInterval").AsString().Nullable();

            Create.Index().OnTable("EntityAnalysisModelReprocessingRule")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("Deleted").Ascending();
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelReprocessingRule");
        }
    }
}