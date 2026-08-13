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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Models
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using EntityAnalysisModelDictionary=Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelDictionary;
    using EntityAnalysisModelInlineScript=Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript.EntityAnalysisModelInlineScript;
    using SanctionEntry=Sanctions.Models.SanctionEntry;

    public class EntityAnalysisModels
    {
        public Dictionary<int, EntityAnalysisModel> ActiveEntityAnalysisModels { get; set; }
        public List<EntityAnalysisModelInlineScript> EntityAnalysisModelInlineScripts { get; set; }
        public Guid EntityAnalysisInstanceGuid { get; set; }
        public ConcurrentDictionary<int, SanctionEntry> SanctionsEntries { get; set; } = new ConcurrentDictionary<int, SanctionEntry>();
        public ConcurrentDictionary<string, byte> SanctionsStopTokens { get; set; } = new ConcurrentDictionary<string, byte>();
        public Dictionary<string, List<string>> EntityAnalysisModelLists { get; } = new Dictionary<string, List<string>>();
        public Dictionary<int, EntityAnalysisModelDictionary> KvpDictionaries { get; } = new Dictionary<int, EntityAnalysisModelDictionary>();
        public Dictionary<string, List<string>> EntityAnalysisModelSuppressionModels { get; set; }
        public Dictionary<string, Dictionary<string, List<string>>> EntityAnalysisModelSuppressionRules { get; } = new Dictionary<string, Dictionary<string, List<string>>>();
    }
}
