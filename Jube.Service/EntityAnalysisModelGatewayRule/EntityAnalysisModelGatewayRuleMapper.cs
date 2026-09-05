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

using Jube.Dto.EntityAnalysisModelGatewayRule;

namespace Jube.Service.EntityAnalysisModelGatewayRule
{
    using GatewayRulePoco = Data.Poco.EntityAnalysisModelGatewayRule;

    internal static class EntityAnalysisModelGatewayRuleMapper
    {
        public static EntityAnalysisModelGatewayRuleDto? ToDto(GatewayRulePoco? gatewayRule)
        {
            return gatewayRule is null
                ? null
                : new EntityAnalysisModelGatewayRuleDto
                {
                    Id = gatewayRule.Id,
                    EntityAnalysisModelId = gatewayRule.EntityAnalysisModelId.GetValueOrDefault(),
                    Name = gatewayRule.Name,
                    Description = null,
                    Priority = gatewayRule.Priority.GetValueOrDefault(),
                    BuilderRuleScript = gatewayRule.BuilderRuleScript,
                    Json = gatewayRule.Json,
                    RuleScriptTypeId = gatewayRule.RuleScriptTypeId.GetValueOrDefault(),
                    CoderRuleScript = gatewayRule.CoderRuleScript,
                    MaxResponseElevation = (int)gatewayRule.MaxResponseElevation.GetValueOrDefault(),
                    GatewaySample = gatewayRule.GatewaySample.GetValueOrDefault(),
                    ResponsePayload = false,
                    Active = gatewayRule.Active == 1,
                    Locked = gatewayRule.Locked == 1,
                    ActivationCounter = (int)gatewayRule.ActivationCounter.GetValueOrDefault(),
                    ActivationCounterDate = ToOffset(gatewayRule.ActivationCounterDate),
                    EvaluationCounter = gatewayRule.EvaluationCounter.GetValueOrDefault(),
                    CreatedUser = gatewayRule.CreatedUser,
                    CreatedDate = ToOffset(gatewayRule.CreatedDate),
                    UpdatedUser = gatewayRule.UpdatedUser,
                    UpdatedDate = ToOffset(gatewayRule.UpdatedDate),
                    Version = gatewayRule.Version.GetValueOrDefault(),
                    DeletedUser = gatewayRule.DeletedUser,
                    DeletedDate = ToOffset(gatewayRule.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelGatewayRuleDto> ToDto(IEnumerable<GatewayRulePoco>? source)
        {
            return (source ?? Enumerable.Empty<GatewayRulePoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static GatewayRulePoco ToPoco(EntityAnalysisModelGatewayRuleDto dto)
        {
            return new GatewayRulePoco
            {
                Id = dto.Id,
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Name = dto.Name,
                Priority = dto.Priority,
                BuilderRuleScript = dto.BuilderRuleScript,
                // The Json column is jsonb -- an empty string (sent when RuleScriptTypeId selects the Coder
                // surface, which doesn't populate Json) is not valid JSON, so it must go through as null.
                Json = string.IsNullOrWhiteSpace(dto.Json) ? null : dto.Json,
                RuleScriptTypeId = (byte)dto.RuleScriptTypeId,
                CoderRuleScript = dto.CoderRuleScript,
                MaxResponseElevation = dto.MaxResponseElevation,
                GatewaySample = dto.GatewaySample,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                ActivationCounter = dto.ActivationCounter,
                ActivationCounterDate = dto.ActivationCounterDate?.UtcDateTime,
                EvaluationCounter = dto.EvaluationCounter
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