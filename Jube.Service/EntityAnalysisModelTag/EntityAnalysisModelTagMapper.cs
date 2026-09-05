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

using Jube.Dto.EntityAnalysisModelTag;

namespace Jube.Service.EntityAnalysisModelTag
{
    using TagPoco = Data.Poco.EntityAnalysisModelTag;

    internal static class EntityAnalysisModelTagMapper
    {
        public static EntityAnalysisModelTagDto? ToDto(TagPoco? tag)
        {
            return tag is null
                ? null
                : new EntityAnalysisModelTagDto
                {
                    Id = tag.Id,
                    EntityAnalysisModelId = tag.EntityAnalysisModelId.GetValueOrDefault(),
                    Name = tag.Name,
                    Active = tag.Active == 1,
                    Locked = tag.Locked == 1,
                    CreatedUser = tag.CreatedUser,
                    CreatedDate = ToOffset(tag.CreatedDate),
                    UpdatedUser = tag.UpdatedUser,
                    UpdatedDate = ToOffset(tag.UpdatedDate),
                    Version = tag.Version.GetValueOrDefault(),
                    DeletedUser = tag.DeletedUser,
                    DeletedDate = ToOffset(tag.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelTagDto> ToDto(IEnumerable<TagPoco>? source)
        {
            return (source ?? Enumerable.Empty<TagPoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static TagPoco ToPoco(EntityAnalysisModelTagDto dto)
        {
            return new TagPoco
            {
                Id = dto.Id,
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0)
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