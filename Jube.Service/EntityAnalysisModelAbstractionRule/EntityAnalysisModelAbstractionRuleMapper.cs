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

using Jube.Dto.EntityAnalysisModelAbstractionRule;

namespace Jube.Service.EntityAnalysisModelAbstractionRule
{
    using AbstractionRulePoco = Data.Poco.EntityAnalysisModelAbstractionRule;

    internal static class EntityAnalysisModelAbstractionRuleMapper
    {
        public static EntityAnalysisModelAbstractionRuleDto? ToDto(AbstractionRulePoco? abstractionRule)
        {
            return abstractionRule is null
                ? null
                : new EntityAnalysisModelAbstractionRuleDto
                {
                    Id = abstractionRule.Id,
                    EntityAnalysisModelId = abstractionRule.EntityAnalysisModelId.GetValueOrDefault(),
                    Name = abstractionRule.Name,
                    Active = abstractionRule.Active == 1,
                    Locked = abstractionRule.Locked == 1,
                    RuleScriptTypeId = abstractionRule.RuleScriptTypeId.GetValueOrDefault(),
                    BuilderRuleScript = abstractionRule.BuilderRuleScript,
                    Json = abstractionRule.Json,
                    CoderRuleScript = abstractionRule.CoderRuleScript,
                    Search = abstractionRule.Search == 1,
                    SearchKey = abstractionRule.SearchKey,
                    SearchValue = abstractionRule.SearchValue,
                    SearchInterval = abstractionRule.SearchInterval,
                    SearchFunctionTypeId = abstractionRule.SearchFunctionTypeId.GetValueOrDefault(),
                    SearchFunctionKey = abstractionRule.SearchFunctionKey,
                    Offset = abstractionRule.Offset == 1,
                    OffsetTypeId = abstractionRule.OffsetTypeId.GetValueOrDefault(),
                    OffsetValue = abstractionRule.OffsetValue.GetValueOrDefault(),
                    ReportTable = abstractionRule.ReportTable == 1,
                    ResponsePayload = abstractionRule.ResponsePayload == 1,
                    InheritedId = abstractionRule.InheritedId.GetValueOrDefault(),
                    CreatedUser = abstractionRule.CreatedUser,
                    CreatedDate = ToOffset(abstractionRule.CreatedDate),
                    UpdatedUser = abstractionRule.UpdatedUser,
                    UpdatedDate = ToOffset(abstractionRule.UpdatedDate),
                    Version = abstractionRule.Version.GetValueOrDefault(),
                    DeletedUser = abstractionRule.DeletedUser,
                    DeletedDate = ToOffset(abstractionRule.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelAbstractionRuleDto> ToDto(IEnumerable<AbstractionRulePoco>? source)
        {
            return (source ?? Enumerable.Empty<AbstractionRulePoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static AbstractionRulePoco ToPoco(EntityAnalysisModelAbstractionRuleDto dto)
        {
            return new AbstractionRulePoco
            {
                Id = dto.Id,
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                RuleScriptTypeId = (byte)dto.RuleScriptTypeId,
                BuilderRuleScript = dto.BuilderRuleScript,
                // The Json column is jsonb -- an empty string (sent when RuleScriptTypeId selects the Coder
                // surface, which doesn't populate Json) is not valid JSON, so it must go through as null.
                Json = string.IsNullOrWhiteSpace(dto.Json) ? null : dto.Json,
                CoderRuleScript = dto.CoderRuleScript,
                Search = (byte)(dto.Search ? 1 : 0),
                SearchKey = dto.SearchKey,
                SearchValue = dto.SearchValue,
                SearchInterval = dto.SearchInterval,
                SearchFunctionTypeId = dto.SearchFunctionTypeId,
                SearchFunctionKey = dto.SearchFunctionKey,
                Offset = (byte)(dto.Offset ? 1 : 0),
                OffsetTypeId = (byte)dto.OffsetTypeId,
                OffsetValue = dto.OffsetValue,
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