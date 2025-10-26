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
    [Migration(20220429125014)]
    public class AddSanctionEntrySourceTable : Migration
    {
        public override void Up()
        {
            Create.Table("SanctionEntrySource")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Severity").AsByte().Nullable()
                .WithColumn("DirectoryLocation").AsString().Nullable()
                .WithColumn("Delimiter").AsString().Nullable()
                .WithColumn("MultiPartStringIndex").AsString().Nullable()
                .WithColumn("ReferenceIndex").AsByte().Nullable()
                .WithColumn("EnableDirectoryLocation").AsByte().Nullable()
                .WithColumn("EnableHttpLocation").AsByte().Nullable()
                .WithColumn("HttpLocation").AsString().Nullable()
                .WithColumn("Skip").AsByte().Nullable();

            Insert.IntoTable("SanctionEntrySource").Row(new
            {
                Name = "SDN",
                Severity = 1,
                DirectoryLocation = "",
                Delimiter = ",",
                MultiPartStringIndex = "1",
                ReferenceIndex = 0,
                EnableDirectoryLocation = 0,
                EnableHttpLocation = 1,
                HttpLocation = "https://www.treasury.gov/ofac/downloads/sdn.csv",
                Skip = 0
            });

            Insert.IntoTable("SanctionEntrySource").Row(new
            {
                Name = "BOE",
                Severity = 1,
                DirectoryLocation = "",
                Delimiter = ",",
                MultiPartStringIndex = "0,1,2,3,4,5",
                ReferenceIndex = 28,
                EnableDirectoryLocation = 0,
                EnableHttpLocation = 1,
                HttpLocation = "https://ofsistorage.blob.core.windows.net/publishlive/ConList.csv",
                Skip = 2
            });

            Insert.IntoTable("SanctionEntrySource").Row(new
            {
                Name = "EU",
                Severity = 1,
                DirectoryLocation = "",
                Delimiter = ";",
                MultiPartStringIndex = "17",
                ReferenceIndex = 8,
                EnableDirectoryLocation = 0,
                EnableHttpLocation = 1,
                HttpLocation =
                    "https://webgate.ec.europa.eu/europeaid/fsd/fsf/public/files/csvFullSanctionsList/content?token=dG9rZW4tMjAxNw",
                Skip = 1
            });

            Insert.IntoTable("SanctionEntrySource").Row(new
            {
                Name = "SDN ALT",
                Severity = 1,
                DirectoryLocation = "",
                Delimiter = ";",
                MultiPartStringIndex = "3",
                ReferenceIndex = 8,
                EnableDirectoryLocation = 0,
                EnableHttpLocation = 1,
                HttpLocation = "https://www.treasury.gov/ofac/downloads/alt.csv",
                Skip = 0
            });
        }

        public override void Down()
        {
            Delete.Table("SanctionEntrySource");
        }
    }
}