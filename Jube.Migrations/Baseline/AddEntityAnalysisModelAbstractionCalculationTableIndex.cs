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
    [Migration(20220429124924)]
    public class AddEntityAnalysisModelAbstractionCalculationTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelAbstractionCalculation")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("EntityAnalysisModelAbstractionNameLeft").AsString().Nullable()
                .WithColumn("EntityAnalysisModelAbstractionNameRight").AsString().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("AbstractionCalculationTypeId").AsByte().Nullable()
                .WithColumn("ReportTable").AsByte().Nullable()
                .WithColumn("FunctionScript").AsString().Nullable();

            Create.Index().OnTable("EntityAnalysisModelAbstractionCalculation")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("Deleted").Ascending();

            var functionScript = "Return Abstraction.NotResponseCodeEqual0Volume / " + Environment.NewLine +
                                 " (Abstraction.NotResponseCodeEqual0Volume _  " + Environment.NewLine +
                                 "+ Abstraction.ResponseCodeEqual0Volume) _ " + Environment.NewLine;

            Insert.IntoTable("EntityAnalysisModelAbstractionCalculation").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "ResponseCodeVolumeRatio",
                Active = 1,
                AbstractionCalculationTypeId = 5,
                Version = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                ResponsePayload = 1,
                FunctionScript = functionScript
            });
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelAbstractionCalculation");
        }
    }
}