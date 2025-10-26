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
    [Migration(20220429124918)]
    public class AddCaseWorkflowXPathTable : Migration
    {
        public override void Up()
        {
            Create.Table("CaseWorkflowXPath")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("XPath").AsString().Nullable()
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
                .WithColumn("ConditionalRegularExpressionFormatting").AsByte().Nullable()
                .WithColumn("ConditionalFormatForeColor").AsString().Nullable()
                .WithColumn("ConditionalFormatBackColor").AsString().Nullable()
                .WithColumn("RegularExpression").AsString().Nullable()
                .WithColumn("ForeRowColorScope").AsString().Nullable()
                .WithColumn("BackRowColorScope").AsString().Nullable()
                .WithColumn("Drill").AsByte().Nullable()
                .WithColumn("BoldLineFormatForeColor").AsString().Nullable()
                .WithColumn("BoldLineFormatBackColor").AsString().Nullable()
                .WithColumn("BoldLineMatched").AsByte().Nullable();

            Create.Index().OnTable("CaseWorkflowXPath")
                .OnColumn("CaseWorkflowId").Ascending()
                .OnColumn("Deleted").Ascending();

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "AccountId",
                XPath = "payload.AccountId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "TxnDateTime",
                XPath = "payload.TxnDateTime",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "Currency",
                XPath = "payload.Currency",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "ResponseCode",
                XPath = "payload.ResponseCode",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff",
                RegularExpression = "[^0]+",
                ForeRowColorScope = 1,
                BackRowColorScope = 0
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "CurrencyAmount",
                XPath = "payload.CurrencyAmount",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "SettlementAmount",
                XPath = "payload.SettlementAmount",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "AccountCurrency",
                XPath = "payload.AccountCurrency",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "ChannelId",
                XPath = "payload.ChannelId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "AppVersionCode",
                XPath = "payload.AppVersionCode",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "ServiceCode",
                XPath = "payload.ServiceCode",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "System",
                XPath = "payload.System",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "Brand",
                XPath = "payload.Brand",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "Model",
                XPath = "payload.Model",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "AccountLongitude",
                XPath = "payload.AccountLongitude",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "AccountLatitude",
                XPath = "payload.AccountLatitude",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "OS",
                XPath = "payload.OS",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "Resolution",
                XPath = "payload.Resolution",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "DebuggerAttached",
                XPath = "payload.DebuggerAttached",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "SimulatorAttached",
                XPath = "payload.SimulatorAttached",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "Jailbreak",
                XPath = "payload.Jailbreak",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "MAC",
                XPath = "payload.MAC",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "ToAccountId",
                XPath = "payload.ToAccountId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "ToAccountExternalRef",
                XPath = "payload.ToAccountExternalRef",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "TwoFATypeId",
                XPath = "payload.TwoFATypeId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "TwoFAResponseId",
                XPath = "payload.TwoFAResponseId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "Storage",
                XPath = "payload.Storage",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "TransactionExternalResponseId",
                XPath = "payload.TransactionExternalResponseId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "FingerprintHash",
                XPath = "payload.FingerprintHash",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "BusinessModel",
                XPath = "payload.BusinessModel",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "AmountEUR",
                XPath = "payload.AmountEUR",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "AmountUSD",
                XPath = "payload.AmountUSD",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "AmountUSDRate",
                XPath = "payload.AmountUSDRate",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "AmountGBP",
                XPath = "payload.AmountGBP",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "AmountGBPRate",
                XPath = "payload.AmountGBPRate",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "Is3D",
                XPath = "payload.Is3D",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "OriginalAmount",
                XPath = "payload.OriginalAmount",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "OriginalCurrency",
                XPath = "payload.OriginalCurrency",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "Email",
                XPath = "payload.Email",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "CreditCardHash",
                XPath = "payload.CreditCardHash",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "AcquirerBankName",
                XPath = "payload.AcquirerBankName",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "ActionDate",
                XPath = "payload.ActionDate",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "APMAccountId",
                XPath = "payload.APMAccountId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "BankId",
                XPath = "payload.BankId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "BillingAddress",
                XPath = "payload.BillingAddress",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "BillingCity",
                XPath = "payload.BillingCity",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "BillingCountry",
                XPath = "payload.BillingCountry",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "BillingFirstName",
                XPath = "payload.BillingFirstName",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "BillingLastName",
                XPath = "payload.BillingLastName",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "BillingPhone",
                XPath = "payload.BillingPhone",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "BillingState",
                XPath = "payload.BillingState",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "BillingZip",
                XPath = "payload.BillingZip",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "IsAPM",
                XPath = "payload.IsAPM",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "IsCascaded",
                XPath = "payload.IsCascaded",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "IsCredited",
                XPath = "payload.IsCredited",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "IsCurrencyConverted",
                XPath = "payload.IsCurrencyConverted",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "IsModification",
                XPath = "payload.IsModification",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "IsRebill",
                XPath = "payload.IsRebill",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "TransactionTypeId",
                XPath = "payload.TransactionTypeId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "TransactionResultId",
                XPath = "payload.TransactionResultId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "IP",
                Drill = 1,
                XPath = "payload.IP",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "AmountEURRate",
                XPath = "payload.AmountEURRate",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "DeviceId",
                Drill = 1,
                XPath = "payload.DeviceId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "IsModified",
                XPath = "payload.IsModified",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "OrderId",
                XPath = "payload.OrderId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineFormatForeColor = "#000000",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });

            Insert.IntoTable("CaseWorkflowXPath").Row(new
            {
                CaseWorkflowId = 1,
                Name = "TxnId",
                XPath = "payload.TxnId",
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                BoldLineMatched = 1,
                BoldLineFormatForeColor = "#032cfc",
                BoldLineFormatBackColor = "#ffffff",
                ConditionalFormatForeColor = "#000000",
                ConditionalFormatBackColor = "#ffffff"
            });
        }

        public override void Down()
        {
            Delete.Table("CaseWorkflowXPath");
        }
    }
}