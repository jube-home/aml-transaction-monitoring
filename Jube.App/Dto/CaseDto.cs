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

namespace Jube.App.Dto
{
    using System;

    public class CaseDto
    {
        public int Id { get; set; }
        public DateTimeOffset? DiaryDate { get; set; }
        public Guid CaseWorkflowStatusGuid { get; set; }
        public DateTimeOffset? CreatedDate { get; set; }
        public bool Locked { get; set; }
        public string LockedUser { get; set; }
        public DateTimeOffset? LockedDate { get; set; }
        public byte ClosedStatusId { get; set; }
        public DateTimeOffset? ClosedDate { get; set; }
        public string ClosedUser { get; set; }
        public string CaseKey { get; set; }
        public bool Diary { get; set; }
        public string DiaryUser { get; set; }
        public byte Rating { get; set; }
        public string Json { get; set; }
        public string CaseKeyValue { get; set; }
        public byte LastClosedStatus { get; set; }
        public string Payload { get; set; }
    }
}
