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

using Jube.Dto.EntityAnalysisModelAbstractionCalculation;

namespace Jube.Service.EntityAnalysisModelAbstractionCalculation
{
    using AbstractionCalculationPoco = Data.Poco.EntityAnalysisModelAbstractionCalculation;

    internal static class EntityAnalysisModelAbstractionCalculationMapper
    {
        public static EntityAnalysisModelAbstractionCalculationDto? ToDto(
            AbstractionCalculationPoco? abstractionCalculation)
        {
            return abstractionCalculation is null
                ? null
                : new EntityAnalysisModelAbstractionCalculationDto
                {
                    Id = abstractionCalculation.Id,
                    EntityAnalysisModelId = abstractionCalculation.EntityAnalysisModelId.GetValueOrDefault(),
                    Name = abstractionCalculation.Name,
                    Active = abstractionCalculation.Active == 1,
                    Locked = abstractionCalculation.Locked == 1,
                    EntityAnalysisModelAbstractionNameLeft =
                        abstractionCalculation.EntityAnalysisModelAbstractionNameLeft,
                    EntityAnalysisModelAbstractionNameRight =
                        abstractionCalculation.EntityAnalysisModelAbstractionNameRight,
                    AbstractionCalculationTypeId =
                        abstractionCalculation.AbstractionCalculationTypeId.GetValueOrDefault(),
                    ResponsePayload = abstractionCalculation.ResponsePayload == 1,
                    ReportTable = abstractionCalculation.ReportTable == 1,
                    FunctionScript = abstractionCalculation.FunctionScript,
                    CreatedUser = abstractionCalculation.CreatedUser,
                    CreatedDate = ToOffset(abstractionCalculation.CreatedDate),
                    UpdatedUser = abstractionCalculation.UpdatedUser,
                    UpdatedDate = ToOffset(abstractionCalculation.UpdatedDate),
                    Version = abstractionCalculation.Version.GetValueOrDefault(),
                    DeletedUser = abstractionCalculation.DeletedUser,
                    DeletedDate = ToOffset(abstractionCalculation.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelAbstractionCalculationDto> ToDto(
            IEnumerable<AbstractionCalculationPoco>? source)
        {
            return (source ?? Enumerable.Empty<AbstractionCalculationPoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static AbstractionCalculationPoco ToPoco(EntityAnalysisModelAbstractionCalculationDto dto)
        {
            return new AbstractionCalculationPoco
            {
                Id = dto.Id,
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                EntityAnalysisModelAbstractionNameLeft = dto.EntityAnalysisModelAbstractionNameLeft,
                EntityAnalysisModelAbstractionNameRight = dto.EntityAnalysisModelAbstractionNameRight,
                AbstractionCalculationTypeId = dto.AbstractionCalculationTypeId,
                ResponsePayload = (byte)(dto.ResponsePayload ? 1 : 0),
                ReportTable = (byte)(dto.ReportTable ? 1 : 0),
                FunctionScript = dto.FunctionScript
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