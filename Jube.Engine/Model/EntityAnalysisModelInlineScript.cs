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

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Jube.Engine.Model
{
    public class EntityAnalysisModelInlineScript
    {
        public int Id { get; set; }
        public Assembly InlineScriptCompile { get; set; }
        public string InlineScriptCode { get; set; }
        public string Dependencies { get; set; }
        public int InlineScriptId { get; set; }
        public string ClassName { get; set; }
        public string Name { get; set; }
        public Type InlineScriptType { get; set; }
        public string MethodName { get; set; }
        public MethodInfo PreProcessingMethodInfo { get; set; }
        public Dictionary<string, int> PromoteReportTableColumns { get; } = [];
        public List<DistinctSearchKey> GroupingKeys { get; } = [];
        public object ActivatedObject { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}