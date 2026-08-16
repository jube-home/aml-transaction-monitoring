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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Data.Repository;
    using Helpers;
    using Parser;

    public static class SyncEntityAnalysisModelRequestXPathExtensions
    {
        public static async Task SyncEntityAnalysisModelRequestXPathAsync(this Context context)
        {
            try
            {
                context.Services.Parser.EntityAnalysisModelRequestXPaths = new Dictionary<string, EntityAnalysisModelRequestXPath>();

                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Looping through active models {key} is started for the purpose adding the XPath.");
                    }

                    var repository = new EntityAnalysisModelRequestXPathRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelRequestXPathRepository.GetByEntityAnalysisModelId for entity model key of {key}.");
                    }

                    var records = await
                        repository.GetByEntityAnalysisModelIdOrderByIdAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

                    var shadowEntityAnalysisModelRequestXPath = new List<Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelRequestXPath>();
                    var archivePayloadSqlSelect = "select a.\"EntityAnalysisModelInstanceEntryGuid\"," +
                                                  "a.\"CreatedDate\"," +
                                                  $"a.\"ReferenceDate\" AS \"{value.References.ReferenceDateName}\"";

                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: XPath ID {record.Id} returned for model {key}.");
                            }

                            if (record.Active != 1)
                            {
                                continue;
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: XPath ID {record.Id} returned for model {key} is active.");
                            }

                            var entityAnalysisModelRequestXPath = new Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelRequestXPath
                            {
                                Id = record.Id
                            };

                            if (record.Name == null)
                            {
                                entityAnalysisModelRequestXPath.Name =
                                    $"XPath_Name_{entityAnalysisModelRequestXPath.Id}";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Name value as {entityAnalysisModelRequestXPath.Name}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.Name = record.Name.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Name value as {entityAnalysisModelRequestXPath.Name}.");
                                }
                            }

                            if (!record.DataTypeId.HasValue)
                            {
                                entityAnalysisModelRequestXPath.DataTypeId = record.DataTypeId.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Data Type value as {entityAnalysisModelRequestXPath.DataTypeId}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.DataTypeId = record.DataTypeId.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Data Type value as {entityAnalysisModelRequestXPath.DataTypeId}.");
                                }
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                entityAnalysisModelRequestXPath.ReportTable = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Promote Report Table value as {entityAnalysisModelRequestXPath.ReportTable}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.ReportTable = record.ReportTable.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Promote Report Table value as {entityAnalysisModelRequestXPath.ReportTable}.");
                                }
                            }

                            if (record.XPath == null)
                            {
                                entityAnalysisModelRequestXPath.XPath = "$." + record.Name.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT XPath value as {entityAnalysisModelRequestXPath.XPath}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.XPath = record.XPath;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set XPath value as {entityAnalysisModelRequestXPath.XPath}.");
                                }
                            }

                            if (record.DefaultValue == null)
                            {
                                switch (entityAnalysisModelRequestXPath.DataTypeId)
                                {
                                    case 1:
                                        entityAnalysisModelRequestXPath.DefaultValue = "default";

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default String value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                        }

                                        break;
                                    case 2:
                                        entityAnalysisModelRequestXPath.DefaultValue = "0";

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Integer value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                        }

                                        break;
                                    case 3:
                                        entityAnalysisModelRequestXPath.DefaultValue = "0";

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Float value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                        }

                                        break;
                                    case 4:
                                        entityAnalysisModelRequestXPath.DefaultValue = "0";

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Date value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                        }

                                        break;
                                    case 5:
                                        entityAnalysisModelRequestXPath.DefaultValue = "0";

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Boolean value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                        }

                                        break;
                                    case 6:
                                        entityAnalysisModelRequestXPath.DefaultValue = "0";

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Latitude value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                        }

                                        break;
                                    case 7:
                                        entityAnalysisModelRequestXPath.DefaultValue = "0";

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Default Value {entityAnalysisModelRequestXPath.Id} set DEFAULT Default Longitude value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                        }

                                        break;
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.DefaultValue = record.DefaultValue;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Default value as {entityAnalysisModelRequestXPath.DefaultValue}.");
                                }
                            }

                            if (!record.SearchKey.HasValue)
                            {
                                entityAnalysisModelRequestXPath.SearchKey = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key value as {entityAnalysisModelRequestXPath.SearchKey}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKey = record.SearchKey.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key value as {entityAnalysisModelRequestXPath.SearchKey}.");
                                }
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                entityAnalysisModelRequestXPath.ResponsePayload = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set DEFAULT Response Payload value as {entityAnalysisModelRequestXPath.ResponsePayload}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.ResponsePayload = record.ResponsePayload == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set Response Payload value as {entityAnalysisModelRequestXPath.ResponsePayload}.");
                                }
                            }

                            if (!record.Cache.HasValue)
                            {
                                entityAnalysisModelRequestXPath.Cache = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set DEFAULT Cache value as {entityAnalysisModelRequestXPath.Cache}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.Cache = record.Cache == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set Cache value as {entityAnalysisModelRequestXPath.Cache}.");
                                }
                            }

                            if (!record.CacheIndexId.HasValue)
                            {
                                entityAnalysisModelRequestXPath.CacheIndexId = record.Id;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set DEFAULT Cache Index Id value as {entityAnalysisModelRequestXPath.Id}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.CacheIndexId = record.CacheIndexId.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set Cache Index Id value as {entityAnalysisModelRequestXPath.CacheIndexId}.");
                                }
                            }

                            if (!record.EncryptionId.HasValue)
                            {
                                entityAnalysisModelRequestXPath.EncryptionId = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set DEFAULT Encryption Id value as {entityAnalysisModelRequestXPath.EncryptionId}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.EncryptionId = record.EncryptionId.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set Encryption Id value as {entityAnalysisModelRequestXPath.EncryptionId}.");
                                }
                            }

                            if (!record.EnableSuppression.HasValue)
                            {
                                entityAnalysisModelRequestXPath.EnableSuppression = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set DEFAULT Enable Suppression value as {entityAnalysisModelRequestXPath.EnableSuppression}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.EnableSuppression = record.EnableSuppression == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Response Payload {entityAnalysisModelRequestXPath.Id} set Enable Suppression value as {entityAnalysisModelRequestXPath.EnableSuppression}.");
                                }
                            }

                            if (!record.SearchKeyCache.HasValue)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCache = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Cache value as {entityAnalysisModelRequestXPath.SearchKeyCache}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCache = record.SearchKeyCache.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Cache value as {entityAnalysisModelRequestXPath.SearchKeyCache}.");
                                }
                            }

                            if (!record.SearchKeyCacheSample.HasValue)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheSample = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Cache Sample value as {entityAnalysisModelRequestXPath.SearchKeyCacheSample}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheSample = record.SearchKeyCacheSample == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Cache Sample value as {entityAnalysisModelRequestXPath.SearchKeyCacheSample}.");
                                }
                            }

                            if (record.SearchKeyCacheInterval == null)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheInterval = "h";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Cache Interval Type value as {entityAnalysisModelRequestXPath.SearchKeyCacheInterval}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheInterval =
                                    record.SearchKeyCacheInterval;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Cache Interval Type value as {entityAnalysisModelRequestXPath.SearchKeyCacheInterval}.");
                                }
                            }

                            if (record.SearchKeyTtlInterval == null)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyTtlInterval = "h";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Interval Type value as {entityAnalysisModelRequestXPath.SearchKeyTtlInterval}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyTtlInterval =
                                    record.SearchKeyTtlInterval;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Interval Type value as {entityAnalysisModelRequestXPath.SearchKeyTtlInterval}.");
                                }
                            }

                            if (!record.SearchKeyCacheValue.HasValue)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheValue = 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Interval value as {entityAnalysisModelRequestXPath.SearchKeyCacheValue}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheValue =
                                    record.SearchKeyCacheValue.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Interval value as {entityAnalysisModelRequestXPath.SearchKeyCacheValue}.");
                                }
                            }

                            if (!record.SearchKeyCacheTtlValue.HasValue)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheTtlValue = 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key Cache TTL Interval value as {entityAnalysisModelRequestXPath.SearchKeyCacheTtlValue}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheTtlValue =
                                    record.SearchKeyCacheTtlValue.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Cache TTL Interval value as {entityAnalysisModelRequestXPath.SearchKeyCacheTtlValue}.");
                                }
                            }

                            if (!record.SearchKeyTtlIntervalValue.HasValue)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyTtlIntervalValue = 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Search Key TTL Interval value as {entityAnalysisModelRequestXPath.SearchKeyTtlIntervalValue}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyTtlIntervalValue =
                                    record.SearchKeyTtlIntervalValue.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Search Key Cache TTL Interval value as {entityAnalysisModelRequestXPath.SearchKeyTtlIntervalValue}.");
                                }
                            }

                            if (!record.SearchKeyCacheFetchLimit.HasValue)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheFetchLimit = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT Cache Limit value as {entityAnalysisModelRequestXPath.SearchKeyCacheFetchLimit}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyCacheFetchLimit =
                                    record.SearchKeyCacheFetchLimit.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set Cache Limit value as {entityAnalysisModelRequestXPath.SearchKeyCacheFetchLimit}.");
                                }
                            }

                            if (!record.SearchKeyFetchLimit.HasValue)
                            {
                                entityAnalysisModelRequestXPath.SearchKeyFetchLimit = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set DEFAULT search limit value as {entityAnalysisModelRequestXPath.SearchKeyFetchLimit}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelRequestXPath.SearchKeyFetchLimit =
                                    record.SearchKeyFetchLimit.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and XPath {entityAnalysisModelRequestXPath.Id} set search limit value as {entityAnalysisModelRequestXPath.SearchKeyFetchLimit}.");
                                }
                            }

                            var databaseType = entityAnalysisModelRequestXPath.DataTypeId switch
                            {
                                2 => "::int",
                                3 => "::float8",
                                4 => "::timestamp",
                                5 => "::boolean",
                                6 => "::float8",
                                7 => "::float8",
                                _ => ""
                            };

                            archivePayloadSqlSelect +=
                                $",(a.\"Json\" -> 'payload' ->> '{entityAnalysisModelRequestXPath.Name}'){databaseType} AS \"{entityAnalysisModelRequestXPath.Name}\"";

                            shadowEntityAnalysisModelRequestXPath.Add(entityAnalysisModelRequestXPath);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: XPath ID {entityAnalysisModelRequestXPath.Id} added to shadow list for model {key}.");
                            }

                            if (!context.Services.Parser.EntityAnalysisModelRequestXPaths.ContainsKey(entityAnalysisModelRequestXPath
                                    .Name))
                            {
                                context.Services.Parser.EntityAnalysisModelRequestXPaths.Add(entityAnalysisModelRequestXPath.Name,
                                    new EntityAnalysisModelRequestXPath
                                    {
                                        DataTypeId = entityAnalysisModelRequestXPath.DataTypeId,
                                        DefaultValue = entityAnalysisModelRequestXPath.DefaultValue,
                                        Cache = entityAnalysisModelRequestXPath.Cache
                                    });
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: XPath ID {entityAnalysisModelRequestXPath.Id} added {key} with data type {entityAnalysisModelRequestXPath.DataTypeId} to context.Parser.");
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: XPath ID {record.Id} returned for model {key} has created an error as {ex}.");
                        }
                    }

                    value.References.ArchivePayloadSqlSelect = archivePayloadSqlSelect;
                    value.References.ArchivePayloadSqlBody = " From \"Archive\" a" +
                                                             " inner join \"EntityAnalysisModel\" m on a.\"EntityAnalysisModelId\" = m.\"Id\"  where "
                                                             + "m.\"Guid\" = '" + value.Instance.Guid + "'::uuid"
                                                             + " and \"ReferenceDate\" >= (@adjustedStartDate) " +
                                                             "order by m.\"Id\" asc limit (@limit) offset (@skip);";

                    value.Collections.EntityAnalysisModelRequestXPaths = shadowEntityAnalysisModelRequestXPath;
                    value.References.PayloadInitialSize = DictionaryNoBoxingHelpers.CalculateInitialSize(value);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Shadow XPath list set to list of xpath for model {key} and reader closed.");
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: Completed adding XPath to entity models.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelRequestXPathAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.RequestXPath, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }
        }
    }
}
