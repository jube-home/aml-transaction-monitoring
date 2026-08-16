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

namespace Jube.HttpAdaptationProtocol
{
    using System;
    using Calibration;
    using Contribution;
    using Journey;
    using Model;
    using Newtonsoft.Json;

    public sealed record Adaptation
    {
        public double? Value { get; init; }
        public string Error { get; init; }
        public string Narrative { get; init; }
        public string HumanLabel { get; init; }
        public string ProtocolVersion { get; init; }
        public ModelDescriptor Model { get; init; }
        public ResultDescriptor Result { get; init; }
        public CalibrationDescriptor Calibration { get; init; }
        public ContributionSet Contribution { get; init; }
        public JourneyDescriptor Journey { get; init; }

        [JsonIgnore]
        public bool IsSuppressed
        {
            get
            {
                return Value is null || !String.IsNullOrEmpty(Error);
            }
        }

        public static implicit operator double?(Adaptation adaptation)
        {
            return adaptation?.Value;
        }
    }
}
