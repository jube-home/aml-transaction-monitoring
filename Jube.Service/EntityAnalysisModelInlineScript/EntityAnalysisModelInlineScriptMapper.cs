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

using Jube.Dto.EntityAnalysisModelInlineScript;

namespace Jube.Service.EntityAnalysisModelInlineScript
{
    using InlineScriptPoco = Data.Poco.EntityAnalysisModelInlineScript;

    internal static class EntityAnalysisModelInlineScriptMapper
    {
        public static EntityAnalysisModelInlineScriptDto? ToDto(InlineScriptPoco? inlineScript)
        {
            return inlineScript is null
                ? null
                : new EntityAnalysisModelInlineScriptDto
                {
                    Id = inlineScript.Id,
                    EntityAnalysisModelId = inlineScript.EntityAnalysisModelId.GetValueOrDefault(),
                    Name = inlineScript.Name,
                    Active = inlineScript.Active == 1,
                    Locked = inlineScript.Locked == 1,
                    EntityAnalysisInlineScriptId = inlineScript.EntityAnalysisInlineScriptId.GetValueOrDefault(),
                    CreatedUser = inlineScript.CreatedUser,
                    CreatedDate = ToOffset(inlineScript.CreatedDate),
                    UpdatedUser = inlineScript.UpdatedUser,
                    UpdatedDate = ToOffset(inlineScript.UpdatedDate),
                    Version = inlineScript.Version.GetValueOrDefault(),
                    DeletedUser = inlineScript.DeletedUser,
                    DeletedDate = ToOffset(inlineScript.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelInlineScriptDto> ToDto(IEnumerable<InlineScriptPoco>? source)
        {
            return (source ?? Enumerable.Empty<InlineScriptPoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static InlineScriptPoco ToPoco(EntityAnalysisModelInlineScriptDto dto)
        {
            return new InlineScriptPoco
            {
                Id = dto.Id,
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                EntityAnalysisInlineScriptId = dto.EntityAnalysisInlineScriptId
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