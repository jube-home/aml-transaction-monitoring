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
    [Migration(20220608104200)]
    public class AddSessionCaseSearchCompiledSqlTableIndex : Migration
    {
        public override void Up()
        {
            Create.Table("SessionCaseSearchCompiledSql")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowId").AsInt32().Nullable()
                .WithColumn("FilterJson").AsCustom("jsonb").Nullable()
                .WithColumn("FilterTokens").AsCustom("jsonb").Nullable()
                .WithColumn("SelectJson").AsCustom("jsonb").Nullable()
                .WithColumn("FilterSql").AsString().Nullable()
                .WithColumn("Prepared").AsByte().Nullable()
                .WithColumn("Error").AsString().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("SelectSqlSearch").AsString().Nullable()
                .WithColumn("SelectSqlDisplay").AsString().Nullable()
                .WithColumn("WhereSql").AsString().Nullable()
                .WithColumn("OrderSql").AsString().Nullable();

            Create.Index().OnTable("SessionCaseSearchCompiledSql").OnColumn("Guid").Unique();
        }

        public override void Down()
        {
            Delete.Table("SessionCaseSearchCompiledSql");
        }
    }
}