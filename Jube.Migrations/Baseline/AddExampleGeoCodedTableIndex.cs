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
    [Migration(20221015100000)]
    public class ExampleGeoCodedTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("ExampleGeoCoded")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("City").AsString().Nullable()
                .WithColumn("Sum").AsDouble().Nullable()
                .WithColumn("Latitude").AsDouble().Nullable()
                .WithColumn("Longitude").AsDouble().Nullable();

            Insert.IntoTable("ExampleGeoCoded").Row(
                new
                {
                    City = "Tokyo",
                    Sum = 2055.69,
                    Latitude = 35.6762,
                    Longitude = 139.6503
                });

            Insert.IntoTable("ExampleGeoCoded").Row(
                new
                {
                    City = "New York",
                    Sum = 1874.39,
                    Latitude = 40.7128,
                    Longitude = 74.0060
                });

            Insert.IntoTable("ExampleGeoCoded").Row(
                new
                {
                    City = "Los Angeles",
                    Sum = 1133.62,
                    Latitude = 34.0522,
                    Longitude = 118.2437
                });

            Insert.IntoTable("ExampleGeoCoded").Row(
                new
                {
                    City = "Seoul",
                    Sum = 926.79,
                    Latitude = 37.5665,
                    Longitude = 126.9780
                });

            Insert.IntoTable("ExampleGeoCoded").Row(
                new
                {
                    City = "London",
                    Sum = 978.40,
                    Latitude = 51.5072,
                    Longitude = 0.1276
                });

            Insert.IntoTable("ExampleGeoCoded").Row(
                new
                {
                    City = "Paris",
                    Sum = 934.16,
                    Latitude = 48.8566,
                    Longitude = 2.3522
                });

            Insert.IntoTable("ExampleGeoCoded").Row(
                new
                {
                    City = "Shanghai",
                    Sum = 633.93,
                    Latitude = 31.2304,
                    Longitude = 121.4737
                });

            Insert.IntoTable("ExampleGeoCoded").Row(
                new
                {
                    City = "Moscow",
                    Sum = 504.80,
                    Latitude = 55.7558,
                    Longitude = 37.6173
                });
        }

        public override void Down()
        {
            Delete.Table("ExampleGeoCoded");
        }
    }
}