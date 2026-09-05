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

using Jube.Dto.ExhaustiveSearchInstance;

namespace Jube.Service.ExhaustiveSearchInstance
{
    using ExhaustiveSearchInstancePoco = Data.Poco.ExhaustiveSearchInstance;

    internal static class ExhaustiveSearchInstanceMapper
    {
        public static ExhaustiveSearchInstanceDto? ToDto(ExhaustiveSearchInstancePoco? exhaustiveSearchInstance)
        {
            return exhaustiveSearchInstance is null
                ? null
                : new ExhaustiveSearchInstanceDto
                {
                    Id = exhaustiveSearchInstance.Id,
                    EntityAnalysisModelId = exhaustiveSearchInstance.EntityAnalysisModelId.GetValueOrDefault(),
                    Name = exhaustiveSearchInstance.Name,
                    Active = exhaustiveSearchInstance.Active == 1,
                    Locked = exhaustiveSearchInstance.Locked == 1,
                    Guid = exhaustiveSearchInstance.Guid,
                    StatusId = exhaustiveSearchInstance.StatusId.GetValueOrDefault(),
                    Anomaly = exhaustiveSearchInstance.Anomaly == 1,
                    AnomalyProbability = exhaustiveSearchInstance.AnomalyProbability,
                    Filter = exhaustiveSearchInstance.Filter == 1,
                    FilterJson = exhaustiveSearchInstance.FilterJson,
                    FilterSql = exhaustiveSearchInstance.FilterSql,
                    FilterTokens = exhaustiveSearchInstance.FilterTokens,
                    Models = exhaustiveSearchInstance.Models.GetValueOrDefault(),
                    ModelsSinceBest = exhaustiveSearchInstance.ModelsSinceBest.GetValueOrDefault(),
                    Score = exhaustiveSearchInstance.Score.GetValueOrDefault(),
                    TopologyComplexity = exhaustiveSearchInstance.TopologyComplexity.GetValueOrDefault(),
                    CompletedDate = ToOffset(exhaustiveSearchInstance.CompletedDate),
                    ReportTable = exhaustiveSearchInstance.ReportTable == 1,
                    ResponsePayload = exhaustiveSearchInstance.ResponsePayload == 1,
                    CreatedUser = exhaustiveSearchInstance.CreatedUser,
                    CreatedDate = ToOffset(exhaustiveSearchInstance.CreatedDate),
                    UpdatedUser = null,
                    UpdatedDate = ToOffset(exhaustiveSearchInstance.UpdatedDate),
                    Version = exhaustiveSearchInstance.Version.GetValueOrDefault(),
                    DeletedUser = exhaustiveSearchInstance.DeletedUser,
                    DeletedDate = ToOffset(exhaustiveSearchInstance.DeletedDate)
                };
        }

        public static List<ExhaustiveSearchInstanceDto> ToDto(IEnumerable<ExhaustiveSearchInstancePoco>? source)
        {
            return (source ?? Enumerable.Empty<ExhaustiveSearchInstancePoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static ExhaustiveSearchInstancePoco ToPoco(ExhaustiveSearchInstanceDto dto)
        {
            return new ExhaustiveSearchInstancePoco
            {
                Id = dto.Id,
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                Anomaly = (byte)(dto.Anomaly ? 1 : 0),
                AnomalyProbability = dto.AnomalyProbability,
                Filter = (byte)(dto.Filter ? 1 : 0),
                FilterJson = dto.FilterJson,
                FilterSql = dto.FilterSql,
                FilterTokens = dto.FilterTokens,
                ReportTable = (byte)(dto.ReportTable ? 1 : 0),
                ResponsePayload = (byte)(dto.ResponsePayload ? 1 : 0),
                // Training-progress fields are exclusively engine-managed (see the migration report) -- never
                // taken from client input, but explicitly zeroed here (not left null) since Create/Update return
                // this Poco directly to the client and the legacy contract always reported these as 0, not null
                // (a null StatusId falls through the page's status switch to "Stopped for reasons unexpected").
                StatusId = 0,
                Models = 0,
                ModelsSinceBest = 0,
                Score = 0,
                TopologyComplexity = 0
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
