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

    public class EntityModelGatewayRule
    {
        public delegate bool Match(DictionaryNoBoxing data, Dictionary<string, List<string>> list,
            PooledDictionary<string, double> kvp, ILog log);

        public int EntityAnalysisModelGatewayRuleId { get; init; }
        public int RuleScriptTypeId { get; set; }
        public string GatewayRuleScript { get; set; }
        public string Name { get; set; }
        public double GatewaySample { get; set; }
        public Assembly GatewayRuleCompile { get; set; }
        public Match GatewayRuleCompileDelegate { get; set; }
        public double MaxResponseElevation { get; set; }
        public int Counter { get; set; }
    }
}
