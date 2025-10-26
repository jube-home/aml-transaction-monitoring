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
    [Migration(20220429125005)]
    public class AddExhaustiveSearchInstanceVariableHistogramTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("ExhaustiveSearchInstanceVariableHistogram")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ExhaustiveSearchInstanceVariableId").AsInt32().Nullable()
                .WithColumn("BinSequence").AsInt32().Nullable()
                .WithColumn("BinRangeStart").AsDouble().Nullable()
                .WithColumn("BinRangeEnd").AsDouble().Nullable()
                .WithColumn("Frequency").AsInt32().Nullable();

            Create.Index().OnTable("ExhaustiveSearchInstanceVariableHistogram")
                .OnColumn("ExhaustiveSearchInstanceVariableId");
        }

        public override void Down()
        {
            Delete.Table("ExhaustiveSearchInstanceVariableHistogram");
        }
    }
}