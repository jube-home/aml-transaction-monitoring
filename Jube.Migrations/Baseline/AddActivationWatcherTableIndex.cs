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
    [Migration(20220429124900)]
    public class AddActivationWatcherTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("ActivationWatcher")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("Key").AsString()
                .WithColumn("KeyValue").AsString()
                .WithColumn("Longitude").AsDouble().Nullable()
                .WithColumn("Latitude").AsDouble().Nullable()
                .WithColumn("ActivationRuleSummary").AsString()
                .WithColumn("ResponseElevationContent").AsString()
                .WithColumn("ResponseElevation").AsDouble().Nullable()
                .WithColumn("BackColor").AsString()
                .WithColumn("ForeColor").AsString()
                .WithColumn("CreatedDate").AsDateTime2();

            Create.Index().OnTable("ActivationWatcher")
                .OnColumn("TenantRegistryId");

            Create.Index().OnTable("ActivationWatcher")
                .OnColumn("CreatedDate").Descending()
                .OnColumn("Key").Ascending()
                .OnColumn("KeyValue").Ascending();

            Create.Index().OnTable("ActivationWatcher")
                .OnColumn("CreatedDate").Descending()
                .OnColumn("ResponseElevation").Descending();
        }

        public override void Down()
        {
            Delete.Table("ActivationWatcher");
        }
    }
}