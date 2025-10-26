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
    [Migration(20220429124951)]
    public class AddEntityAnalysisModelTtlCounterTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelTtlCounter")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
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

            Create.Index().OnTable("EntityAnalysisModelTtlCounter")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("Deleted").Ascending();

            Insert.IntoTable("EntityAnalysisModelTtlCounter").Row(new
            {
                Name = "TtlCounterAll",
                EntityAnalysisModelId = 1,
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                TtlCounterInterval = "h",
                TtlCounterValue = 1,
                ResponsePayload = 1,
                TtlCounterDataName = "AccountId",
                OnlineAggregation = 0,
                EnableLiveForever = 0
            });
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelTtlCounter");
        }
    }
}