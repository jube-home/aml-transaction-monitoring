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

    public class CaseFileDto
    {
        public int Id { get; set; }
        public DateTimeOffset? CreatedDate { get; set; }
        public string CaseKey { get; set; }
        public string CreatedUser { get; set; }
        public string CaseKeyValue { get; set; }
        public int CaseId { get; set; }
        public DateTimeOffset? DeletedDate { get; set; }
        public string DeletedUser { get; set; }
        public bool Deleted { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public string ContentType { get; set; }
        public int Size { get; set; }
    }
}
