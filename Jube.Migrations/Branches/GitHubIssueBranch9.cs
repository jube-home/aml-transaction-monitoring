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

namespace Jube.Migrations.Branches
{
    [Migration(20231123110900)]
    public class GitHubIssueBranch9 : Migration
    {
        public override void Up()
        {
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstance")
                .AddColumn("Json")
                .AsCustom("jsonb");

            Create.Table("ExhaustiveSearchInstanceVariableClassification")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ExhaustiveSearchInstanceVariableId").AsInt32().Nullable()
                .WithColumn("Mode").AsDouble().Nullable()
                .WithColumn("Mean").AsDouble().Nullable()
                .WithColumn("StandardDeviation").AsDouble().Nullable()
                .WithColumn("Kurtosis").AsDouble().Nullable()
                .WithColumn("Skewness").AsDouble().Nullable()
                .WithColumn("Maximum").AsDouble().Nullable()
                .WithColumn("Minimum").AsDouble().Nullable()
                .WithColumn("Iqr").AsDouble().Nullable()
                .WithColumn("DistinctValues").AsInt32().Nullable()
                .WithColumn("Bins").AsInt32().Nullable();
        
            Create.Index().OnTable("ExhaustiveSearchInstanceVariableClassification")
                .OnColumn("ExhaustiveSearchInstanceVariableId");
        
            Create.Table("ExhaustiveSearchInstanceVariableHistogramClassification")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ExhaustiveSearchInstanceVariableClassificationId").AsInt32().Nullable()
                .WithColumn("BinSequence").AsInt32().Nullable()
                .WithColumn("BinRangeStart").AsDouble().Nullable()
                .WithColumn("BinRangeEnd").AsDouble().Nullable()
                .WithColumn("Frequency").AsInt32().Nullable();

            Create.Index().OnTable("ExhaustiveSearchInstanceVariableHistogramClassification")
                .OnColumn("ExhaustiveSearchInstanceVariableClassificationId");
        
            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceVariableClassification")
                .ForeignColumn("ExhaustiveSearchInstanceVariableId").ToTable("ExhaustiveSearchInstanceVariable")
                .PrimaryColumn("Id");
        
            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceVariableHistogramClassification")
                .ForeignColumn("ExhaustiveSearchInstanceVariableClassificationId").ToTable("ExhaustiveSearchInstanceVariableClassification")
                .PrimaryColumn("Id");
        
            Create.Table("ExhaustiveSearchInstanceVariableAnomaly")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ExhaustiveSearchInstanceVariableId").AsInt32().Nullable()
                .WithColumn("Mode").AsDouble().Nullable()
                .WithColumn("Mean").AsDouble().Nullable()
                .WithColumn("StandardDeviation").AsDouble().Nullable()
                .WithColumn("Kurtosis").AsDouble().Nullable()
                .WithColumn("Skewness").AsDouble().Nullable()
                .WithColumn("Maximum").AsDouble().Nullable()
                .WithColumn("Minimum").AsDouble().Nullable()
                .WithColumn("Iqr").AsDouble().Nullable()
                .WithColumn("DistinctValues").AsInt32().Nullable()
                .WithColumn("Bins").AsInt32().Nullable();
        
            Create.Index().OnTable("ExhaustiveSearchInstanceVariableAnomaly")
                .OnColumn("ExhaustiveSearchInstanceVariableId");
        
            Create.Table("ExhaustiveSearchInstanceVariableHistogramAnomaly")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ExhaustiveSearchInstanceVariableAnomalyId").AsInt32().Nullable()
                .WithColumn("BinSequence").AsInt32().Nullable()
                .WithColumn("BinRangeStart").AsDouble().Nullable()
                .WithColumn("BinRangeEnd").AsDouble().Nullable()
                .WithColumn("Frequency").AsInt32().Nullable();

            Create.Index().OnTable("ExhaustiveSearchInstanceVariableHistogramAnomaly")
                .OnColumn("ExhaustiveSearchInstanceVariableAnomalyId");
        
            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceVariableAnomaly")
                .ForeignColumn("ExhaustiveSearchInstanceVariableId").ToTable("ExhaustiveSearchInstanceVariable")
                .PrimaryColumn("Id");
        
            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceVariableHistogramAnomaly")
                .ForeignColumn("ExhaustiveSearchInstanceVariableAnomalyId").ToTable("ExhaustiveSearchInstanceVariableAnomaly")
                .PrimaryColumn("Id");
        }

        public override void Down()
        {
            Delete.Column("Json")
                .FromTable("ExhaustiveSearchInstancePromotedTrialInstance");
        
            Delete.Table("ExhaustiveSearchInstanceVariableClassification");
        
            Delete.Table("ExhaustiveSearchInstanceVariableHistogramClassification");
        
            Delete.ForeignKey().FromTable("ExhaustiveSearchInstanceVariableClassification")
                .ForeignColumn("ExhaustiveSearchInstanceVariableId").ToTable("ExhaustiveSearchInstanceVariable")
                .PrimaryColumn("Id");
        
            Delete.ForeignKey().FromTable("ExhaustiveSearchInstanceVariableHistogramClassification")
                .ForeignColumn("ExhaustiveSearchInstanceVariableClassificationId").ToTable("ExhaustiveSearchInstanceVariableClassification")
                .PrimaryColumn("Id");
        }
    }
}