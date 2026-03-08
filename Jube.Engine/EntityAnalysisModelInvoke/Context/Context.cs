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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using DynamicEnvironment;
    using log4net;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using TaskCancellation.TaskHelper;
    using EntityAnalysisModel=EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

    public class Context
    {
        public readonly List<Task<TimedTaskResult>> PendingReadTasks = [];
        public readonly List<Task<TimedTaskResult>> PendingWriteTasks = [];
        public long? StartBytesUsed { get; init; }
        public EntityAnalysisModel EntityAnalysisModel { get; init; }
        public Dictionary<int, EntityAnalysisModel> AvailableEntityAnalysisModels { get; init; }
        public EntityAnalysisModelInstanceEntryPayload EntityAnalysisModelInstanceEntryPayload { get; init; }
        public Stopwatch Stopwatch { get; init; }
        public ILog Log { get; init; }
        public Random Random { get; init; }
        public DynamicEnvironment Environment { get; init; }
        public bool Async { get; set; }
    }
}
