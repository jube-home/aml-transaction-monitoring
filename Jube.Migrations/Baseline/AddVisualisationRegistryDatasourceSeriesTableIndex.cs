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
    [Migration(20220429125024)]
    public class AddVisualisationRegistryDatasourceSeriesTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("VisualisationRegistryDatasourceSeries")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("VisualisationRegistryDatasourceId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("DataTypeId").AsInt32().Nullable();

            Create.Index().OnTable("VisualisationRegistryDatasourceSeries")
                .OnColumn("VisualisationRegistryDatasourceId");

            Insert.IntoTable("VisualisationRegistryDatasourceSeries").Row(new
            {
                VisualisationRegistryDatasourceId = 1,
                Name = "ActivationRuleName",
                DataTypeId = 1
            });

            Insert.IntoTable("VisualisationRegistryDatasourceSeries").Row(new
            {
                VisualisationRegistryDatasourceId = 1,
                Name = "Frequency",
                DataTypeId = 2
            });

            Insert.IntoTable("VisualisationRegistryDatasourceSeries").Row(new
            {
                VisualisationRegistryDatasourceId = 1,
                Name = "PercentageContribution",
                DataTypeId = 3
            });

            Insert.IntoTable("VisualisationRegistryDatasourceSeries").Row(new
            {
                VisualisationRegistryDatasourceId = 2,
                Name = "ActivationRuleName",
                DataTypeId = 1
            });

            Insert.IntoTable("VisualisationRegistryDatasourceSeries").Row(new
            {
                VisualisationRegistryDatasourceId = 2,
                Name = "Frequency",
                DataTypeId = 2
            });

            Insert.IntoTable("VisualisationRegistryDatasourceSeries").Row(new
            {
                VisualisationRegistryDatasourceId = 2,
                Name = "PercentageContribution",
                DataTypeId = 3
            });

            Insert.IntoTable("VisualisationRegistryDatasourceSeries").Row(new
            {
                VisualisationRegistryDatasourceId = 3,
                Name = "MCC",
                DataTypeId = 1
            });

            Insert.IntoTable("VisualisationRegistryDatasourceSeries").Row(new
            {
                VisualisationRegistryDatasourceId = 3,
                Name = "Frequency",
                DataTypeId = 2
            });

            Insert.IntoTable("VisualisationRegistryDatasourceSeries").Row(new
            {
                VisualisationRegistryDatasourceId = 3,
                Name = "Sum",
                DataTypeId = 3
            });

            Insert.IntoTable("VisualisationRegistryDatasourceSeries").Row(new
            {
                VisualisationRegistryDatasourceId = 4,
                Name = "MCC",
                DataTypeId = 1
            });

            Insert.IntoTable("VisualisationRegistryDatasourceSeries").Row(new
            {
                VisualisationRegistryDatasourceId = 4,
                Name = "Frequency",
                DataTypeId = 2
            });

            Insert.IntoTable("VisualisationRegistryDatasourceSeries").Row(new
            {
                VisualisationRegistryDatasourceId = 4,
                Name = "Sum",
                DataTypeId = 3
            });
        }

        public override void Down()
        {
            Delete.Table("VisualisationRegistryDatasourceSeries");
        }
    }
}