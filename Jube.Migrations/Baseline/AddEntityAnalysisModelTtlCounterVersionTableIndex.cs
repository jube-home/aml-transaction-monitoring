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
    [Migration(20220819093600)]
    public class AddEntityAnalysisModelTtlCounterVersionTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelTtlCounterVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelTtlCounterId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("TtlCounterInterval").AsString().Nullable()
                .WithColumn("TtlCounterValue").AsInt32().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("ReportTable").AsString().Nullable()
                .WithColumn("TtlCounterDataName").AsString().Nullable()
                .WithColumn("OnlineAggregation").AsByte().Nullable()
                .WithColumn("EnableLiveForever").AsByte().Nullable();
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelTtlCounterVersion");
        }
    }
}