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

namespace Jube.Preservation.Models
{
    using Data.Poco;
    using MessagePack;

    [MessagePackObject]
    public class Payload
    {
        [Key(0)] public IEnumerable<EntityAnalysisModel>? EntityAnalysisModel { get; set; }
        [Key(1)] public IEnumerable<VisualisationRegistry>? VisualisationRegistry { get; set; }
        [Key(2)] public EntityPermission? EntityPermission { get; set; }
        [Key(3)] public IEnumerable<RoleRegistry>? RoleRegistry { get; set; }
    }
}
