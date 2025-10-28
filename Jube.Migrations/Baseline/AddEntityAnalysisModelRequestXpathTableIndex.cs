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
    [Migration(20220429124941)]
    public class AddEntityAnalysisModelRequestXpathTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelRequestXpath")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("DataTypeId").AsByte().Nullable()
                .WithColumn("XPath").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("SearchKey").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("SearchKeyCache").AsByte().Nullable()
                .WithColumn("SearchKeyCacheInterval").AsString().Nullable()
                .WithColumn("SearchKeyCacheValue").AsInt32().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("SearchKeyCacheTtlInterval").AsString().Nullable()
                .WithColumn("SearchKeyCacheTtlValue").AsInt32().Nullable()
                .WithColumn("PayloadLocationTypeId").AsByte().Nullable()
                .WithColumn("SearchKeyCacheFetchLimit").AsInt32().Nullable()
                .WithColumn("ReportTable").AsInt32().Nullable()
                .WithColumn("SearchKeyCacheSample").AsInt32().Nullable()
                .WithColumn("DefaultValue").AsString().Nullable()
                .WithColumn("EnableSuppression").AsByte().Nullable();

            Create.Index().OnTable("EntityAnalysisModelRequestXpath")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("Deleted").Ascending();

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "AccountId",
                XPath = "$.AccountId",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 1,
                ResponsePayload = 1,
                SearchKey = 1,
                PayloadLocationTypeId = 1,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "Test1"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "TxnDateTime",
                XPath = "$.TxnDateTime",
                DataTypeId = 4,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "2022-08-19T21:41:37.247"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "Currency",
                XPath = "$.Currency",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "826"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "ResponseCode",
                XPath = "$.ResponseCode",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "1"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "CurrencyAmount",
                XPath = "$.CurrencyAmount",
                DataTypeId = 3,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "123.45"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "SettlementAmount",
                XPath = "$.SettlementAmount",
                DataTypeId = 3,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "123.45"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "AccountCurrency",
                XPath = "$.AccountCurrency",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "566"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "ChannelId",
                XPath = "$.ChannelId",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "1"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "AppVersionCode",
                XPath = "$.AppVersionCode",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "12.34"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "ServiceCode",
                XPath = "$.ServiceCode",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "DID"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "System",
                XPath = "$.System",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "Android"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "Brand",
                XPath = "$.Brand",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "ZTE"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "Model",
                XPath = "$.Model",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "Barby"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "AccountLongitude",
                XPath = "$.AccountLongitude",
                DataTypeId = 6,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "36.1408"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "AccountLatitude",
                XPath = "$.AccountLatitude",
                DataTypeId = 7,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "5.3536"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "OS",
                XPath = "$.OS",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "Lollypop"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "Resolution",
                XPath = "$.Resolution",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "720*1280"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "DebuggerAttached",
                XPath = "$.DebuggerAttached",
                DataTypeId = 5,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "true"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "SimulatorAttached",
                XPath = "$.SimulatorAttached",
                DataTypeId = 5,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "true"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "Jailbreak",
                XPath = "$.Jailbreak",
                DataTypeId = 5,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "false"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "MAC",
                XPath = "$.MAC",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "94:23:44f:2:d3"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "ToAccountId",
                XPath = "$.ToAccountId",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "Test2"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "ToAccountExternalRef",
                XPath = "$.ToAccountExternalRef",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "ChurchmanR"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "TwoFATypeId",
                XPath = "$.TwoFATypeId",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "SMS"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "TwoFAResponseId",
                XPath = "$.TwoFAResponseId",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "1"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "Storage",
                XPath = "$.Storage",
                DataTypeId = 5,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "true"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "TransactionExternalResponseId",
                XPath = "$.TransactionExternalResponseId",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "0"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "FingerprintHash",
                XPath = "$.FingerprintHash",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "jhjkhjkhsjh2hjhjkhj2k"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "BusinessModel",
                XPath = "$.BusinessModel",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "Travel"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "AmountEUR",
                XPath = "$.AmountEUR",
                DataTypeId = 3,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "100.00"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "AmountUSD",
                XPath = "$.AmountUSD",
                DataTypeId = 3,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "113.05"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "AmountUSDRate",
                XPath = "$.AmountUSDRate",
                DataTypeId = 3,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "1.1305502954"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "AmountGBP",
                XPath = "$.AmountGBP",
                DataTypeId = 3,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "86.5866"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "AmountGBPRate",
                XPath = "$.AmountGBPRate",
                DataTypeId = 3,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "0.8658658602"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "Is3D",
                XPath = "$.Is3D",
                DataTypeId = 5,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "true"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "OriginalAmount",
                XPath = "$.OriginalAmount",
                DataTypeId = 3,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "100"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "OriginalCurrency",
                XPath = "$.OriginalCurrency",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "EUR"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "Email",
                XPath = "$.Email",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "please@hash.me"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "CreditCardHash",
                XPath = "$.CreditCardHash",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "1FDA39A3EE5E6B4HKAJAA890AFD80709"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "AcquirerBankName",
                XPath = "$.AcquirerBankName",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "Caixa"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "ActionDate",
                XPath = "$.ActionDate",
                DataTypeId = 4,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "2022-08-19T21:41:37.247"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "APMAccountId",
                XPath = "$.APMAccountId",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "Skrill123456789"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "BankId",
                XPath = "$.BankId",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "57"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "BillingAddress",
                XPath = "$.BillingAddress",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "Address Line 1"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "BillingCity",
                XPath = "$.BillingCity",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "Address Line 2"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "BillingCountry",
                XPath = "$.BillingCountry",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "DE"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "BillingFirstName",
                XPath = "$.BillingFirstName",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "Richard"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "BillingLastName",
                XPath = "$.BillingLastName",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "Churchman"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "BillingPhone",
                XPath = "$.BillingPhone",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "1234567890"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "BillingState",
                XPath = "$.BillingState",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "DE"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "BillingZip",
                XPath = "$.BillingZip",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "123456"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "IsAPM",
                XPath = "$.IsAPM",
                DataTypeId = 5,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "true"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "IsCascaded",
                XPath = "$.IsCascaded",
                DataTypeId = 5,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "false"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "IsCredited",
                XPath = "$.IsCredited",
                DataTypeId = 5,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "false"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "IsCurrencyConverted",
                XPath = "$.IsCurrencyConverted",
                DataTypeId = 5,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "true"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "IsModification",
                XPath = "$.IsModification",
                DataTypeId = 5,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "false"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "IsRebill",
                XPath = "$.IsRebill",
                DataTypeId = 5,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "true"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "TransactionTypeId",
                XPath = "$.TransactionTypeId",
                DataTypeId = 5,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "1000"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "TransactionResultId",
                XPath = "$.TransactionResultId",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "2000"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "IP",
                XPath = "$.IP",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "123.456.789.200"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "AmountEURRate",
                XPath = "$.AmountEURRate",
                DataTypeId = 3,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "1"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "DeviceId",
                XPath = "$.DeviceId",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 1,
                ResponsePayload = 1,
                SearchKey = 1,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "OlaRoseGoldPhone6"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "IsModified",
                XPath = "$.IsModified",
                DataTypeId = 5,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "false"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "OrderId",
                XPath = "$.OrderId",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "10607324128"
            });

            Insert.IntoTable("EntityAnalysisModelRequestXpath").Row(new
            {
                EntityAnalysisModelId = 1,
                Name = "TxnId",
                XPath = "$.TxnId",
                DataTypeId = 1,
                CreatedDate = DateTime.Now,
                Version = 1,
                EnableSuppression = 0,
                ResponsePayload = 1,
                SearchKey = 0,
                CreatedUser = "Administrator",
                Active = 1,
                DefaultValue = "0987654321"
            });
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisModelRequestXpath");
        }
    }
}