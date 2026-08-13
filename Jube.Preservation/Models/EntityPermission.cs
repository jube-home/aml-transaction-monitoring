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
    public class EntityPermission
    {
        [Key(0)]
        public IEnumerable<CaseWorkflowRole>? CaseWorkflowRole { get; set; }
        [Key(1)]
        public IEnumerable<CaseWorkflowActionRole>? CaseWorkflowActionRole { get; set; }
        [Key(2)]
        public IEnumerable<CaseWorkflowDisplayRole>? CaseWorkflowDisplayRole { get; set; }
        [Key(3)]
        public IEnumerable<CaseWorkflowFilterRole>? CaseWorkflowFilterRole { get; set; }
        [Key(4)]
        public IEnumerable<CaseWorkflowFormRole>? CaseWorkflowFormRole { get; set; }
        [Key(5)]
        public IEnumerable<CaseWorkflowMacroRole>? CaseWorkflowMacroRole { get; set; }
        [Key(6)]
        public IEnumerable<CaseWorkflowStatusRole>? CaseWorkflowStatusRole { get; set; }
        [Key(7)]
        public IEnumerable<CaseWorkflowXPathRole>? CaseWorkflowXPathRole { get; set; }
        [Key(8)]
        public IEnumerable<VisualisationRegistryRole>? VisualisationRegistryRole { get; set; }
        [Key(9)]
        public IEnumerable<VisualisationRegistryDatasourceRole>? VisualisationRegistryDatasourceRole { get; set; }
        [Key(10)]
        public IEnumerable<VisualisationRegistryParameterRole>? VisualisationRegistryParameterRole { get; set; }
        [Key(11)]
        public IEnumerable<EntityAnalysisModelRole>? EntityAnalysisModelRole { get; set; }
    }
}
