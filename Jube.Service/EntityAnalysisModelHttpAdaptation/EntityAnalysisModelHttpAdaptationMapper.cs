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

using Jube.Dto.EntityAnalysisModelHttpAdaptation;

namespace Jube.Service.EntityAnalysisModelHttpAdaptation
{
    using HttpAdaptationPoco = Data.Poco.EntityAnalysisModelHttpAdaptation;

    internal static class EntityAnalysisModelHttpAdaptationMapper
    {
        public static EntityAnalysisModelHttpAdaptationDto? ToDto(HttpAdaptationPoco? httpAdaptation)
        {
            return httpAdaptation is null
                ? null
                : new EntityAnalysisModelHttpAdaptationDto
                {
                    Id = httpAdaptation.Id,
                    EntityAnalysisModelId = httpAdaptation.EntityAnalysisModelId.GetValueOrDefault(),
                    Name = httpAdaptation.Name,
                    Active = httpAdaptation.Active == 1,
                    Locked = httpAdaptation.Locked == 1,
                    HttpEndpoint = httpAdaptation.HttpEndpoint,
                    Priority = httpAdaptation.Priority.GetValueOrDefault(),
                    ReportTable = httpAdaptation.ReportTable == 1,
                    ResponsePayload = httpAdaptation.ResponsePayload == 1,
                    InheritedId = httpAdaptation.InheritedId.GetValueOrDefault(),
                    CreatedUser = httpAdaptation.CreatedUser,
                    CreatedDate = ToOffset(httpAdaptation.CreatedDate),
                    UpdatedUser = httpAdaptation.UpdatedUser,
                    UpdatedDate = ToOffset(httpAdaptation.UpdatedDate),
                    Version = httpAdaptation.Version.GetValueOrDefault(),
                    DeletedUser = httpAdaptation.DeletedUser,
                    DeletedDate = ToOffset(httpAdaptation.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelHttpAdaptationDto> ToDto(IEnumerable<HttpAdaptationPoco>? source)
        {
            return (source ?? Enumerable.Empty<HttpAdaptationPoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static HttpAdaptationPoco ToPoco(EntityAnalysisModelHttpAdaptationDto dto)
        {
            return new HttpAdaptationPoco
            {
                Id = dto.Id,
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                HttpEndpoint = dto.HttpEndpoint,
                Priority = dto.Priority,
                ReportTable = (byte)(dto.ReportTable ? 1 : 0),
                ResponsePayload = (byte)(dto.ResponsePayload ? 1 : 0),
                InheritedId = dto.InheritedId
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