/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.Migrations.Branches.GitHubIssueBranch139
{
    using FluentMigrator;

    [Migration(20263107210000)]
    public class AddHashCacheAssemblyObservabilityTables : Migration
    {
        public override void Up()
        {
            CreateHashCacheAssemblyInstance();
            CreateHashCacheAssemblyInstanceEntry();
            CreateHashCacheAssemblyInstanceJournal();
        }

        private void CreateHashCacheAssemblyInstance()
        {
            Create.Table("HashCacheAssemblyInstance")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Instance").AsString().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Count").AsInt64().Nullable()
                .WithColumn("Bytes").AsInt64().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable();
        }

        private void CreateHashCacheAssemblyInstanceEntry()
        {
            Create.Table("HashCacheAssemblyInstanceEntry")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("HashCacheAssemblyInstanceId").AsInt32().Nullable()
                .WithColumn("ScriptHash").AsString().Nullable()
                .WithColumn("Bytes").AsInt64().Nullable()
                .WithColumn("Code").AsString().Nullable()
                .WithColumn("Binary").AsBinary().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("LastSeenDate").AsDateTime().Nullable();

            Create.Index().OnTable("HashCacheAssemblyInstanceEntry")
                .OnColumn("HashCacheAssemblyInstanceId").Ascending();

            Create.Index("IX_HashCacheAssemblyInstanceEntry_ScriptHash").OnTable("HashCacheAssemblyInstanceEntry")
                .OnColumn("ScriptHash").Ascending()
                .WithOptions().Unique();

            Create.ForeignKey().FromTable("HashCacheAssemblyInstanceEntry").ForeignColumn("HashCacheAssemblyInstanceId")
                .ToTable("HashCacheAssemblyInstance").PrimaryColumn("Id");
        }

        private void CreateHashCacheAssemblyInstanceJournal()
        {
            Create.Table("HashCacheAssemblyInstanceJournal")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("HashCacheAssemblyInstanceId").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Count").AsInt64().Nullable()
                .WithColumn("Bytes").AsInt64().Nullable();

            Create.Index().OnTable("HashCacheAssemblyInstanceJournal")
                .OnColumn("HashCacheAssemblyInstanceId").Ascending();

            Create.ForeignKey().FromTable("HashCacheAssemblyInstanceJournal").ForeignColumn("HashCacheAssemblyInstanceId")
                .ToTable("HashCacheAssemblyInstance").PrimaryColumn("Id");
        }

        public override void Down()
        {
            Delete.Table("HashCacheAssemblyInstanceJournal");
            Delete.Table("HashCacheAssemblyInstanceEntry");
            Delete.Table("HashCacheAssemblyInstance");
        }
    }
}
