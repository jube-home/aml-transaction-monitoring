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
    [Migration(20220429124930)]
    public class AddEntityAnalysisModelGatewayRuleTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelGatewayRule")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Priority").AsByte().Nullable()
                .WithColumn("BuilderRuleScript").AsString().Nullable()
                .WithColumn("Json").AsCustom("jsonb").Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("MaxResponseElevation").AsDouble().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("ActivationCounter").AsInt64().Nullable()
                .WithColumn("ActivationCounterDate").AsDateTime2().Nullable()
                .WithColumn("CoderRuleScript").AsString().Nullable()
                .WithColumn("RuleScriptTypeId").AsByte().Nullable()
                .WithColumn("GatewaySample").AsDouble().Nullable();

            Create.Index().OnTable("EntityAnalysisModelGatewayRule")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("Deleted").Ascending();

            var builderRuleScript = "If (Payload.CurrencyAmount > 0) Then " + Environment.NewLine +
                                    "   Return True " + Environment.NewLine +
                                    "End If";

            var json = "{\"not\": false, \"rules\": [{\"id\": \"Payload.CurrencyAmount\", \"type\": \"double\", " +
                       "\"field\": \"Payload.CurrencyAmount\", \"input\": \"number\", \"value\": 0, \"operator\": " +
                       "\"greater\"}], \"valid\": true, \"condition\": \"AND\"}";

            Insert.IntoTable("EntityAnalysisModelGatewayRule").Row(new
            {
                EntityAnalysisModelId = 1,
                Priority = 0,
                BuilderRuleScript = builderRuleScript,
                Json = json,
                Name = "CurrencyAmountAllGreaterThan0.",
                CreatedDate = DateTime.Now,
                MaxResponseElevation = 10,
                CoderRuleScript = "Return = True",
                RuleScriptTypeId = 1,
                CreatedUser = "Administrator",
                GatewaySample = 1,
                Version = 1,
                Active = 1
            });
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelGatewayRule");
        }
    }
}