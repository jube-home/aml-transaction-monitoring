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

    public class VisualisationRegistryDto
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public bool Locked { get; set; }
        public DateTimeOffset? CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public string Name { get; set; }
        public bool ShowInDirectory { get; set; }
        public int Columns { get; set; }
        public int ColumnWidth { get; set; }
        public int RowHeight { get; set; }
        public int Version { get; set; }
        public Guid Guid { get; set; }
    }
}
