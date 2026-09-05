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

using Jube.Dto.EntityAnalysisModelTtlCounter;

namespace Jube.Service.EntityAnalysisModelTtlCounter
{
    using TtlCounterPoco = Data.Poco.EntityAnalysisModelTtlCounter;

    internal static class EntityAnalysisModelTtlCounterMapper
    {
        public static EntityAnalysisModelTtlCounterDto? ToDto(TtlCounterPoco? ttlCounter)
        {
            return ttlCounter is null
                ? null
                : new EntityAnalysisModelTtlCounterDto
                {
                    Id = ttlCounter.Id,
                    EntityAnalysisModelId = ttlCounter.EntityAnalysisModelId.GetValueOrDefault(),
                    Guid = ttlCounter.Guid,
                    Name = ttlCounter.Name,
                    Active = ttlCounter.Active == 1,
                    Locked = ttlCounter.Locked == 1,
                    OnlineAggregation = ttlCounter.OnlineAggregation == 1,
                    EnableLiveForever = ttlCounter.EnableLiveForever == 1,
                    TtlCounterDataName = ttlCounter.TtlCounterDataName,
                    EnableSum = ttlCounter.EnableSum == 1,
                    TtlCounterDataValue = ttlCounter.TtlCounterDataValue,
                    TtlCounterInterval = ttlCounter.TtlCounterInterval,
                    TtlCounterValue = ttlCounter.TtlCounterValue.GetValueOrDefault(),
                    ResolutionInterval = ttlCounter.ResolutionInterval,
                    ResponsePayload = ttlCounter.ResponsePayload == 1,
                    ReportTable = ttlCounter.ReportTable == 1,
                    CreatedUser = ttlCounter.CreatedUser,
                    CreatedDate = ToOffset(ttlCounter.CreatedDate),
                    Version = ttlCounter.Version.GetValueOrDefault(),
                    DeletedUser = ttlCounter.DeletedUser,
                    DeletedDate = ToOffset(ttlCounter.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelTtlCounterDto> ToDto(IEnumerable<TtlCounterPoco>? source)
        {
            return (source ?? Enumerable.Empty<TtlCounterPoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static TtlCounterPoco ToPoco(EntityAnalysisModelTtlCounterDto dto)
        {
            return new TtlCounterPoco
            {
                Id = dto.Id,
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                OnlineAggregation = (byte)(dto.OnlineAggregation ? 1 : 0),
                EnableLiveForever = (byte)(dto.EnableLiveForever ? 1 : 0),
                TtlCounterDataName = dto.TtlCounterDataName,
                EnableSum = (byte)(dto.EnableSum ? 1 : 0),
                TtlCounterDataValue = dto.TtlCounterDataValue,
                TtlCounterInterval = dto.TtlCounterInterval,
                TtlCounterValue = dto.TtlCounterValue,
                ResolutionInterval = dto.ResolutionInterval,
                ResponsePayload = (byte)(dto.ResponsePayload ? 1 : 0),
                ReportTable = (byte)(dto.ReportTable ? 1 : 0)
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