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
    [Migration(20220429125007)]
    public class AddExhaustiveSearchInstanceVariableTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("ExhaustiveSearchInstanceVariable")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ExhaustiveSearchInstanceId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("ProcessingTypeId").AsByte().Nullable()
                .WithColumn("Mode").AsDouble().Nullable()
                .WithColumn("Mean").AsDouble().Nullable()
                .WithColumn("StandardDeviation").AsDouble().Nullable()
                .WithColumn("Kurtosis").AsDouble().Nullable()
                .WithColumn("Skewness").AsDouble().Nullable()
                .WithColumn("Maximum").AsDouble().Nullable()
                .WithColumn("Minimum").AsDouble().Nullable()
                .WithColumn("Iqr").AsDouble().Nullable()
                .WithColumn("PrescriptionSimulation").AsByte().Nullable()
                .WithColumn("NormalisationTypeId").AsByte().Nullable()
                .WithColumn("DistinctValues").AsInt32().Nullable()
                .WithColumn("Correlation").AsDouble().Nullable()
                .WithColumn("CorrelationAbsRank").AsInt32().Nullable()
                .WithColumn("Bins").AsInt32().Nullable()
                .WithColumn("VariableSequence").AsInt32().Nullable();
        }

        public override void Down()
        {
            Delete.Table("ExhaustiveSearchInstanceVariable");
        }
    }
}