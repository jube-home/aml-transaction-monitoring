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
    [Migration(20220429124911)]
    public class AddCaseWorkflowFilterTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("CaseWorkflowFilter")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("FilterSql").AsString().Nullable()
                .WithColumn("FilterJson").AsCustom("jsonb").Nullable()
                .WithColumn("SelectJson").AsCustom("jsonb").Nullable()
                .WithColumn("FilterTokens").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("VisualisationRegistryId").AsInt32().Nullable();

            Create.Index().OnTable("CaseWorkflowFilter")
                .OnColumn("CaseWorkflowId").Ascending()
                .OnColumn("Deleted").Ascending();

            Insert.IntoTable("CaseWorkflowFilter").Row(new
            {
                CaseWorkflowId = 1,
                Name = "OpenCasesNotLockedByCreatedDate",
                FilterSql = "\"Case\".\"ClosedStatusId\" = @1 AND \"Case\".\"Locked\" = @2",
                FilterJson = "{\"rules\": [{\"id\": \"ClosedStatusId\", \"type\": \"integer\", \"field\": " +
                             "\"\\\"Case\\\".\\\"ClosedStatusId\\\"\", \"input\": \"select\", " +
                             "\"value\": 0, \"operator\": \"equal\"}, {\"id\": \"Locked\", " +
                             "\"type\": \"integer\", \"field\": \"\\\"Case\\\".\\\"Locked\\\"\", " +
                             "\"input\": \"radio\", \"value\": 0, \"operator\": \"equal\"}], " +
                             "\"valid\": true, \"condition\": \"AND\"}",
                SelectJson = "{\"rules\": [{\"id\": \"CaseKey\", \"type\": \"string\", \"field\": \"\\\"CaseKey\\\"\", " +
                             "\"input\": \"radio\", \"value\": \"ASC\", \"operator\": \"order\"}, " +
                             "{\"id\": \"CaseKeyValue\", \"type\": \"string\", " +
                             "\"field\": \"\\\"CaseKeyValue\\\"\", " +
                             "\"input\": \"radio\", \"value\": \"ASC\", " +
                             "\"operator\": \"order\"}, {\"id\": \"CaseWorkflowStatusId\", " +
                             "\"type\": \"string\", \"field\": \"\\\"CaseWorkflowStatus\\\".\\\"Id\\\"\", " +
                             "\"input\": \"radio\", \"value\": \"ASC\", \"operator\": \"order\"}, " +
                             "{\"id\": \"Payload.CurrencyAmount\", " +
                             "\"type\": \"string\", \"field\": \"(\\\"Json\\\"-> 'payload' -> 'CurrencyAmount')::double precision\", " +
                             "\"input\": \"radio\", \"value\": \"ASC\", \"operator\": \"order\"}, " +
                             "{\"id\": \"Payload.BusinessModel\", \"type\": \"string\", " +
                             "\"field\": \"(\\\"Json\\\"-> 'payload' -> 'BusinessModel')\", " +
                             "\"input\": \"radio\", \"value\": \"ASC\", \"operator\": \"order\"}, " +
                             "{\"id\": \"Payload.IP\", \"type\": \"string\", " +
                             "\"field\": \"(\\\"Json\\\"-> 'payload' -> 'IP')\", " +
                             "\"input\": \"radio\", \"value\": \"ASC\", \"operator\": \"order\"}, " +
                             "{\"id\": \"Payload.Email\", \"type\": \"string\", " +
                             "\"field\": \"(\\\"Json\\\"-> 'payload' -> 'Email')\", " +
                             "\"input\": \"radio\", \"value\": \"ASC\", \"operator\": \"order\"}, " +
                             "{\"id\": \"Payload.BusinessModel\", \"type\": \"string\", " +
                             "\"field\": \"(\\\"Json\\\"-> 'payload' -> 'BusinessModel')\", " +
                             "\"input\": \"radio\", \"value\": \"ASC\", \"operator\": \"order\"}, " +
                             "{\"id\": \"Payload.BillingAddress\", \"type\": \"string\", " +
                             "\"field\": \"(\\\"Json\\\"-> 'payload' -> 'BillingAddress')\", " +
                             "\"input\": \"radio\", \"value\": \"ASC\", \"operator\": \"order\"}, " +
                             "{\"id\": \"Payload.BillingCountry\", \"type\": \"string\", \"field\": " +
                             "\"(\\\"Json\\\"-> 'payload' -> 'BillingCountry')\", \"input\": \"radio\", " +
                             "\"value\": \"ASC\", \"operator\": \"order\"}, {\"id\": \"CreatedDate\", " +
                             "\"type\": \"string\", \"field\": \"\\\"Case\\\".\\\"CreatedDate\\\"\", " +
                             "\"input\": \"radio\", \"value\": \"DESC\", \"operator\": \"order\"}], " +
                             "\"valid\": true, \"condition\": \"AND\"}",
                FilterTokens = "[0,0]",
                Active = 1,
                Version = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator"
            });
        }

        public override void Down()
        {
            Delete.Table("CaseWorkflowFilter");
        }
    }
}