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

namespace Jube.Data.Query.CaseQuery.Dto
{
    public class CaseQueryDto
    {
        public int Id { get; set; }
        public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
        public DateTime DiaryDate { get; set; }
        public Guid CaseWorkflowGuid { get; set; }
        public Guid CaseWorkflowStatusGuid { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool Locked { get; set; }
        public string LockedUser { get; set; }
        public DateTime LockedDate { get; set; }
        public int ClosedStatusId { get; set; }
        public DateTime ClosedDate { get; set; }
        public string ClosedUser { get; set; }
        public string CaseKey { get; set; }
        public bool Diary { get; set; }
        public string DiaryUser { get; set; }
        public int Rating { get; set; }
        public string CaseKeyValue { get; set; }
        public int LastClosedStatus { get; set; }
        public DateTime ClosedStatusMigrationDate { get; set; }
        public string ForeColor { get; set; }
        public string BackColor { get; set; }
        public string Json { get; set; }
        // ReSharper disable once CollectionNeverQueried.Global
        public List<GetCaseByIdFieldEntryDto> FormattedPayload { get; set; }
        // ReSharper disable once CollectionNeverQueried.Global
        public List<GetCaseByIdActivationDto> Activation { get; set; }
        public bool EnableVisualisation { get; set; }
        public Guid VisualisationRegistryGuid { get; set; }
    }
}