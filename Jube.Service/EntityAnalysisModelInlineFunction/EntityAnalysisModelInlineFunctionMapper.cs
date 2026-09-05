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

using Jube.Dto.EntityAnalysisModelInlineFunction;

namespace Jube.Service.EntityAnalysisModelInlineFunction
{
    using InlineFunctionPoco = Data.Poco.EntityAnalysisModelInlineFunction;

    internal static class EntityAnalysisModelInlineFunctionMapper
    {
        public static EntityAnalysisModelInlineFunctionDto? ToDto(InlineFunctionPoco? inlineFunction)
        {
            return inlineFunction is null
                ? null
                : new EntityAnalysisModelInlineFunctionDto
                {
                    Id = inlineFunction.Id,
                    EntityAnalysisModelId = inlineFunction.EntityAnalysisModelId.GetValueOrDefault(),
                    Name = inlineFunction.Name,
                    Active = inlineFunction.Active == 1,
                    Locked = inlineFunction.Locked == 1,
                    ReturnDataTypeId = inlineFunction.ReturnDataTypeId.GetValueOrDefault(),
                    FunctionScript = inlineFunction.FunctionScript,
                    EncryptionId = inlineFunction.EncryptionId.GetValueOrDefault(),
                    ReportTable = inlineFunction.ReportTable == 1,
                    ResponsePayload = inlineFunction.ResponsePayload == 1,
                    CreatedUser = inlineFunction.CreatedUser,
                    CreatedDate = ToOffset(inlineFunction.CreatedDate),
                    UpdatedUser = inlineFunction.UpdatedUser,
                    UpdatedDate = ToOffset(inlineFunction.UpdatedDate),
                    Version = inlineFunction.Version.GetValueOrDefault(),
                    DeletedUser = inlineFunction.DeletedUser,
                    DeletedDate = ToOffset(inlineFunction.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelInlineFunctionDto> ToDto(IEnumerable<InlineFunctionPoco>? source)
        {
            return (source ?? Enumerable.Empty<InlineFunctionPoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static InlineFunctionPoco ToPoco(EntityAnalysisModelInlineFunctionDto dto)
        {
            return new InlineFunctionPoco
            {
                Id = dto.Id,
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                ReturnDataTypeId = dto.ReturnDataTypeId,
                FunctionScript = dto.FunctionScript,
                EncryptionId = (byte)dto.EncryptionId,
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