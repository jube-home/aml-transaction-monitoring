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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models
{
    using System.Collections.Generic;
    using Exhaustive.Models;
    using Models;
    using Models.EntityAnalysisModelInlineScript;

    public class Collections
    {
        public List<string> Users = [];
        public List<EntityAnalysisModelAbstractionRule> ModelAbstractionRules { get; set; } = [];
        public List<EntityAnalysisModelTtlCounter> ModelTtlCounters { get; set; } = [];
        public List<EntityAnalysisModelSanction> EntityAnalysisModelSanctions { get; set; } = [];
        public List<EntityAnalysisModelActivationRule> ModelActivationRules { get; set; } = [];
        public List<EntityModelGatewayRule> ModelGatewayRules { get; set; } = [];
        public Dictionary<int, EntityAnalysisModelHttpAdaptation> EntityAnalysisModelAdaptations { get; set; } = new Dictionary<int, EntityAnalysisModelHttpAdaptation>();
        public List<ExhaustiveSearchInstance> ExhaustiveModels { get; set; } = [];
        public List<EntityAnalysisModelRequestXPath> EntityAnalysisModelRequestXPaths { get; set; } = [];
        public List<EntityAnalysisModelAbstractionCalculation> EntityAnalysisModelAbstractionCalculations { get; set; } = [];
        public List<EntityAnalysisModelInlineFunction> EntityAnalysisModelInlineFunctions { get; set; } = [];
        public List<EntityAnalysisModelInlineScript> EntityAnalysisModelInlineScripts { get; set; } = [];
        public List<EntityAnalysisModelTag> EntityAnalysisModelTags { get; set; } = [];
        public Dictionary<string, DistinctSearchKey> DistinctSearchKeys { get; set; } = new Dictionary<string, DistinctSearchKey>();
    }
}
