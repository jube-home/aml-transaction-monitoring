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

namespace Jube.Migrations.Branches.GitHubIssueBranch139
{
    using FluentMigrator;

    [Migration(20263107250000)]
    public class AddReasonIdToSanctionEntryRejection : Migration
    {
        private const int InsufficientFields = 1;
        private const int NoReferenceIndexConfigured = 2;
        private const int ParseError = 3;

        private const string InsufficientFieldsMessage = "Row contains insufficient fields.";
        private const string NoReferenceIndexConfiguredMessage =
            "Sanction Entry Source has no Reference Index configured.";

        public override void Up()
        {
            Alter.Table("SanctionEntryRejection").AddColumn("ReasonId").AsInt32().Nullable();

            Update.Table("SanctionEntryRejection")
                .Set(new { ReasonId = InsufficientFields })
                .Where(new { Reason = InsufficientFieldsMessage });

            Update.Table("SanctionEntryRejection")
                .Set(new { ReasonId = NoReferenceIndexConfigured })
                .Where(new { Reason = NoReferenceIndexConfiguredMessage });

            Execute.Sql(
                $"UPDATE \"SanctionEntryRejection\" SET \"ReasonId\" = {ParseError} WHERE \"ReasonId\" IS NULL");

            Delete.Column("Reason").FromTable("SanctionEntryRejection");
        }

        public override void Down()
        {
        }
    }
}
