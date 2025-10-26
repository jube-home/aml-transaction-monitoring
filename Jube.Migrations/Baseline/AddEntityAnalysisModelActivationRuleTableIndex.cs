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
    [Migration(20220429124926)]
    public class AddEntityAnalysisModelActivationRuleTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelActivationRule")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("BuilderRuleScript").AsString().Nullable()
                .WithColumn("Json").AsCustom("jsonb").Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("ResponseElevation").AsFloat().Nullable()
                .WithColumn("CaseWorkflowId").AsInt32().Nullable()
                .WithColumn("EnableCaseWorkflow").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("EntityAnalysisModelTtlCounterId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelIdTtlCounter").AsInt32().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("EnableTtlCounter").AsByte().Nullable()
                .WithColumn("ResponseElevationContent").AsString().Nullable()
                .WithColumn("SendToActivationWatcher").AsByte().Nullable()
                .WithColumn("ResponseElevationForeColor").AsString().Nullable()
                .WithColumn("ResponseElevationBackColor").AsString().Nullable()
                .WithColumn("CaseWorkflowStatusId").AsInt32().Nullable()
                .WithColumn("ActivationSample").AsDouble().Nullable()
                .WithColumn("ActivationCounter").AsInt64().Nullable()
                .WithColumn("ActivationCounterDate").AsDateTime2().Nullable()
                .WithColumn("ResponseElevationRedirect").AsString().Nullable()
                .WithColumn("ReviewStatusId").AsByte().Nullable()
                .WithColumn("ReportTable").AsByte().Nullable()
                .WithColumn("EnableNotification").AsByte().Nullable()
                .WithColumn("NotificationTypeId").AsByte().Nullable()
                .WithColumn("NotificationDestination").AsString().Nullable()
                .WithColumn("NotificationSubject").AsString().Nullable()
                .WithColumn("NotificationBody").AsString().Nullable()
                .WithColumn("CoderRuleScript").AsString().Nullable()
                .WithColumn("RuleScriptTypeId").AsString().Nullable()
                .WithColumn("EnableResponseElevation").AsByte().Nullable()
                .WithColumn("CaseKey").AsString().Nullable()
                .WithColumn("ResponseElevationKey").AsString().Nullable()
                .WithColumn("EnableBypass").AsByte().Nullable()
                .WithColumn("BypassSuspendInterval").AsString().Nullable()
                .WithColumn("BypassSuspendValue").AsInt32().Nullable()
                .WithColumn("BypassSuspendSample").AsDouble().Nullable()
                .WithColumn("Visible").AsByte().Nullable()
                .WithColumn("EnableReprocessing").AsByte().Nullable()
                .WithColumn("EnableSuppression").AsByte().Nullable();

            Create.Index().OnTable("EntityAnalysisModelActivationRule")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("Deleted").Ascending();

            Insert.IntoTable("EntityAnalysisModelActivationRule").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "IncrementTtlCounterAll",
                BuilderRuleScript = "Return False",
                CoderRuleScript = "Return True",
                RuleScriptTypeId = 2,
                ResponseElevation = 0,
                EnableCaseWorkflow = 0,
                CaseWorkflowId = 0,
                CaseKey = "",
                Active = 1,
                Version = 1,
                CreatedUser = "Administrator",
                CreatedDate = DateTime.Now,
                EnableTtlCounter = 1,
                EntityAnalysisModelTtlCounterId = 1,
                EntityAnalysisModelIdTtlCounter = 1,
                ResponsePayload = 0,
                EnableNotification = 0,
                EnableResponseElevation = 0,
                ResponseElevationKey = "",
                EnableBypass = 0,
                Visible = 0,
                EnableReprocessing = 0,
                EnableSuppression = 0,
                ActivationSample = 1,
                ReviewStatusId = 4
            });

            Insert.IntoTable("EntityAnalysisModelActivationRule").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "ThresholdTtlCounterAll",
                BuilderRuleScript = "If (TTLCounter.TtlCounterAll > 5) Then " + Environment.NewLine +
                                    "  Return True" + Environment.NewLine +
                                    "End If",
                CoderRuleScript = "Return False",
                RuleScriptTypeId = 1,
                Json = "{\"not\": false, \"rules\": [{\"id\": \"TTLCounter.TtlCounterAll\", " +
                       "\"type\": \"double\", \"field\": \"TTLCounter.TtlCounterAll\", " +
                       "\"input\": \"number\", \"value\": 5, \"operator\": \"greater\"}], " +
                       "\"valid\": true, \"condition\": \"AND\"}",
                ResponseElevation = 1,
                EnableCaseWorkflow = 1,
                CaseWorkflowId = 1,
                CaseKey = "AccountId",
                Active = 1,
                Version = 1,
                CreatedUser = "Administrator",
                CreatedDate = DateTime.Now,
                EnableTtlCounter = 0,
                EntityAnalysisModelTtlCounterId = 1,
                EntityAnalysisModelIdTtlCounter = 1,
                ResponsePayload = 1,
                EnableNotification = 0,
                EnableResponseElevation = 1,
                ResponseElevationKey = "AccountId",
                EnableBypass = 0,
                Visible = 1,
                EnableReprocessing = 1,
                EnableSuppression = 1,
                ActivationSample = 1,
                ReviewStatusId = 4,
                ResponseElevationContent = "Declined for \"ThresholdTtlCounterAll\"",
                ResponseElevationForeColor = "#fb0707",
                ResponseElevationBackColor = "#f5f2cb",
                SendToActivationWatcher = 1,
                CaseWorkflowStatusId = 1,
                ResponseElevationRedirect = "https://www.jube.io"
            });

            Insert.IntoTable("EntityAnalysisModelActivationRule").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "ThresholdSanctionsDistance",
                BuilderRuleScript = "If (Sanction.FuzzyMatchDistance2JoinedName < 1) Then " + Environment.NewLine +
                                    "  Return True" + Environment.NewLine +
                                    "End If",
                CoderRuleScript = "Return False",
                RuleScriptTypeId = 1,
                Json = "{\"not\": false, \"rules\": [{\"id\": \"Sanction.FuzzyMatchDistance2JoinedName\", " +
                       "\"type\": \"double\", \"field\": \"sanction.FuzzyMatchDistance2JoinedName\", " +
                       "\"input\": \"number\", \"value\": 1, \"operator\": \"less\"}], " +
                       "\"valid\": true, \"condition\": \"AND\"}",
                ResponseElevation = 2,
                EnableCaseWorkflow = 1,
                CaseWorkflowId = 1,
                CaseKey = "AccountId",
                Active = 1,
                Version = 1,
                CreatedUser = "Administrator",
                CreatedDate = DateTime.Now,
                EnableTtlCounter = 0,
                EntityAnalysisModelTtlCounterId = 1,
                EntityAnalysisModelIdTtlCounter = 1,
                ResponsePayload = 1,
                EnableNotification = 0,
                EnableResponseElevation = 1,
                ResponseElevationKey = "AccountId",
                EnableBypass = 0,
                Visible = 1,
                EnableReprocessing = 1,
                EnableSuppression = 1,
                ActivationSample = 1,
                ReviewStatusId = 4,
                ResponseElevationContent = "Declined for \"ThresholdSanctionsDistance\"",
                ResponseElevationForeColor = "#fb0707",
                ResponseElevationBackColor = "#f5f2cb",
                SendToActivationWatcher = 1,
                CaseWorkflowStatusId = 1,
                ResponseElevationRedirect = "https://www.jube.io"
            });

            Insert.IntoTable("EntityAnalysisModelActivationRule").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "AllIPDenyList",
                BuilderRuleScript = "If (( List.IPDenyList.contains(Payload.IP))) Then" + Environment.NewLine +
                                    "  Return True" + Environment.NewLine +
                                    "End If",
                CoderRuleScript = "Return False",
                RuleScriptTypeId = 1,
                Json = "{\"rules\": [{\"id\": \"List.IPDenyList\", \"type\": \"string\", \"field\": " +
                       "\"List.IPDenyList\", \"input\": \"select\", \"value\": \"Payload.IP\", \"operator\": \"has\"}], " +
                       "\"valid\": true, \"condition\": \"AND\"}",
                ResponseElevation = 2,
                EnableCaseWorkflow = 1,
                CaseWorkflowId = 1,
                CaseKey = "AccountId",
                Active = 1,
                Version = 1,
                CreatedUser = "Administrator",
                CreatedDate = DateTime.Now,
                EnableTtlCounter = 0,
                EntityAnalysisModelTtlCounterId = 1,
                EntityAnalysisModelIdTtlCounter = 1,
                ResponsePayload = 1,
                EnableNotification = 0,
                EnableResponseElevation = 1,
                ResponseElevationKey = "AccountId",
                EnableBypass = 0,
                Visible = 1,
                EnableReprocessing = 1,
                EnableSuppression = 1,
                ActivationSample = 1,
                ReviewStatusId = 4,
                ResponseElevationContent = "Declined for \"AllIPDenyList\"",
                ResponseElevationForeColor = "#fb0707",
                ResponseElevationBackColor = "#f5f2cb",
                SendToActivationWatcher = 1,
                CaseWorkflowStatusId = 1,
                ResponseElevationRedirect = "https://www.jube.io"
            });

            Insert.IntoTable("EntityAnalysisModelActivationRule").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "VolumeThresholdByAccountId",
                BuilderRuleScript = "Return False",
                CoderRuleScript =
                    "If (Abstraction.ResponseCodeEqual0Volume > Dictionary.VolumeThresholdByAccountId) Then" +
                    Environment.NewLine +
                    "   Return True" + Environment.NewLine +
                    "End If",
                RuleScriptTypeId = 2,
                ResponseElevation = 2,
                EnableCaseWorkflow = 1,
                CaseWorkflowId = 1,
                CaseKey = "AccountId",
                Active = 1,
                Version = 1,
                CreatedUser = "Administrator",
                CreatedDate = DateTime.Now,
                EnableTtlCounter = 0,
                EntityAnalysisModelTtlCounterId = 1,
                EntityAnalysisModelIdTtlCounter = 1,
                ResponsePayload = 1,
                EnableNotification = 0,
                EnableResponseElevation = 1,
                ResponseElevationKey = "AccountId",
                EnableBypass = 0,
                Visible = 1,
                EnableReprocessing = 1,
                EnableSuppression = 1,
                ActivationSample = 1,
                ReviewStatusId = 4,
                ResponseElevationContent = "Declined for \"VolumeThresholdByAccountId\"",
                ResponseElevationForeColor = "#fb0707",
                ResponseElevationBackColor = "#f5f2cb",
                SendToActivationWatcher = 1,
                CaseWorkflowStatusId = 1,
                ResponseElevationRedirect = "https://www.jube.io"
            });
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelActivationRule");
        }
    }
}