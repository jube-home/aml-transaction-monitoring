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

namespace Jube.Engine.Model
{
    using System.Collections.Generic;
    using System.Reflection;
    using Dictionary;
    using log4net;

    public class EntityAnalysisModelAbstractionRule
    {
        public delegate bool Match(DictionaryNoBoxing data, Dictionary<string, int> ttlCounter,
            Dictionary<string, List<string>> list, PooledDictionary<string, double> kvp, ILog log);

        public int Id { get; init; }
        public string AbstractionRuleScript { get; set; }
        public byte RuleScriptTypeId { get; set; }
        public string Name { get; set; }
        public string SearchKey { get; set; }
        public string SearchFunctionKey { get; set; }
        public int AbstractionRuleAggregationFunctionType { get; set; }
        public string AbstractionHistoryIntervalType { get; set; }
        public int AbstractionHistoryIntervalValue { get; set; }
        public bool Search { get; set; }
        public Assembly AbstractionRuleCompile { get; set; }
        public Match AbstractionRuleCompileDelegate { get; set; }
        public bool ResponsePayload { get; set; }
        public string LogicHash { get; set; }
        public bool ReportTable { get; set; }
        public bool EnableOffset { get; set; }
        public int OffsetType { get; set; }
        public int OffsetValue { get; set; }
        public string AbstractionRuleAggregationFunctionIntervalType { get; set; }
    }
}
