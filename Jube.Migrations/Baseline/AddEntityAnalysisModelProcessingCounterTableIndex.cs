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
    [Migration(20220429124938)]
    public class AddAddEntityAnalysisModelProcessingCounterTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelProcessingCounter")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("Instance").AsString().Nullable()
                .WithColumn("ModelInvoke").AsInt32().Nullable()
                .WithColumn("GatewayMatch").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("ResponseElevation").AsInt32().Nullable()
                .WithColumn("ResponseElevationSum").AsDouble().Nullable()
                .WithColumn("ResponseElevationValueLimit").AsInt32().Nullable()
                .WithColumn("ResponseElevationLimit").AsInt32().Nullable()
                .WithColumn("ActivationWatcher").AsInt32().Nullable()
                .WithColumn("ResponseElevationValueGatewayLimit").AsInt32().Nullable();

            Create.Index().OnTable("EntityAnalysisModelProcessingCounter")
                .OnColumn("EntityAnalysisModelId").Ascending();
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelProcessingCounter");
        }
    }
}