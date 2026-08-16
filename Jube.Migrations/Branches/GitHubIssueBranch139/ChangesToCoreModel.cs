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

namespace Jube.Migrations.Branches.GitHubIssueBranch139
{
    using System;
    using FluentMigrator;

    [Migration(20260404101600)]
    public class ChangesToCoreModel : Migration
    {
        public override void Up()
        {
            CreateUserRegistryApiKey();
            CreateEntityAnalysisModelRole();
            UpdateUserRegistry();
            UpdateUserRegistryVersion();
            CreateSamplingPermissionAndSubscribeToDefaultAdministrator();
            CreateEntityAnalysisModelSampleExecutionLog();
            RemoveTheForeignKeyFromInlineScriptToMakeLessBrittleToImport();
        }

        private void RemoveTheForeignKeyFromInlineScriptToMakeLessBrittleToImport()
        {
            Delete.ForeignKey("FK_EntityAnalysisModelInlineScript_EntityAnalysisInlineScriptId")
                .OnTable("EntityAnalysisModelInlineScript");
        }

        private void CreateEntityAnalysisModelSampleExecutionLog()
        {
            Create.Table("EntityAnalysisModelSampleExecutionLog")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("DateFrom").AsDateTime().Nullable()
                .WithColumn("DateTo").AsDateTime().Nullable()
                .WithColumn("Sample").AsDouble().Nullable()
                .WithColumn("InError").AsByte().Nullable()
                .WithColumn("ErrorStack").AsString().Nullable()
                .WithColumn("RowCount").AsInt32().Nullable()
                .WithColumn("ResponseTime").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelSampleExecutionLog")
                .ForeignColumn("EntityAnalysisModelId")
                .ToTable("EntityAnalysisModel").PrimaryColumn("Id");

            Create.Index().OnTable("EntityAnalysisModelSampleExecutionLog")
                .OnColumn("EntityAnalysisModelId").Ascending();
        }

        private void CreateSamplingPermissionAndSubscribeToDefaultAdministrator()
        {
            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = 40,
                Name = "View Entity Analysis Model Sampling"
            });

            Insert.IntoTable("RoleRegistryPermission").Row(new
            {
                RoleRegistryId = 1,
                PermissionSpecificationId = 40,
                Active = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "Administrator",
                Version = 1,
                Guid = Guid.NewGuid()
            });
        }

        private void UpdateUserRegistryVersion()
        {
            Alter.Table("UserRegistryVersion").AddColumn("RoleRegistryGuid").AsGuid().Nullable();

            Execute.Sql(
                """UPDATE "UserRegistryVersion" b SET "RoleRegistryGuid" = a."Guid" FROM "RoleRegistry" a WHERE a."Id" = b."RoleRegistryId";""");

            Delete.Column("RoleRegistryId").FromTable("UserRegistryVersion");
        }

        private void UpdateUserRegistry()
        {
            Alter.Table("UserRegistry").AddColumn("RoleRegistryGuid").AsGuid().Nullable();

            Execute.Sql(
                """UPDATE "UserRegistry" b SET "RoleRegistryGuid" = a."Guid" FROM "RoleRegistry" a WHERE a."Id" = b."RoleRegistryId";""");

            Create.Index().OnTable("UserRegistry").OnColumn("RoleRegistryGuid").Ascending();

            Delete.Column("RoleRegistryId").FromTable("UserRegistry");
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
