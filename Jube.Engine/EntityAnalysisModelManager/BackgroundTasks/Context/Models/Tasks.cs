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

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.Context.Models
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class Tasks
    {
        // ReSharper disable once CollectionNeverQueried.Global
        public readonly List<Task> ArchiverTasks = [];
        // ReSharper disable once CollectionNeverQueried.Global
        public readonly List<Task> PersistToActivationWatcherPollingTasks = [];
        // ReSharper disable once CollectionNeverQueried.Global
        public readonly List<Task> ReprocessingAsyncTasks = [];
        public Task ModelSyncTask { get; set; }
        public Task AbstractionRuleCachingTask { get; set; }
        public Task TtlCounterAdministrationTask { get; set; }
        public Task CachePruneAsyncTask { get; set; }
        public Task LruJournalPruneAsyncTask { get; set; }
        public Task HashCacheAssemblyObservabilityTask { get; set; }
    }
}
