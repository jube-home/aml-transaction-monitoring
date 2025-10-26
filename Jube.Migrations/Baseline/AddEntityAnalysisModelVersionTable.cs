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
    [Migration(20220429124952)]
    public class AddEntityAnalysisModelVersionTable : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("EntryXPath").AsString().Nullable()
                .WithColumn("ReferenceDateXPath").AsString().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("EntryName").AsString().Nullable()
                .WithColumn("ReferenceDateName").AsString().Nullable()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("CacheFetchLimit").AsInt32().Nullable()
                .WithColumn("ReferenceDatePayloadLocationTypeId").AsByte().Nullable()
                .WithColumn("MaxResponseElevation").AsDouble().Nullable()
                .WithColumn("MaxResponseElevationInterval").AsString().Nullable()
                .WithColumn("MaxResponseElevationValue").AsInt32().Nullable()
                .WithColumn("MaxResponseElevationThreshold").AsInt32().Nullable()
                .WithColumn("MaxActivationWatcherInterval").AsString().Nullable()
                .WithColumn("MaxActivationWatcherValue").AsInt32().Nullable()
                .WithColumn("MaxActivationWatcherThreshold").AsInt32().Nullable()
                .WithColumn("ActivationWatcherSample").AsDouble().Nullable()
                .WithColumn("PromoteActivationArchive").AsByte().Nullable()
                .WithColumn("EnableCache").AsByte().Nullable()
                .WithColumn("EnableTtlCounter").AsByte().Nullable()
                .WithColumn("EnableRdbmsArchive").AsByte().Nullable()
                .WithColumn("EnableSanctionCache").AsByte().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("EnableActivationWatcher").AsByte().Nullable()
                .WithColumn("EnableResponseElevationLimit").AsByte().Nullable();
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelVersion");
        }
    }
}