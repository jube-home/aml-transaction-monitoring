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

using Jube.Data.Poco;
using Jube.Dto.EntityAnalysisModelRequestXPath;

namespace Jube.Service.EntityAnalysisModelRequestXPath
{
    using RequestXPathPoco = EntityAnalysisModelRequestXpath;

    internal static class EntityAnalysisModelRequestXPathMapper
    {
        public static EntityAnalysisModelRequestXPathDto? ToDto(RequestXPathPoco? requestXPath)
        {
            return requestXPath is null
                ? null
                : new EntityAnalysisModelRequestXPathDto
                {
                    Id = requestXPath.Id,
                    EntityAnalysisModelId = requestXPath.EntityAnalysisModelId.GetValueOrDefault(),
                    Name = requestXPath.Name,
                    Active = requestXPath.Active == 1,
                    Locked = requestXPath.Locked == 1,
                    DataTypeId = requestXPath.DataTypeId.GetValueOrDefault(),
                    XPath = requestXPath.XPath,
                    DefaultValue = requestXPath.DefaultValue,
                    EncryptionId = requestXPath.EncryptionId.GetValueOrDefault(),
                    EnableSuppression = requestXPath.EnableSuppression == 1,
                    Cache = requestXPath.Cache == 1,
                    ReportTable = requestXPath.ReportTable == 1,
                    ResponsePayload = requestXPath.ResponsePayload == 1,
                    SearchKey = requestXPath.SearchKey == 1,
                    SearchKeyTtlInterval = requestXPath.SearchKeyTtlInterval,
                    SearchKeyTtlIntervalValue = requestXPath.SearchKeyTtlIntervalValue.GetValueOrDefault(),
                    SearchKeyFetchLimit = requestXPath.SearchKeyFetchLimit.GetValueOrDefault(),
                    SearchKeyCache = requestXPath.SearchKeyCache == 1,
                    SearchKeyCacheInterval = requestXPath.SearchKeyCacheInterval,
                    SearchKeyCacheValue = requestXPath.SearchKeyCacheValue.GetValueOrDefault(),
                    SearchKeyCacheSample = requestXPath.SearchKeyCacheSample == 1,
                    SearchKeyCacheFetchLimit = requestXPath.SearchKeyCacheFetchLimit.GetValueOrDefault(),
                    SearchKeyCacheTtlInterval = requestXPath.SearchKeyCacheTtlInterval,
                    SearchKeyCacheTtlValue = requestXPath.SearchKeyCacheTtlValue.GetValueOrDefault(),
                    CreatedUser = requestXPath.CreatedUser,
                    CreatedDate = ToOffset(requestXPath.CreatedDate),
                    UpdatedUser = requestXPath.UpdatedUser,
                    UpdatedDate = ToOffset(requestXPath.UpdatedDate),
                    Version = requestXPath.Version.GetValueOrDefault(),
                    DeletedUser = requestXPath.DeletedUser,
                    DeletedDate = ToOffset(requestXPath.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelRequestXPathDto> ToDto(IEnumerable<RequestXPathPoco>? source)
        {
            return (source ?? Enumerable.Empty<RequestXPathPoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static RequestXPathPoco ToPoco(EntityAnalysisModelRequestXPathDto dto)
        {
            return new RequestXPathPoco
            {
                Id = dto.Id,
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                DataTypeId = dto.DataTypeId,
                XPath = dto.XPath,
                DefaultValue = dto.DefaultValue,
                EncryptionId = (byte)dto.EncryptionId,
                EnableSuppression = (byte)(dto.EnableSuppression ? 1 : 0),
                Cache = (byte)(dto.Cache ? 1 : 0),
                ReportTable = (byte)(dto.ReportTable ? 1 : 0),
                ResponsePayload = (byte)(dto.ResponsePayload ? 1 : 0),
                SearchKey = (byte)(dto.SearchKey ? 1 : 0),
                SearchKeyTtlInterval = dto.SearchKeyTtlInterval,
                SearchKeyTtlIntervalValue = dto.SearchKeyTtlIntervalValue,
                SearchKeyFetchLimit = dto.SearchKeyFetchLimit,
                SearchKeyCache = (byte)(dto.SearchKeyCache ? 1 : 0),
                SearchKeyCacheInterval = dto.SearchKeyCacheInterval,
                SearchKeyCacheValue = dto.SearchKeyCacheValue,
                SearchKeyCacheSample = (byte)(dto.SearchKeyCacheSample ? 1 : 0),
                SearchKeyCacheFetchLimit = dto.SearchKeyCacheFetchLimit,
                SearchKeyCacheTtlInterval = dto.SearchKeyCacheTtlInterval,
                SearchKeyCacheTtlValue = dto.SearchKeyCacheTtlValue
            };
        }

        private static DateTimeOffset? ToOffset(DateTime? value)
        {
            return value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
        }
    }
}