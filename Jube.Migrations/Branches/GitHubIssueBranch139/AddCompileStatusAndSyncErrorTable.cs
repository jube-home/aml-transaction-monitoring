/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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

    [Migration(20263107170000)]
    public class AddCompileStatusAndSyncErrorTable : Migration
    {
        public override void Up()
        {
            Alter.Table("EntityAnalysisModelInlineFunction").AddColumn("Compiled").AsByte().Nullable();
            Alter.Table("EntityAnalysisModelInlineFunction").AddColumn("CompileError").AsString().Nullable();

            Alter.Table("EntityAnalysisInlineScript").AddColumn("Compiled").AsByte().Nullable();
            Alter.Table("EntityAnalysisInlineScript").AddColumn("CompileError").AsString().Nullable();

            Alter.Table("EntityAnalysisModelAbstractionCalculation").AddColumn("Compiled").AsByte().Nullable();
            Alter.Table("EntityAnalysisModelAbstractionCalculation").AddColumn("CompileError").AsString().Nullable();

            Alter.Table("EntityAnalysisModelActivationRule").AddColumn("Compiled").AsByte().Nullable();
            Alter.Table("EntityAnalysisModelActivationRule").AddColumn("CompileError").AsString().Nullable();

            Alter.Table("EntityAnalysisModelGatewayRule").AddColumn("Compiled").AsByte().Nullable();
            Alter.Table("EntityAnalysisModelGatewayRule").AddColumn("CompileError").AsString().Nullable();

            Alter.Table("EntityAnalysisModelAbstractionRule").AddColumn("Compiled").AsByte().Nullable();
            Alter.Table("EntityAnalysisModelAbstractionRule").AddColumn("CompileError").AsString().Nullable();

            Create.Table("EntityAnalysisModelSynchronisationError")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("SynchronisationStepId").AsInt32().Nullable()
                .WithColumn("ErrorMessage").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable();
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelSynchronisationError");

            Delete.Column("CompileError").FromTable("EntityAnalysisModelAbstractionRule");
            Delete.Column("Compiled").FromTable("EntityAnalysisModelAbstractionRule");

            Delete.Column("CompileError").FromTable("EntityAnalysisModelGatewayRule");
            Delete.Column("Compiled").FromTable("EntityAnalysisModelGatewayRule");

            Delete.Column("CompileError").FromTable("EntityAnalysisModelActivationRule");
            Delete.Column("Compiled").FromTable("EntityAnalysisModelActivationRule");

            Delete.Column("CompileError").FromTable("EntityAnalysisModelAbstractionCalculation");
            Delete.Column("Compiled").FromTable("EntityAnalysisModelAbstractionCalculation");

            Delete.Column("CompileError").FromTable("EntityAnalysisInlineScript");
            Delete.Column("Compiled").FromTable("EntityAnalysisInlineScript");

            Delete.Column("CompileError").FromTable("EntityAnalysisModelInlineFunction");
            Delete.Column("Compiled").FromTable("EntityAnalysisModelInlineFunction");
        }
    }
}
