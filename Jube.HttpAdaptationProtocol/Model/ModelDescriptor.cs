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

namespace Jube.HttpAdaptationProtocol.Model
{
    using System;

    public sealed record ModelDescriptor
    {
        public string Name { get; init; }
        public string Family { get; init; }
        public string Version { get; init; }
        public string ArtifactHash { get; init; }
        public DateTime? TrainedDate { get; init; }
        public int? FeatureCount { get; init; }
        public ValidationDescriptor Validation { get; init; }
        public int? BootstrapReplicates { get; init; }
        public string LabelsVersion { get; init; }
        public string LabelsHash { get; init; }
        public string TopologyVersion { get; init; }
        public string TopologyHash { get; init; }
        public DateTime? TopologyDate { get; init; }
        public string WeightsVersion { get; init; }
        public string WeightsHash { get; init; }
        public DateTime? WeightsDate { get; init; }
        public string StructureLearning { get; init; }
        public int? WhitelistedArcs { get; init; }
        public int? BlacklistedArcs { get; init; }
        public int? HiddenLayers { get; init; }
        public int? ProcessingElements { get; init; }
    }
}
