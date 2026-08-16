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

namespace Jube.Migrations.Branches.GitHubIssueBranch118
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cache;
    using Cache.Redis.Serialization.DictionaryNoBoxing.MessagePack;
    using Data.Extension;
    using Dictionary;
    using DynamicEnvironment;
    using FluentMigrator;
    using log4net;
    using MessagePack;
    using MessagePack.Resolvers;
    using Npgsql;
    using StackExchange.Redis;

    [Migration(20262302172900)]
    public class MigrateLegacyPayloadMessagePackValues(
        CacheService cacheService,
        DynamicEnvironment dynamicEnvironment,
        ILog log) : Migration
    {
        public override void Up()
        {
            MigratePayloadsInRedis();
        }

        public override void Down()
        {

        }

        private Dictionary<int, Dictionary<string, int>> GetEntityAnalysisModelRequestXPaths()
        {
            var values = new Dictionary<int, Dictionary<string, int>>();
            var connection = new NpgsqlConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            connection.Open();

            var command = new NpgsqlCommand("""
                                            select e."EntityAnalysisModelId",e."Name", e."CacheIndexId"
                                            from "EntityAnalysisModelRequestXpath" e
                                            """);

            command.Connection = connection;

            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (values.ContainsKey(reader[0].AsInt()))
                {
                    values[reader[0].AsInt()].Add(reader[1].AsString(), reader[2].AsInt());
                }
                else
                {
                    values.Add(reader[0].AsInt(), new Dictionary<string, int>());
                    values[reader[0].AsInt()].Add(reader[1].AsString(), reader[2].AsInt());
                }
            }

            reader.Close();
            reader.Dispose();
            command.Dispose();

            return values;
        }

        private Dictionary<Guid, (int id, string referenceDateName, int tenantRegistryId)> GetEntityAnalysisModels()
        {
            var values = new Dictionary<Guid, (int id, string referenceDateName, int tenantRegistryId)>();
            var connection = new NpgsqlConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            connection.Open();

            var command = new NpgsqlCommand("""
                                            select e."Guid", e."Id", e."ReferenceDateName",e."TenantRegistryId"
                                            from "EntityAnalysisModel" e
                                            """);

            command.Connection = connection;

            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (values.ContainsKey(reader[0].AsGuid()))
                {
                    values[reader[0].AsGuid()] = (reader[1].AsInt(), reader[2].ToString(), reader[3].AsInt());
                }
                else
                {
                    values.Add(reader[0].AsGuid(), (reader[1].AsInt(), reader[2].ToString(), reader[3].AsInt()));
                }
            }

            reader.Close();
            reader.Dispose();
            command.Dispose();

            return values;
        }

        private void MigratePayloadsInRedis()
        {
            var redisServers = cacheService.ConnectionMultiplexer?.GetEndPoints()
                .Select(redisEndpoint => cacheService.ConnectionMultiplexer.GetServer(redisEndpoint)).ToList();

            if (redisServers == null)
            {
                return;
            }

            var useCompression = dynamicEnvironment.AppSettings("RedisMessagePackCompression")
                .Equals("True", StringComparison.OrdinalIgnoreCase);
            var compression = useCompression ? MessagePackCompression.Lz4BlockArray : MessagePackCompression.None;

            var oldMessagePackSerializerOptions = MessagePackSerializerOptions.Standard
                .WithResolver(CompositeResolver.Create(new EnvelopeDictionaryNoBoxingStringMessagePackFormatter()))
                .WithCompression(compression);

            var newMessagePackSerializerOptions = MessagePackSerializerOptions.Standard
                .WithResolver(CompositeResolver.Create(new EnvelopeDictionaryNoBoxingIntMessagePackFormatter()))
                .WithCompression(compression);

            var entityAnalysisModels = GetEntityAnalysisModels();
            var entityAnalysisModelRequestXPaths = GetEntityAnalysisModelRequestXPaths();

            const int batchSize = 1000;
            foreach (var key in redisServers.SelectMany(s => s.Keys(pattern: "Payload:*")))
            {
                try
                {
                    var batch = new List<HashEntry>(batchSize);
                    var splits = key.ToString().Split(":");

                    foreach (var entry in cacheService.ResilientRedisResilientRedisDatabase.HashScan(key, pageSize: 500))
                    {
                        if (!entry.Value.HasValue)
                        {
                            continue;
                        }

                        try
                        {
                            if (!entityAnalysisModels.TryGetValue(Guid.Parse(splits[2]), out var entityAnalysisModel))
                            {
                                continue;
                            }

                            if (entityAnalysisModel.tenantRegistryId != Int32.Parse(splits[1]))
                            {
                                continue;
                            }

                            if (!entityAnalysisModelRequestXPaths.TryGetValue(entityAnalysisModel.id, out var xPathsForModel))
                            {
                                continue;
                            }

                            var oldEnvelope = MessagePackSerializer.Deserialize<EnvelopeDictionaryNoBoxing<string>>(
                                entry.Value, oldMessagePackSerializerOptions);

                            var newEnvelope = new EnvelopeDictionaryNoBoxing<int>
                            {
                                Version = 2,
                                Data = MapToNewReducedInternedPayload(oldEnvelope, entityAnalysisModel, xPathsForModel)
                            };

                            batch.Add(new HashEntry(entry.Name, MessagePackSerializer.Serialize(newEnvelope, newMessagePackSerializerOptions)));

                            if (batch.Count >= batchSize)
                            {
                                cacheService.ResilientRedisResilientRedisDatabase.HashSet(key, batch.ToArray());
                                batch.Clear();
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Payload migration failed for hashEntry {entry.Name}: {ex.Message}");
                        }
                    }

                    if (batch.Count > 0)
                    {
                        cacheService.ResilientRedisResilientRedisDatabase.HashSet(key, batch.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Could not migrate key {key}: {ex}");
                }
            }
        }

        private static DictionaryNoBoxing<int> MapToNewReducedInternedPayload(
            EnvelopeDictionaryNoBoxing<string> oldEnvelope,
            (int id, string referenceDateName, int tenantRegistryId) entityAnalysisModel,
            Dictionary<string, int> xPathsForModel)
        {
            var reducedInternedPayload = new DictionaryNoBoxing<int>();

            if (!oldEnvelope.Data.TryGetValue(entityAnalysisModel.referenceDateName, out var referenceDate))
            {
                return reducedInternedPayload;
            }

            reducedInternedPayload.Add(-1, referenceDate);

            foreach (var xPath in xPathsForModel)
            {
                if (oldEnvelope.Data.TryGetValue(xPath.Key, out var value))
                {
                    reducedInternedPayload.Add(xPath.Value, value);
                }
            }

            return reducedInternedPayload;
        }
    }
}
