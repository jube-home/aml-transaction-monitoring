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
    [Migration(20220429125025)]
    public class AddVisualisationRegistryDatasourceTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("VisualisationRegistryDatasource")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("VisualisationRegistryId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("VisualisationTypeId").AsByte().Nullable()
                .WithColumn("Command").AsString().Nullable()
                .WithColumn("VisualisationText").AsString().Nullable()
                .WithColumn("Priority").AsInt32().Nullable()
                .WithColumn("IncludeGrid").AsByte().Nullable()
                .WithColumn("IncludeDisplay").AsByte().Nullable()
                .WithColumn("ColumnSpan").AsInt32().Nullable()
                .WithColumn("RowSpan").AsInt32().Nullable();

            Create.Index().OnTable("VisualisationRegistryDatasource")
                .OnColumn("VisualisationRegistryId");

            Insert.IntoTable("VisualisationRegistryDatasource").Row(new
            {
                VisualisationRegistryId = 1,
                Name = "ExamplePie",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                VisualisationTypeId = 1,
                Command = "select " + Environment.NewLine +
                          "\"ActivationRuleName\"," + Environment.NewLine +
                          "\"Frequency\"," + Environment.NewLine +
                          "\"PercentageContribution\" " + Environment.NewLine +
                          "from \"ExampleCaseVolumeEntry\" " + Environment.NewLine +
                          "where \"Frequency\" > @Frequency_Greater_Than " + Environment.NewLine +
                          "and \"PercentageContribution\" > @Percentage_Contribution_Greater_Than " +
                          Environment.NewLine +
                          "order by \"Frequency\" desc",
                Priority = 1,
                IncludeGrid = 0,
                IncludeDisplay = 1,
                ColumnSpan = 3,
                RowSpan = 2,
                VisualisationText = "({" + Environment.NewLine +
                                    "    legend: {" + Environment.NewLine +
                                    "        position: \"bottom\"" + Environment.NewLine +
                                    "    }," + Environment.NewLine +
                                    "    series: [{" + Environment.NewLine +
                                    "        type: \"pie\"," + Environment.NewLine +
                                    "        field: \"Frequency\"," + Environment.NewLine +
                                    "        categoryField: \"ActivationRuleName\"" + Environment.NewLine +
                                    "    }]," + Environment.NewLine +
                                    "    seriesColors: [\"#03a9f4\", \"#ff9800\", \"#fad84a\", \"#4caf50\"]," +
                                    Environment.NewLine +
                                    "    tooltip: {" + Environment.NewLine +
                                    "        visible: true," + Environment.NewLine +
                                    "        template: \"${ category } - ${ value }%\"" + Environment.NewLine +
                                    "    }" + Environment.NewLine +
                                    "})" + Environment.NewLine
            });

            Insert.IntoTable("VisualisationRegistryDatasource").Row(new
            {
                VisualisationRegistryId = 1,
                Name = "ExampleBar",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                VisualisationTypeId = 1,
                Command = "select " + Environment.NewLine +
                          "\"ActivationRuleName\"," + Environment.NewLine +
                          "\"Frequency\"," + Environment.NewLine +
                          "\"PercentageContribution\" " + Environment.NewLine +
                          "from \"ExampleCaseVolumeEntry\" " + Environment.NewLine +
                          "where \"Frequency\" > @Frequency_Greater_Than " + Environment.NewLine +
                          "and \"PercentageContribution\" > @Percentage_Contribution_Greater_Than " +
                          Environment.NewLine +
                          "order by \"Frequency\" desc",
                Priority = 2,
                IncludeGrid = 0,
                IncludeDisplay = 1,
                ColumnSpan = 3,
                RowSpan = 2,
                VisualisationText = "({" + Environment.NewLine +
                                    "    legend: {" + Environment.NewLine +
                                    "        visible: false" + Environment.NewLine +
                                    "    }," + Environment.NewLine +
                                    "    seriesDefaults: {" + Environment.NewLine +
                                    "        type: \"bar\"" + Environment.NewLine +
                                    "    }," + Environment.NewLine +
                                    "    series: [" + Environment.NewLine +
                                    "        {" + Environment.NewLine +
                                    "        field: \"Frequency\"," + Environment.NewLine +
                                    "        categoryField: \"ActivationRuleName\"," + Environment.NewLine +
                                    "        name: \"Frequency\"" + Environment.NewLine +
                                    "    }]," + Environment.NewLine +
                                    "    valueAxis: {" + Environment.NewLine +
                                    "        max: 140000," + Environment.NewLine +
                                    "        line: {" + Environment.NewLine +
                                    "            visible: false" + Environment.NewLine +
                                    "        }," + Environment.NewLine +
                                    "        minorGridLines: {" + Environment.NewLine +
                                    "            visible: true" + Environment.NewLine +
                                    "        }," + Environment.NewLine +
                                    "        labels: {" + Environment.NewLine +
                                    "            rotation: \"auto\"" + Environment.NewLine +
                                    "        }" + Environment.NewLine +
                                    "    }," + Environment.NewLine +
                                    "    tooltip: {" + Environment.NewLine +
                                    "        visible: true," + Environment.NewLine +
                                    "        template: \"#= series.name #: #= value #\"" + Environment.NewLine +
                                    "    }" + Environment.NewLine +
                                    "})" + Environment.NewLine
            });

            Insert.IntoTable("VisualisationRegistryDatasource").Row(new
            {
                VisualisationRegistryId = 2,
                Name = "ExamplePie",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                VisualisationTypeId = 1,
                Command = "select " + Environment.NewLine +
                          "\"MCC\"," + Environment.NewLine +
                          "\"Frequency\"," + Environment.NewLine +
                          "\"Sum\" " + Environment.NewLine +
                          "from \"ExampleCustomerCaseManagement\" " + Environment.NewLine +
                          "where \"AccountId\" = @AccountId " + Environment.NewLine +
                          "order by \"Frequency\" desc",
                Priority = 1,
                IncludeGrid = 0,
                IncludeDisplay = 1,
                ColumnSpan = 3,
                RowSpan = 2,
                VisualisationText = "({" + Environment.NewLine +
                                    "    legend: {" + Environment.NewLine +
                                    "        position: \"bottom\"" + Environment.NewLine +
                                    "    }," + Environment.NewLine +
                                    "    series: [{" + Environment.NewLine +
                                    "        type: \"pie\"," + Environment.NewLine +
                                    "        field: \"Frequency\"," + Environment.NewLine +
                                    "        categoryField: \"MCC\"" + Environment.NewLine +
                                    "    }]," + Environment.NewLine +
                                    "    seriesColors: [\"#03a9f4\", \"#ff9800\", \"#fad84a\", \"#4caf50\"]," +
                                    Environment.NewLine +
                                    "    tooltip: {" + Environment.NewLine +
                                    "        visible: true," + Environment.NewLine +
                                    "        template: \"${ category } - ${ value }%\"" + Environment.NewLine +
                                    "    }" + Environment.NewLine +
                                    "})" + Environment.NewLine
            });

            Insert.IntoTable("VisualisationRegistryDatasource").Row(new
            {
                VisualisationRegistryId = 2,
                Name = "ExampleBar",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                VisualisationTypeId = 1,
                Command = "select " + Environment.NewLine +
                          "\"MCC\"," + Environment.NewLine +
                          "\"Frequency\"," + Environment.NewLine +
                          "\"Sum\" " + Environment.NewLine +
                          "from \"ExampleCustomerCaseManagement\" " + Environment.NewLine +
                          "where \"AccountId\" = @AccountId " + Environment.NewLine +
                          "order by \"Frequency\" desc",
                Priority = 2,
                IncludeGrid = 0,
                IncludeDisplay = 1,
                ColumnSpan = 3,
                RowSpan = 2,
                VisualisationText = "({" + Environment.NewLine +
                                    "    legend: {" + Environment.NewLine +
                                    "        visible: false" + Environment.NewLine +
                                    "    }," + Environment.NewLine +
                                    "    seriesDefaults: {" + Environment.NewLine +
                                    "        type: \"bar\"" + Environment.NewLine +
                                    "    }," + Environment.NewLine +
                                    "    series: [" + Environment.NewLine +
                                    "        {" + Environment.NewLine +
                                    "        field: \"Sum\"," + Environment.NewLine +
                                    "        categoryField: \"MCC\"," + Environment.NewLine +
                                    "        name: \"Sum\"" + Environment.NewLine +
                                    "    }]," + Environment.NewLine +
                                    "    valueAxis: {" + Environment.NewLine +
                                    "        max: 140000," + Environment.NewLine +
                                    "        line: {" + Environment.NewLine +
                                    "            visible: false" + Environment.NewLine +
                                    "        }," + Environment.NewLine +
                                    "        minorGridLines: {" + Environment.NewLine +
                                    "            visible: true" + Environment.NewLine +
                                    "        }," + Environment.NewLine +
                                    "        labels: {" + Environment.NewLine +
                                    "            rotation: \"auto\"" + Environment.NewLine +
                                    "        }" + Environment.NewLine +
                                    "    }," + Environment.NewLine +
                                    "    tooltip: {" + Environment.NewLine +
                                    "        visible: true," + Environment.NewLine +
                                    "        template: \"#= series.name #: #= value #\"" + Environment.NewLine +
                                    "    }" + Environment.NewLine +
                                    "})" + Environment.NewLine
            });
        }

        public override void Down()
        {
            Delete.Table("VisualisationRegistryDatasource");
        }
    }
}