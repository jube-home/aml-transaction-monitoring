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

namespace Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload
{
    using System;
    using System.Collections.Generic;
    using CaseManagement;
    using Data.Poco;
    using Dictionary;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using TasksPerformance;
    using Adaptation=HttpAdaptationProtocol.Adaptation;

    public class EntityAnalysisModelInstanceEntryPayload
    {
        [JsonProperty(Order = 1)] public DictionaryNoBoxing<string> Payload { get; set; }
        [JsonProperty(Order = 2)] public ResponseElevation ResponseElevation { get; set; } = new ResponseElevation();
        [JsonProperty(Order = 3)] public string EntityAnalysisModelInstanceName { get; set; }
        [JsonProperty(Order = 4)] public double R { get; set; }
        [JsonProperty(Order = 5)] public DateTime CreatedDate { get; set; }
        [JsonProperty(Order = 6)] public Guid EntityAnalysisModelGuid { get; set; }
        [JsonProperty(Order = 7)] public string EntityAnalysisModelName { get; set; }
        [JsonProperty(Order = 8)] public Guid EntityAnalysisModelInstanceGuid { get; set; }
        [JsonProperty(Order = 9)] public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
        [JsonProperty(Order = 10)] public string EntityInstanceEntryId { get; set; }
        [JsonProperty(Order = 11)] public double ResponseElevationLimit { get; set; }
        [JsonProperty(Order = 12)] public DateTime ReferenceDate { get; set; }
        [JsonProperty(Order = 13)] public int? EntityAnalysisModelReprocessingRuleInstanceId { get; set; }
        [JsonProperty(Order = 14)] public DateTime ArchiveEnqueueDate { get; set; }
        [JsonProperty(Order = 15)] public bool MatchedGatewayRule { get; set; }
        [JsonProperty(Order = 16)] public int? PrevailingEntityAnalysisModelActivationRuleId { get; set; }
        [JsonProperty(Order = 17)] public string PrevailingEntityAnalysisModelActivationRuleName { get; set; }
        [JsonProperty(Order = 18)] public int EntityAnalysisModelActivationRuleCount { get; set; }
        [JsonProperty(Order = 19)] public PooledDictionary<string, double> Dictionary { get; set; }
        [JsonProperty(Order = 20)] public PooledDictionary<string, double> TtlCounter { get; set; }
        [JsonProperty(Order = 21)] public PooledDictionary<string, double> Sanction { get; set; }
        [JsonProperty(Order = 22)] public PooledDictionary<string, double> Abstraction { get; set; }
        [JsonProperty(Order = 23)] public PooledDictionary<string, double> AbstractionCalculation { get; set; }
        [JsonProperty(Order = 24)] public PooledDictionary<string, Adaptation> HttpAdaptation { get; set; }
        [JsonProperty(Order = 25)] public PooledDictionary<string, double> ExhaustiveAdaptation { get; set; }
        [JsonProperty(Order = 26)] public PooledDictionary<string, EntityModelActivationRulePayload> Activation { get; set; }
        [JsonProperty(Order = 27)] public CreateCase CreateCase { get; set; }
        [JsonProperty(Order = 28)] public string[] Tag { get; set; }
        [JsonProperty(Order = 30)] public InvokeTaskPerformance InvokeTaskPerformance { get; set; }
        [JsonIgnore] public byte[] ArchiveJson { get; set; }
        [JsonIgnore] public byte[] ResponseJson { get; set; }
        [JsonIgnore] public List<ArchiveKey> ArchiveKeys { get; set; }
        [JsonIgnore] public bool EnableRdbmsArchive { get; init; }
        [JsonIgnore] public int EntityAnalysisModelId { get; init; }
        [JsonIgnore] public int TenantRegistryId { get; init; }
        [JsonIgnore] public JObject JObject { get; set; }
    }
}
