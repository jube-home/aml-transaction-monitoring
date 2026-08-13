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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using EntityAnalysisModelInvoke.Context;
    
    // ReSharper disable once ClassNeverInstantiated.Global
    public class EntityAnalysisModelInlineScript
    {
        public int Id { get; set; }
        public Assembly InlineScriptCompile { get; set; }
        public string InlineScriptCode { get; set; }
        public string Dependencies { get; set; }
        public string ClassName { get; set; }
        public string Name { get; set; }
        public Type InlineScriptType { get; set; }
        public MethodInfo PreProcessingMethodInfo { get; set; }
        public Dictionary<string, EntityAnalysisModelInlineScriptPropertyAttribute.EntityAnalysisModelInlineScriptPropertyAttribute> EntityAnalysisModelInlineScriptPropertyAttributes { get; } = [];
        public List<EntityAnalysisModelInlineScriptEvent> EntityAnalysisModelInlineScriptEvents { get; set; } = [];
        // ReSharper disable once CollectionNeverUpdated.Global
        public List<DistinctSearchKey> GroupingKeys { get; } = [];
        public DateTime? CreatedDate { get; set; }
        public byte LanguageId { get; set; }
        public Func<object> ActivatorDelegate { get; set; }
        public Func<object, Context, Task<bool>> ExecuteAsyncDelegate { get; set; }
    }
}
