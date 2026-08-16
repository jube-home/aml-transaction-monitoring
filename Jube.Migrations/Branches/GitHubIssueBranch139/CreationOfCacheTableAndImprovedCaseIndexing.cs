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
    using FluentMigrator;

    [Migration(20260523081600)]
    public class CreationOfCacheTableAndImprovedCaseIndexing : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        CREATE TABLE "CacheSetHash" (
                            "Key" text NOT NULL,
                            "Field" text NOT NULL,
                            "ByteValue" bytea,
                            "IntValue" bigint,
                            "FloatValue" double precision
                        ) PARTITION BY HASH ("Key");

                        CREATE TABLE "CacheSetHash_0" PARTITION OF "CacheSetHash" 
                            FOR VALUES WITH (MODULUS 4, REMAINDER 0) 
                            WITH (fillfactor = 70, autovacuum_vacuum_scale_factor = 0.005, autovacuum_vacuum_threshold = 50, autovacuum_vacuum_cost_delay = 0, autovacuum_vacuum_cost_limit = 400, autovacuum_analyze_scale_factor = 0.005, parallel_workers = 4);

                        CREATE TABLE "CacheSetHash_1" PARTITION OF "CacheSetHash" 
                            FOR VALUES WITH (MODULUS 4, REMAINDER 1) 
                            WITH (fillfactor = 70, autovacuum_vacuum_scale_factor = 0.005, autovacuum_vacuum_threshold = 50, autovacuum_vacuum_cost_delay = 0, autovacuum_vacuum_cost_limit = 400, autovacuum_analyze_scale_factor = 0.005, parallel_workers = 4);

                        CREATE TABLE "CacheSetHash_2" PARTITION OF "CacheSetHash" 
                            FOR VALUES WITH (MODULUS 4, REMAINDER 2) 
                            WITH (fillfactor = 70, autovacuum_vacuum_scale_factor = 0.005, autovacuum_vacuum_threshold = 50, autovacuum_vacuum_cost_delay = 0, autovacuum_vacuum_cost_limit = 400, autovacuum_analyze_scale_factor = 0.005, parallel_workers = 4);

                        CREATE TABLE "CacheSetHash_3" PARTITION OF "CacheSetHash" 
                            FOR VALUES WITH (MODULUS 4, REMAINDER 3) 
                            WITH (fillfactor = 70, autovacuum_vacuum_scale_factor = 0.005, autovacuum_vacuum_threshold = 50, autovacuum_vacuum_cost_delay = 0, autovacuum_vacuum_cost_limit = 400, autovacuum_analyze_scale_factor = 0.005, parallel_workers = 4);

                        ALTER TABLE "CacheSetHash" 
                            ADD CONSTRAINT "PK_CacheSetHash" PRIMARY KEY ("Key", "Field");

                        CREATE INDEX ON "CacheSetHash_0" ("Key", "Field") INCLUDE ("ByteValue", "IntValue", "FloatValue");
                        CREATE INDEX ON "CacheSetHash_1" ("Key", "Field") INCLUDE ("ByteValue", "IntValue", "FloatValue");
                        CREATE INDEX ON "CacheSetHash_2" ("Key", "Field") INCLUDE ("ByteValue", "IntValue", "FloatValue");
                        CREATE INDEX ON "CacheSetHash_3" ("Key", "Field") INCLUDE ("ByteValue", "IntValue", "FloatValue");
                        """);

            Execute.Sql("""
                        CREATE INDEX ON "Case" ("CaseKey", "CaseKeyValue", "CaseWorkflowGuid")
                        WHERE "ClosedStatusId" IN (0, 1, 2, 4);
                        """);
        }

        public override void Down()
        {

        }
    }
}
