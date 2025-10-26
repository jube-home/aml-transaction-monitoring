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
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Dictionary;
    using log4net;

    public class EntityAnalysisModelActivationRule
    {
        public delegate bool Match(DictionaryNoBoxing data, PooledDictionary<string, int> ttlCounter,
            PooledDictionary<string, double> abstraction,
            PooledDictionary<string, double> httpAdaptation,
            PooledDictionary<string, double> exhaustiveAdaptation,
            Dictionary<string, List<string>> list,
            PooledDictionary<string, double> calculation,
            PooledDictionary<string, double> sanctions, PooledDictionary<string, double> kvp, ILog log);

        public int Id { get; init; }
        public bool Visible { get; set; }
        public int RuleScriptTypeId { get; set; }
        public string ActivationRuleScript { get; set; }
        public string Name { get; set; }
        public bool EnableCaseWorkflow { get; set; }
        public Guid CaseWorkflowGuid { get; set; }
        public Guid CaseWorkflowStatusGuid { get; set; }
        public Assembly ActivationRuleCompile { get; set; }
        public Match ActivationRuleCompileDelegate { get; set; }
        public bool ResponsePayload { get; set; }
        public bool EnableTtlCounter { get; set; }
        public Guid EntityAnalysisModelTtlCounterGuid { get; set; }
        public Guid EntityAnalysisModelGuidTtlCounter { get; set; }
        public double ResponseElevation { get; set; }
        public string ResponseElevationContent { get; set; }
        public string ResponseElevationRedirect { get; set; }
        public bool EnableReprocessing { get; set; }
        public bool SendToActivationWatcher { get; set; }
        public string ResponseElevationForeColor { get; set; }
        public string ResponseElevationBackColor { get; set; }
        public char BypassSuspendInterval { get; set; }
        public int BypassSuspendValue { get; set; }
        public double BypassSuspendSample { get; set; }
        public int Counter { get; set; }
        public double ActivationSample { get; set; }
        public bool ReportTable { get; set; }
        public bool EnableNotification { get; set; }
        public int NotificationTypeId { get; set; }
        public string NotificationDestination { get; set; }
        public string NotificationSubject { get; set; }
        public string NotificationBody { get; set; }
        public string CaseKey { get; set; }
        public bool EnableResponseElevation { get; set; }
        public string ResponseElevationKey { get; set; }
    }
}
