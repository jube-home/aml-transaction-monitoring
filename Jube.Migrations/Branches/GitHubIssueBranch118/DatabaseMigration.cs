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

namespace Jube.Migrations.Branches.GitHubIssueBranch118
{
    using FluentMigrator;

    [Migration(20262302085700)]
    public class DatabaseMigration : Migration
    {
        public override void Up()
        {
            UpdateEntityAnalysisModelProcessingCounter();
            UpdateEntityAnalysisModelRequestXpath();
            UpdateEntityAnalysisModelRequestXpathVersion();
            UpdateEntityAnalysisModelTtlCounter();
            MigrateIntToDoubleForCacheTtlCounterEntryRemovalBatchEntry();
            UpdateEntityAnalysisModelActivationRule();
            UpdateEntityAnalysisModelActivationRuleVersion();
            UpdateEntityAnalysisModelGatewayRule();
            UpdateEntityAnalysisModelGatewayRuleVersion();
        }
        
        private void UpdateEntityAnalysisModelGatewayRuleVersion()
        {

            Alter.Table("EntityAnalysisModelGatewayRuleVersion").AddColumn("EvaluationCounter").AsInt64().Nullable();
        }
        
        private void UpdateEntityAnalysisModelGatewayRule()
        {

            Alter.Table("EntityAnalysisModelGatewayRule").AddColumn("EvaluationCounter").AsInt64().Nullable();
        }
        
        private void UpdateEntityAnalysisModelActivationRuleVersion()
        {

            Alter.Table("EntityAnalysisModelActivationRuleVersion").AddColumn("EvaluationCounter").AsInt64().Nullable();
        }
        
        private void UpdateEntityAnalysisModelActivationRule()
        {

            Alter.Table("EntityAnalysisModelActivationRule").AddColumn("EvaluationCounter").AsInt64().Nullable();
        }

        private void UpdateEntityAnalysisModelProcessingCounter()
        {
            Alter.Table("EntityAnalysisModelProcessingCounter").AddColumn("ModelTotalResponseTime").AsInt64().Nullable();
        }

        private void MigrateIntToDoubleForCacheTtlCounterEntryRemovalBatchEntry()
        {
            Execute.Sql("""
                        ALTER TABLE "CacheTtlCounterEntryRemovalBatchEntry" ALTER COLUMN "DecrementCount" TYPE DOUBLE PRECISION USING "DecrementCount"::DOUBLE PRECISION;
                        """);

            Execute.Sql("""
                        ALTER TABLE "CacheTtlCounterEntryRemovalBatchEntry" ALTER COLUMN "RevisedCount" TYPE DOUBLE PRECISION USING "RevisedCount"::DOUBLE PRECISION;
                        """);
        }

        private void UpdateEntityAnalysisModelTtlCounter()
        {
            Alter.Table("EntityAnalysisModelTtlCounter").AddColumn("ResolutionInterval").AsString().Nullable();
            Alter.Table("EntityAnalysisModelTtlCounter").AddColumn("TtlCounterDataValue").AsString().Nullable();
            Alter.Table("EntityAnalysisModelTtlCounter").AddColumn("EnableSum").AsByte().Nullable();

            Update.Table("EntityAnalysisModelTtlCounter").Set(new
            {
                ResolutionInterval = "n",
                EnableSum = 0
            }).AllRows();

            Alter.Table("EntityAnalysisModelTtlCounterVersion").AddColumn("ResolutionInterval").AsString().Nullable();
            Alter.Table("EntityAnalysisModelTtlCounterVersion").AddColumn("TtlCounterDataValue").AsString().Nullable();
            Alter.Table("EntityAnalysisModelTtlCounterVersion").AddColumn("EnableSum").AsByte().Nullable();

        }

        private void UpdateEntityAnalysisModelRequestXpathVersion()
        {
            Alter.Table("EntityAnalysisModelRequestXpathVersion").AddColumn("Cache").AsByte().Nullable();
            Alter.Table("EntityAnalysisModelRequestXpathVersion").AddColumn("CacheIndexId").AsInt32().Nullable();
        }

        private void UpdateEntityAnalysisModelRequestXpath()
        {
            Alter.Table("EntityAnalysisModelRequestXpath").AddColumn("Cache").AsByte().Nullable();
            Alter.Table("EntityAnalysisModelRequestXpath").AddColumn("CacheIndexId").AsInt32().Nullable();

            Execute.Sql("""
                        WITH "Ranked" AS (
                            SELECT "Id",
                                   ROW_NUMBER() OVER (PARTITION BY "EntityAnalysisModelId" ORDER BY "Id") AS "RowNum"
                            FROM "EntityAnalysisModelRequestXpath"
                        )
                        UPDATE "EntityAnalysisModelRequestXpath"
                        SET "CacheIndexId" = "Ranked"."RowNum", "Cache" = 1
                        FROM "Ranked"
                        WHERE "EntityAnalysisModelRequestXpath"."Id" = "Ranked"."Id";
                        """);

            Create.Index().OnTable("EntityAnalysisModelRequestXpath")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("CacheIndexId").Ascending()
                .WithOptions().Unique();
        }

        public override void Down()
        {

        }
    }
}
