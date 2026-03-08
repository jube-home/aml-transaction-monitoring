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
    using System;

    public class Counters
    {
        public int ActivationWatcherCount;
        public double ActivationWatcherSample;
        public char MaxActivationWatcherInterval;
        public double MaxActivationWatcherThreshold;
        public int MaxActivationWatcherValue;
        public double MaxResponseElevation;
        public char MaxResponseElevationInterval;
        public int MaxResponseElevationThreshold;
        public int MaxResponseElevationValue;
        public int ModelInvokeCounter;
        public int ModelInvokeGatewayCounter;
        public int ModelResponseElevationCounter;
        public long ModelTotalResponseTime;

        public int ResponseElevationCount;
        public int ResponseElevationFrequencyLimitCounter;
        public int ResponseElevationValueLimitCounter;
        public DateTime LastCountersChecked { get; set; }
        public DateTime LastCountersWritten { get; set; }
        public DateTime LastModelInvokeCountersWritten { get; set; }
    }
}
