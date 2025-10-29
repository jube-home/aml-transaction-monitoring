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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Cache;
    using Cache.Redis.Serialization;
    using Cache.Redis.Serialization.DictionaryNoBoxing.MessagePack;
    using Dictionary;
    using DynamicEnvironment;
    using FluentMigrator;
    using log4net;
    using MessagePack;
    using MessagePack.Resolvers;
    using StackExchange.Redis;

    [Migration(20251019102200)]
    public class GitHubIssueBranch84(
        CacheService cacheService,
        DynamicEnvironment dynamicEnvironment,
        ILog log) : Migration
    {
        public override void Up()
        {
            CreateLocalCacheInstance();
            CreateLocalCacheInstanceKey();
            CreateLocalCacheInstanceLru();
            UpdateInlineScriptCodeForDictionaryNoBoxingAndDeprecatedAttributes();
            ChangeKeyNamesIfNeededOrMigrateDataToEnvelopeNoBoxing();
        }
        
        private void CreateLocalCacheInstance()
        {

            Create.Table("LocalCacheInstance")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Instance").AsString().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Fill").AsByte().Nullable()
                .WithColumn("FillStartedDate").AsDateTime().Nullable()
                .WithColumn("FillCount").AsInt64().Nullable()
                .WithColumn("FillBytes").AsInt64().Nullable()
                .WithColumn("Filled").AsByte().Nullable()
                .WithColumn("FillEndedDate").AsDateTime().Nullable()
                .WithColumn("Bytes").AsInt64().Nullable()
                .WithColumn("Count").AsInt64().Nullable()
                .WithColumn("HeapSizeBytes").AsInt64().Nullable()
                .WithColumn("TotalCommittedBytes").AsInt64().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable();
        }
        
        private void CreateLocalCacheInstanceKey()
        {

            Create.Table("LocalCacheInstanceKey")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("LocalCacheInstanceId").AsInt32().Nullable()
                .WithColumn("Key").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Requests").AsInt64().Nullable()
                .WithColumn("Misses").AsInt32().Nullable()
                .WithColumn("MissRemoteResponseTime").AsInt64().Nullable()
                .WithColumn("UnpackResponseTime").AsInt64().Nullable()
                .WithColumn("HashSetSubscriptions").AsInt32().Nullable()
                .WithColumn("HashRemove").AsInt32().Nullable()
                .WithColumn("HashRemoveMiss").AsInt32().Nullable()
                .WithColumn("HashRemoveSubscription").AsInt32().Nullable()
                .WithColumn("HashRemoveSubscriptionMiss").AsInt32().Nullable()
                .WithColumn("DualMiss").AsInt32().Nullable();

            Create.Index().OnTable("LocalCacheInstanceKey")
                .OnColumn("LocalCacheInstanceId").Ascending();

            Create.ForeignKey().FromTable("LocalCacheInstanceKey").ForeignColumn("LocalCacheInstanceId")
                .ToTable("LocalCacheInstance").PrimaryColumn("Id");
        }
        
        private void CreateLocalCacheInstanceLru()
        {

            Create.Table("LocalCacheInstanceLru")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("LocalCacheInstanceId").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Bytes").AsInt64().Nullable()
                .WithColumn("Count").AsInt64().Nullable()
                .WithColumn("RequestBytes").AsInt64().Nullable()
                .WithColumn("RequestCount").AsInt64().Nullable()
                .WithColumn("AddBytes").AsInt64().Nullable()
                .WithColumn("AddCount").AsInt64().Nullable()
                .WithColumn("RemoveBytes").AsInt64().Nullable()
                .WithColumn("RemoveCount").AsInt64().Nullable()
                .WithColumn("EvictionBytes").AsInt64().Nullable()
                .WithColumn("EvictionCount").AsInt64().Nullable()
                .WithColumn("UpdateBytes").AsInt64().Nullable()
                .WithColumn("UpdateCount").AsInt64().Nullable();

            Create.Index().OnTable("LocalCacheInstanceLru")
                .OnColumn("LocalCacheInstanceId").Ascending();

            Create.ForeignKey().FromTable("LocalCacheInstanceLru").ForeignColumn("LocalCacheInstanceId")
                .ToTable("LocalCacheInstance").PrimaryColumn("Id");
        }
        
        private void UpdateInlineScriptCodeForDictionaryNoBoxingAndDeprecatedAttributes()
        {

            var code = "Imports log4net\nI" +
                       "mports System\n" +
                       "Imports System.Collections.Generic\n" +
                       "Imports Jube.Dictionary\n" +
                       "Imports Jube.Dictionary.Attributes\n" +
                       "Imports Microsoft.VisualBasic\n" +
                       "Public Class IssueOTP\n" +
                       "   Inherits System.Attribute\n\n" +
                       "   <ResponsePayload>\n" +
                       "   Public Property OTP As String\n\n" +
                       "   Private _log as ILog\n" +
                       "   Public Sub New(Log As ILog)\n" +
                       "       _log = Log\n   End Sub\n\n" +
                       "   Public Sub Execute(Data As DictionaryNoBoxing, Log As ILog)\n" +
                       "       Data.Add(\"OTP\", RandomDigits(6))\n" +
                       "   End Sub\n\n" +
                       "   Private Function RandomDigits(ByVal length As Integer) As String\n" +
                       "       Dim random = New Random()\n" +
                       "       Dim s As String = String.Empty\n" +
                       "       For i As Integer = 0 To length - 1\n" +
                       "           s = String.Concat(s, random.[Next](10).ToString())\n" +
                       "       Next\n" +
                       "       Return s\n" +
                       "   End Function\n" +
                       "End Class";

            Update.Table("EntityAnalysisInlineScript").Set(new
            {
                Code = code
            }).Where(new
            {
                Id = 1
            });
        }
        
        private void ChangeKeyNamesIfNeededOrMigrateDataToEnvelopeNoBoxing()
        {
            var redisServers = cacheService.ConnectionMultiplexer?.GetEndPoints()
                .Select(redisEndpoint => cacheService.ConnectionMultiplexer.GetServer(redisEndpoint)).ToList();

            if (redisServers == null)
            {
                return;
            }

            const string journalPrefix = "Journal";
            const string payloadLatestPrefix = "PayloadLatest";

            var messagePackSerializerOptionsOld = MessagePackSerializerOptionsHelper
                .ContractlessStandardResolverWithCompressionMessagePackSerializerOptions(true);

            var messagePackSerializerOptionsNew = MessagePackSerializerOptions.Standard
                .WithResolver(
                    CompositeResolver.Create(new EnvelopeDictionaryNoBoxingMessagePackFormatter())
                ).WithCompression(dynamicEnvironment.AppSettings("RedisMessagePackCompression").Equals("True", StringComparison.OrdinalIgnoreCase)
                    ? MessagePackCompression.Lz4BlockArray : MessagePackCompression.None);

            foreach (var key in redisServers.SelectMany(redisServer => redisServer.Keys()))
            {
                try
                {
                    var splits = key.ToString().Split(":");

                    if (splits[0] == "Payload")
                    {
                        if (CheckAndChangeKeyNameGivenNumberOfSplits(messagePackSerializerOptionsOld, messagePackSerializerOptionsNew,
                                splits, key, payloadLatestPrefix, journalPrefix))
                        {
                            continue;
                        }

                        cacheService.RedisDatabase?.KeyRename(key, String.Join(":", splits));
                    }

                    if (splits[0] != "PayloadCount")
                    {
                        continue;
                    }

                    CorrectWrongGuidFormatInThePayloadCountHashSet(splits);
                }
                catch (Exception ex)
                {
                    log.Error($"Could not migrate key {key} for exception {ex}.");
                }
            }
        }
        
        private void CorrectWrongGuidFormatInThePayloadCountHashSet(string[] splits)
        {

            var redisKeyPayloadCount = String.Join(":", splits);
            var hashEntries = cacheService.RedisDatabase?.HashGetAll(redisKeyPayloadCount);

            if (hashEntries == null)
            {
                return;
            }

            foreach (var hashEntry in hashEntries)
            {
                try
                {
                    RenameHashKeyForCorrectGuidFormat(hashEntry, redisKeyPayloadCount);
                }
                catch (Exception ex)
                {
                    log.Error($"PayloadCount key migration caused error for hashEntry {hashEntry.Name}: {ex.Message}");
                }
            }
        }
        
        private void RenameHashKeyForCorrectGuidFormat(HashEntry hashEntry, string redisKeyPayloadCount)
        {

            var guid = Guid.Parse(hashEntry.Name);
            cacheService.RedisDatabase?.HashSet(redisKeyPayloadCount, guid.ToString("N"), hashEntry.Value);
            cacheService.RedisDatabase?.HashDelete(redisKeyPayloadCount, hashEntry.Name);
        }

        private bool CheckAndChangeKeyNameGivenNumberOfSplits(MessagePackSerializerOptions messagePackSerializerOptionsOld,
            MessagePackSerializerOptions messagePackSerializerOptionsNew, string[] splits, RedisKey key, string payloadLatestPrefix, string journalPrefix)
        {
            switch (splits.Length)
            {
                case 3:
                    MigrateAllHashKeyValuesFromDictionaryToEnvelopeOfDictionaryNoBoxing(messagePackSerializerOptionsOld, messagePackSerializerOptionsNew, key);
                    return true;
                case 4:
                    splits[0] = payloadLatestPrefix;
                    break;
                case 5:
                    splits[0] = journalPrefix;
                    break;
            }
            return false;
        }
        
        private void MigrateAllHashKeyValuesFromDictionaryToEnvelopeOfDictionaryNoBoxing(MessagePackSerializerOptions messagePackSerializerOptionsOld,
            MessagePackSerializerOptions messagePackSerializerOptionsNew, RedisKey key)
        {
            foreach (var hashEntry in cacheService.RedisDatabase.HashScan(key))
            {
                try
                {
                    if (!hashEntry.Value.HasValue)
                    {
                        continue;
                    }

                    MigrateHashKeyValueFromDictionaryToEnvelopeOfDictionaryNoBoxing(messagePackSerializerOptionsOld, messagePackSerializerOptionsNew, hashEntry, key);
                }
                catch (Exception ex)
                {
                    log.Error($"Payload value migration caused for for hashEntry {hashEntry.Name}: {ex.Message}");
                }
            }
        }
        
        private void MigrateHashKeyValueFromDictionaryToEnvelopeOfDictionaryNoBoxing(MessagePackSerializerOptions messagePackSerializerOptionsOld,
            MessagePackSerializerOptions messagePackSerializerOptionsNew, HashEntry hashEntry, RedisKey key)
        {
            var oldKeyValuePairs = MessagePackSerializer.Deserialize<Dictionary<string, object>>(hashEntry.Value,
                messagePackSerializerOptionsOld);

            var bytes = SerializeToMessagePackFormatForEnvelopeDictionaryNoBoxing(messagePackSerializerOptionsNew, oldKeyValuePairs);

            cacheService.RedisDatabase.HashSetAsync(key, hashEntry.Name, bytes);
        }
        
        private byte[] SerializeToMessagePackFormatForEnvelopeDictionaryNoBoxing(MessagePackSerializerOptions messagePackSerializerOptions, Dictionary<string, object> oldKeyValuePairs)
        {
            var ms = new MemoryStream();
            MessagePackSerializer.Serialize(ms, MapToEnvelopeForDictionaryNoBoxing(oldKeyValuePairs),
                messagePackSerializerOptions);
            var bytes = ms.ToArray();
            return bytes;
        }
        
        private static EnvelopeDictionaryNoBoxing MapToEnvelopeForDictionaryNoBoxing(Dictionary<string, object> oldKeyValuePairs)
        {

            var dictionaryNoBoxingWrapper = new EnvelopeDictionaryNoBoxing
            {
                Version = 1,
                Data = MapToDictionaryNoBoxing(oldKeyValuePairs)
            };
            return dictionaryNoBoxingWrapper;
        }
        
        private static DictionaryNoBoxing MapToDictionaryNoBoxing(Dictionary<string, object> oldKeyValuePairs)
        {

            var dictionaryNoBoxing = new DictionaryNoBoxing(oldKeyValuePairs.Count);
            foreach (var oldKeyValuePair in oldKeyValuePairs)
            {
                switch (oldKeyValuePair.Value)
                {
                    case int i:
                        dictionaryNoBoxing.Add(oldKeyValuePair.Key, i);
                        break;
                    case string s:
                        dictionaryNoBoxing.Add(oldKeyValuePair.Key, s);
                        break;
                    case double d:
                        dictionaryNoBoxing.Add(oldKeyValuePair.Key, d);
                        break;
                    case DateTime d:
                        dictionaryNoBoxing.Add(oldKeyValuePair.Key, d);
                        break;
                    case bool d:
                        dictionaryNoBoxing.Add(oldKeyValuePair.Key, d);
                        break;
                    case null:
                        dictionaryNoBoxing.Add(oldKeyValuePair.Key, null);
                        break;
                }
            }
            return dictionaryNoBoxing;
        }

        public override void Down()
        {

        }
    }
}
