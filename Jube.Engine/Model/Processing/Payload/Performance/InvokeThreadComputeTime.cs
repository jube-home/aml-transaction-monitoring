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

namespace Jube.Engine.Model.Processing.Payload.Performance
{
    public class InvokeThreadComputeTime
    {
        public int Parse { get; set; }
        public int InlineFunction { get; set; }
        public int InlineScript { get; set; }
        public int Gateway { get; set; }
        public long SanctionsAsync { get; set; }
        public long DictionaryKvPsAsync { get; set; }
        public long TtlCountersAsync { get; set; }
        public long AbstractionRulesWithSearchKeysAsync { get; set; }
        public ReadTasks ReadTasks { get; set; }
        public int JoinReadTasks { get; set; }
        public int ExecuteAbstractionRulesWithoutSearchKey { get; set; }
        public int ExecuteAbstractionCalculation { get; set; }
        public int ExecuteExhaustiveAdaptation { get; set; }
        public int ExecuteHttpAdaptation { get; set; }
        public int ExecuteActivation { get; set; }
        public int JoinWriteTasks { get; set; }
        public WriteTasks WriteTasks { get; set; }
    }
}
