namespace Jube.Migrations.Branches.GitHubIssueBranch6
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cache;
    using DynamicEnvironment;
    using FluentMigrator;
    using log4net;
    using Npgsql;
    using StackExchange.Redis;

    [Migration(20250405191701)]
    public class MigrateLegacyRedisKeysAfterDatabaseMigration(
        CacheService cacheService,
        DynamicEnvironment dynamicEnvironment,
        ILog log) : Migration
    {
        public override void Up()
        {
            MigrateLegacyRedisKeys();
        }

        private static void Swap(string[] segments, int position, Dictionary<int, Guid> idGuidDictionary)
        {
            var id = Int32.Parse(segments[position]);
            idGuidDictionary.TryGetValue(id, out var guid);
            segments[position] = guid.ToString("N");
        }

        private void MigrateLegacyRedisKeys()
        {
            var entityAnalysisModelIdList = GetIdGuidDictionary("EntityAnalysisModel");
            var entityAnalysisModelTtlCounterIdList = GetIdGuidDictionary("EntityAnalysisModelTtlCounter");
            var redisServers = GetRedisServers();

            IterateAndRenameKeysIfChanged(redisServers, entityAnalysisModelIdList, entityAnalysisModelTtlCounterIdList);
        }

        private void IterateAndRenameKeysIfChanged(List<IServer> redisServers,
            Dictionary<int, Guid> entityAnalysisModelIdList,
            Dictionary<int, Guid> entityAnalysisModelTtlCounterIdList)
        {
            foreach (var key in redisServers.SelectMany(redisServer => redisServer.Keys()))
            {
                try
                {
                    var newKey = MigrateKey(key, entityAnalysisModelIdList, entityAnalysisModelTtlCounterIdList);

                    if (newKey == key)
                    {
                        continue;
                    }

                    if (cacheService.RedisDatabase != null)
                    {
                        cacheService.RedisDatabase.KeyRename(key, newKey);
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Could not migrate key {key} for exception {ex}.");
                }
            }
        }

        private List<IServer> GetRedisServers()
        {
            return cacheService.ConnectionMultiplexer?.GetEndPoints()
                .Select(redisEndpoint => cacheService.ConnectionMultiplexer.GetServer(redisEndpoint)).ToList();
        }

        private static string MigrateKey(RedisKey key, Dictionary<int, Guid> entityAnalysisModelIdList,
            Dictionary<int, Guid> entityAnalysisModelTtlCounterIdList)
        {
            var segments = key.ToString().Split(":");
            switch (segments[0])
            {
                case "ReferenceDate":
                    if (segments.Length > 2)
                    {
                        Swap(segments, 2, entityAnalysisModelIdList);
                    }

                    break;
                case "Payload":
                case "LatestCount":
                case "ReferenceDateLatest":
                case "ReferenceDateFirst":
                case "Abstraction":
                case "Sanction":
                    Swap(segments, 2, entityAnalysisModelIdList);
                    break;
                case "TtlCounter":
                case "TtlCounterEntry":
                    Swap(segments, 2, entityAnalysisModelIdList);
                    Swap(segments, 3, entityAnalysisModelTtlCounterIdList);
                    break;
                default:
                    return key;
            }

            return String.Join(":", segments);
        }

        private Dictionary<int, Guid> GetIdGuidDictionary(string tableName)
        {
            var values = new Dictionary<int, Guid>();
            var connection = new NpgsqlConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            try
            {
                connection.Open();

                var command = new NpgsqlCommand($"select \"Id\",\"Guid\" from \"{tableName}\"");
                command.Connection = connection;

                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var id = reader.IsDBNull(0) ? null : reader.GetValue(0);
                    var guid = reader.IsDBNull(1) ? null : reader.GetValue(1);

                    if (id == null)
                    {
                        continue;
                    }
                    if (guid != null)
                    {
                        values.Add((int)id, (Guid)guid);
                    }
                }

                reader.Close();
                reader.Dispose();
                command.Dispose();
            }
            catch
            {
                connection.Close();
                connection.Dispose();
                throw;
            }
            finally
            {
                connection.Close();
                connection.Dispose();
            }

            return values;
        }

        public override void Down()
        {
        }
    }
}
