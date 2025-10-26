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
    [Migration(20220429125029)]
    public class AddEntityAnalysisModelAbstractionRuleTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelAbstractionRule")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("BuilderRuleScript").AsString().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("SearchKey").AsString().Nullable()
                .WithColumn("SearchFunctionTypeId").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("SearchInterval").AsString().Nullable()
                .WithColumn("SearchValue").AsInt32().Nullable()
                .WithColumn("SearchFunctionKey").AsString().Nullable()
                .WithColumn("Json").AsCustom("jsonb").Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Search").AsByte().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("ReportTable").AsByte().Nullable()
                .WithColumn("Offset").AsByte().Nullable()
                .WithColumn("OffsetTypeId").AsByte().Nullable()
                .WithColumn("OffsetValue").AsInt32().Nullable()
                .WithColumn("CoderRuleScript").AsString().Nullable()
                .WithColumn("RuleScriptTypeId").AsByte().Nullable();

            Create.Index().OnTable("EntityAnalysisModelAbstractionRule")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("Deleted").Ascending();

            var builderRuleScriptApproved = "If (Payload.ResponseCode = \"0\") Then " + Environment.NewLine +
                                            "   Return True " + Environment.NewLine +
                                            "End If";

            var jsonApproved = "{\"not\": false, \"rules\": [{\"id\": \"Payload.ResponseCode\", \"type\": \"string\", " +
                               "\"field\": \"Payload.ResponseCode\", \"input\": \"text\", \"value\": \"0\", " +
                               "\"operator\": \"equal\"}], \"valid\": true, \"condition\": \"AND\"}";

            Insert.IntoTable("EntityAnalysisModelAbstractionRule").Row(new
            {
                EntityAnalysisModelId = 1,
                BuilderRuleScript = builderRuleScriptApproved,
                CoderRuleScript = "Return True",
                RuleScriptTypeId = 1,
                Name = "ResponseCodeEqual0Volume",
                Search = 1,
                SearchFunctionTypeId = 3,
                SearchFunctionKey = "CurrencyAmount",
                SearchInterval = "h",
                SearchValue = 1,
                SearchKey = "AccountId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                ResponsePayload = 1,
                Json = jsonApproved
            });

            var builderRuleScriptDeclined = "If (NOT ( Payload.ResponseCode = \"0\" )) Then " + Environment.NewLine +
                                            "   Return True" + Environment.NewLine +
                                            "End If";

            var jsonDeclined = "{\"not\": true, \"rules\": [{\"id\": \"Payload.ResponseCode\", \"type\": \"string\", " +
                               "\"field\": \"Payload.ResponseCode\", \"input\": \"text\", \"value\": \"0\", " +
                               "\"operator\": \"equal\"}], \"valid\": true, \"condition\": \"AND\"}";

            Insert.IntoTable("EntityAnalysisModelAbstractionRule").Row(new
            {
                EntityAnalysisModelId = 1,
                BuilderRuleScript = builderRuleScriptDeclined,
                CoderRuleScript = "Return True",
                RuleScriptTypeId = 1,
                Name = "NotResponseCodeEqual0Volume",
                Search = 1,
                SearchFunctionTypeId = 3,
                SearchFunctionKey = "CurrencyAmount",
                SearchInterval = "h",
                SearchValue = 1,
                SearchKey = "AccountId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                ResponsePayload = 1,
                Json = jsonDeclined
            });
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelAbstractionRule");
        }
    }
}