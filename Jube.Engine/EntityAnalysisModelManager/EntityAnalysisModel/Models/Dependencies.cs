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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using BackgroundTasks.TaskStarters.Archiver;
    using EntityAnalysisModel=EntityAnalysisModel;
    using EntityAnalysisModelDictionary=Models.EntityAnalysisModelDictionary;
    using SanctionEntry=Sanctions.Models.SanctionEntry;

    public class Dependencies
    {
        public Dictionary<string, List<string>> EntityAnalysisModelLists { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<int, EntityAnalysisModelDictionary> KvpDictionaries { get; set; } = new Dictionary<int, EntityAnalysisModelDictionary>();
        public Dictionary<string, List<string>> EntityAnalysisModelSuppressionModels { get; set; }
        public Dictionary<string, Dictionary<string, List<string>>> EntityAnalysisModelSuppressionRules { get; set; } = new Dictionary<string, Dictionary<string, List<string>>>();
        public ConcurrentDictionary<int, SanctionEntry> SanctionsEntries { get; set; } = new ConcurrentDictionary<int, SanctionEntry>();
        public ConcurrentDictionary<string, byte> SanctionsStopTokens { get; set; } = new ConcurrentDictionary<string, byte>();
        public Dictionary<int, EntityAnalysisModel> ActiveEntityAnalysisModels { get; set; } = new Dictionary<int, EntityAnalysisModel>();
        public Dictionary<string, DateTime> LastAbstractionRuleCache { get; } = new Dictionary<string, DateTime>();
        public Dictionary<int, ArchiveBuffer> BulkInsertMessageBuffers { get; } = new Dictionary<int, ArchiveBuffer>();
    }

}
