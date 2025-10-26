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

namespace Jube.Engine.Model
{
    public class EntityAnalysisModelTtlCounter
    {
        public int Id { get; init; }
        public string TtlCounterInterval { get; set; }
        public int TtlCounterValue { get; set; }
        public bool ResponsePayload { get; set; }
        public string Name { get; set; }
        public bool ReportTable { get; set; }
        public string TtlCounterDataName { get; set; }
        public bool OnlineAggregation { get; set; }
        public bool EnableLiveForever { get; set; }
        public Guid Guid { get; set; }
    }
}