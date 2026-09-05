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

using Jube.Dto.EntityAnalysisModelSanction;

namespace Jube.Service.EntityAnalysisModelSanction
{
    using SanctionPoco = Data.Poco.EntityAnalysisModelSanction;

    internal static class EntityAnalysisModelSanctionMapper
    {
        public static EntityAnalysisModelSanctionDto? ToDto(SanctionPoco? sanction)
        {
            return sanction is null
                ? null
                : new EntityAnalysisModelSanctionDto
                {
                    Id = sanction.Id,
                    EntityAnalysisModelId = sanction.EntityAnalysisModelId.GetValueOrDefault(),
                    Name = sanction.Name,
                    Active = sanction.Active == 1,
                    Locked = sanction.Locked == 1,
                    MultipartStringDataName = sanction.MultipartStringDataName,
                    Distance = sanction.Distance.GetValueOrDefault(),
                    AggregationTypeId = sanction.AggregationTypeId,
                    MaxDistanceRatio = sanction.MaxDistanceRatio,
                    MaxCoverageRatio = sanction.MaxCoverageRatio,
                    CacheInterval = sanction.CacheInterval.GetValueOrDefault(),
                    CacheValue = sanction.CacheValue.GetValueOrDefault(),
                    ReportTable = sanction.ReportTable == 1,
                    ResponsePayload = sanction.ResponsePayload == 1,
                    CreatedUser = sanction.CreatedUser,
                    CreatedDate = ToOffset(sanction.CreatedDate),
                    UpdatedUser = sanction.UpdatedUser,
                    UpdatedDate = ToOffset(sanction.UpdatedDate),
                    Version = sanction.Version.GetValueOrDefault(),
                    DeletedUser = sanction.DeletedUser,
                    DeletedDate = ToOffset(sanction.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelSanctionDto> ToDto(IEnumerable<SanctionPoco>? source)
        {
            return (source ?? Enumerable.Empty<SanctionPoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static SanctionPoco ToPoco(EntityAnalysisModelSanctionDto dto)
        {
            return new SanctionPoco
            {
                Id = dto.Id,
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                MultipartStringDataName = dto.MultipartStringDataName,
                Distance = (byte)dto.Distance,
                AggregationTypeId = dto.AggregationTypeId,
                MaxDistanceRatio = dto.MaxDistanceRatio,
                MaxCoverageRatio = dto.MaxCoverageRatio,
                CacheInterval = dto.CacheInterval,
                CacheValue = dto.CacheValue,
                ReportTable = (byte)(dto.ReportTable ? 1 : 0),
                ResponsePayload = (byte)(dto.ResponsePayload ? 1 : 0)
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