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

    [Migration(20261306095500)]
    public class FixMissingGuidIssueSessionCaseJournal : Migration
    {

        public override void Up()
        {
            UpdateDataInSessionCaseJournal();
        }

        public override void Down()
        {

        }

        private void UpdateDataInSessionCaseJournal()
        {
            Alter.Table("SessionCaseJournal").AddColumn("CaseWorkflowGuid").AsGuid().Nullable();

            Execute.Sql(
                """UPDATE "SessionCaseJournal" b SET "CaseWorkflowGuid" = a."Guid" FROM "CaseWorkflow" a WHERE a."Id" = b."CaseWorkflowId";""");

            Delete.Column("CaseWorkflowId").FromTable("SessionCaseJournal");
            Create.Index().OnTable("SessionCaseJournal").OnColumn("CaseWorkflowGuid").Ascending();
        }
    }
}
