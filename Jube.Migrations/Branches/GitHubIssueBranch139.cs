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

namespace Jube.Migrations.Branches
{
    using FluentMigrator;

    [Migration(20260404101600)]
    public class GitHubIssueBranch139 : Migration
    {
        public override void Up()
        {
            CreateUserRegistryApiKey();
            CreateEntityAnalysisModelRole();
        }
        
        private void CreateUserRegistryApiKey()
        {
            Create.Table("UserRegistryApiKey")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UserRegistryId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Description").AsString().Nullable()
                .WithColumn("ApiKeyVersionId").AsInt32().Nullable()
                .WithColumn("ApiKey").AsString().Nullable()
                .WithColumn("ApiKeyDisplay").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable();

            Create.Index().OnTable("UserRegistryApiKey").OnColumn("UserRegistryId").Ascending();

            Create.ForeignKey().FromTable("UserRegistryApiKey").ForeignColumn("UserRegistryId")
                .ToTable("UserRegistry").PrimaryColumn("Id");
        }

        private void CreateEntityAnalysisModelRole()
        {
            Create.Table("EntityAnalysisModelRole")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("EntityAnalysisModelGuid").AsGuid().Nullable()
                .WithColumn("RoleRegistryGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("EntityAnalysisModelRole").OnColumn("EntityAnalysisModelGuid").Ascending().OnColumn("RoleRegistryGuid");

            Execute.Sql(""" 
                        insert into "EntityAnalysisModelRole"("RoleRegistryGuid", 
                                                                "EntityAnalysisModelGuid", 
                                                                "Guid",
                                                                "CreatedDate",
                                                                "CreatedUser",
                                                                "Version")
                        select r."Guid" as "RoleRegistryGuid", 
                               w."Guid" as "EntityAnalysisModelGuid", 
                               gen_random_uuid() as "Guid",
                               now() as "CreatedDate",
                               'Administrator' as "CreatedUser",
                               1 as "Version"
                        FROM "RoleRegistry" r,
                             "EntityAnalysisModel" w
                        where r."Id" = 1
                         and w."Deleted" is null
                        or w."Deleted" = 0
                         and r."Deleted" is null
                        or r."Deleted" = 0
                        """);
        }

        public override void Down()
        {

        }
    }
}
