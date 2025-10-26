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

namespace Jube.Engine.Model.Processing.Payload
{
    using System;
    using System.Collections.Generic;
    using Cache.Redis.Serialization.DictionaryNoBoxing.Newtonsoft;
    using CaseManagement;
    using Data.Poco;
    using Dictionary;
    using Newtonsoft.Json;

    public class EntityModelActivationRulePayload
    {
        public bool Visible { get; set; }
    }

    public class EntityAnalysisModelInstanceEntryPayload
    {
        [JsonProperty(Order = 1)] [JsonConverter(typeof(DictionaryNoBoxingValueOnlyNewtonsoftConverter))] public DictionaryNoBoxing Payload { get; set; }
        
        [JsonProperty(Order = 2)] public ResponseElevation ResponseElevation { get; set; } = new ResponseElevation();

        [JsonProperty(Order = 3)] public string EntityAnalysisModelInstanceName { get; set; }

        [JsonProperty(Order = 4)] public double R { get; set; }

        [JsonProperty(Order = 5)] public DateTime CreatedDate { get; set; }

        [JsonProperty(Order = 6)] public Guid EntityAnalysisModelGuid { get; set; }

        [JsonProperty(Order = 7)] public string EntityAnalysisModelName { get; set; }

        [JsonProperty(Order = 8)] public Guid EntityAnalysisModelInstanceGuid { get; set; }

        [JsonProperty(Order = 9)] public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }

        [JsonProperty(Order = 10)] public string EntityInstanceEntryId { get; set; }

        [JsonProperty(Order = 11)] public DateTime ReferenceDate { get; set; }

        [JsonProperty(Order = 12)] public bool Reprocess { get; set; }

        [JsonProperty(Order = 13)] public DateTime ArchiveEnqueueDate { get; set; }

        [JsonProperty(Order = 14)] public int? PrevailingEntityAnalysisModelActivationRuleId { get; set; }

        [JsonProperty(Order = 15)] public string PrevailingEntityAnalysisModelActivationRuleName { get; set; }

        [JsonProperty(Order = 16)] public int EntityAnalysisModelActivationRuleCount { get; set; }

        [JsonProperty(Order = 17)] public PooledDictionary<string, double> Dictionary { get; set; }

        [JsonProperty(Order = 18)] public PooledDictionary<string, int> TtlCounter { get; set; }

        [JsonProperty(Order = 19)] public PooledDictionary<string, double> Sanction { get; set; }

        [JsonProperty(Order = 20)]
        public PooledDictionary<string, double> Abstraction { get; set; }

        [JsonProperty(Order = 21)] public PooledDictionary<string, double> AbstractionCalculation { get; set; }

        [JsonProperty(Order = 22)] public PooledDictionary<string, double> HttpAdaptation { get; set; }

        [JsonProperty(Order = 23)] public PooledDictionary<string, double> ExhaustiveAdaptation { get; set; }

        [JsonProperty(Order = 24)] public PooledDictionary<string, EntityModelActivationRulePayload> Activation { get; set; }

        [JsonProperty(Order = 25)] public CreateCase CreateCasePayload { get; set; }

        [JsonProperty(Order = 26)] public PooledDictionary<string, double> Tag { get; set; }

        [JsonProperty(Order = 27)] public Performance.InvokeThreadPerformance InvokeThreadPerformance { get; set; }

        [JsonIgnore] public List<ArchiveKey> ReportDatabaseValue { get; set; }

        [JsonIgnore] public bool StoreInRdbms { get; set; }

        [JsonIgnore] public int EntityAnalysisModelId { get; set; }

        [JsonIgnore] public int TenantRegistryId { get; set; }
    }
}
