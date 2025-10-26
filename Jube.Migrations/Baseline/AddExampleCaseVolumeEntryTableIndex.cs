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

namespace Jube.Migrations.Baseline
{
    [Migration(20220909132300)]
    public class AddExampleCaseVolumeEntryTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("ExampleCaseVolumeEntry")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ActivationRuleName").AsString().Nullable()
                .WithColumn("Frequency").AsInt32().Nullable()
                .WithColumn("PercentageContribution").AsDouble().Nullable();

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "EmailAmountOverIndustryNameAverage",
                    Frequency = 17895,
                    PercentageContribution = 50.1
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "EmailAmountOverBusinessModelAverage",
                    Frequency = 5522,
                    PercentageContribution = 15.46
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "PANAmountOverIndustryNameAverage",
                    Frequency = 1758,
                    PercentageContribution = 4.92
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "PANAmountOverClientAverage",
                    Frequency = 1739,
                    PercentageContribution = 4.87
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "VolumeEmailCountryIDNotEqualIPCountry",
                    Frequency = 860,
                    PercentageContribution = 2.41
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "Sanctions",
                    Frequency = 851,
                    PercentageContribution = 2.38
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "AnyProhibitedHighRiskCountry",
                    Frequency = 600,
                    PercentageContribution = 1.68
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "EmailAmountOverClientAverage",
                    Frequency = 589,
                    PercentageContribution = 1.65
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "VolumeEmailHighRiskCountry",
                    Frequency = 577,
                    PercentageContribution = 1.62
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "IPAmountOverClientAverage",
                    Frequency = 505,
                    PercentageContribution = 1.41
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "ClientWithdrawVolumeRate30Days",
                    Frequency = 391,
                    PercentageContribution = 1.09
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "VolumeEmailTaxHavenCountry",
                    Frequency = 332,
                    PercentageContribution = 0.93
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "EmailAmountOverCountryIDAverage",
                    Frequency = 263,
                    PercentageContribution = 0.74
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "IPAmountOverBusinessModelAverage",
                    Frequency = 263,
                    PercentageContribution = 0.73
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "CountDistinctPANForIP1Days",
                    Frequency = 20,
                    PercentageContribution = 0.06
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "IPAmountOverIndustryNameAverage",
                    Frequency = 16,
                    PercentageContribution = 0.04
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "IPAmountOverCountryIDAverage",
                    Frequency = 4,
                    PercentageContribution = 0.01
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "CountDistinctIPForEmailsFor1Days",
                    Frequency = 1,
                    PercentageContribution = (double)0
                });

            Insert.IntoTable("ExampleCaseVolumeEntry").Row(
                new
                {
                    ActivationRuleName = "PANAmountOverCountryIDAverage",
                    Frequency = 1,
                    PercentageContribution = 0
                });
        }

        public override void Down()
        {
            Delete.Table("ExampleCaseVolumeEntry");
        }
    }
}