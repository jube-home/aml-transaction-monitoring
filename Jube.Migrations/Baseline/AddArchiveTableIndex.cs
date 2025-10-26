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
using FluentMigrator.Postgres;

namespace Jube.Migrations.Baseline
{
    [Migration(20220429124902)]
    public class AddArchiveTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("Archive")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Json").AsCustom("jsonb").Nullable()
                .WithColumn("EntityAnalysisModelInstanceEntryGuid").AsGuid().Nullable()
                .WithColumn("EntryKeyValue").AsString().Nullable()
                .WithColumn("ResponseElevation").AsDouble().Nullable()
                .WithColumn("EntityAnalysisModelActivationRuleId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("ActivationRuleCount").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("ReferenceDate").AsDateTime2().Nullable();

            Create.Index().OnTable("Archive")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("EntityAnalysisModelActivationRuleId").Ascending()
                .OnColumn("ActivationRuleCount").Ascending();

            Create.Index().OnTable("Archive")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("EntryKeyValue").Ascending();

            Create.Index().OnTable("Archive")
                .OnColumn("CreatedDate").Descending();

            Create.Index().OnTable("Archive")
                .OnColumn("EntityAnalysisModelInstanceEntryGuid").Unique();

            Create.Index().OnTable("Archive")
                .OnColumn("ReferenceDate").Descending();

            Create.Index().OnTable("Archive")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("ReferenceDate").Descending();

            Create.Index().OnTable("Archive")
                .OnColumn("EntryKeyValue").Ascending()
                .OnColumn("EntityAnalysisModelId").Descending();

            Create.Index()
                .OnTable("Archive")
                .OnColumn("Json").Ascending()
                .WithOptions()
                .UsingGin();
        }

        public override void Down()
        {
            Delete.Table("Archive");
        }
    }
}