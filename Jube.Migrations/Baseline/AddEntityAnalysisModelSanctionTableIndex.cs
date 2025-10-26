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
    [Migration(20220429124942)]
    public class AddEntityAnalysisModelSanctionTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelSanction")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("MultipartStringDataName").AsString().Nullable()
                .WithColumn("Distance").AsByte().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("ReportTable").AsByte().Nullable()
                .WithColumn("CacheValue").AsInt32().Nullable()
                .WithColumn("CacheInterval").AsString().Nullable();

            Create.Index().OnTable("EntityAnalysisModelSanction")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("Deleted").Ascending();

            Insert.IntoTable("EntityAnalysisModelSanction").Row(new
            {
                Name = "FuzzyMatchDistance2JoinedName",
                EntityAnalysisModelId = 1,
                MultipartStringDataName = "JoinedName",
                Distance = 2,
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                ResponsePayload = 1,
                CacheValue = 1,
                CacheInterval = "h"
            });
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelSanction");
        }
    }
}