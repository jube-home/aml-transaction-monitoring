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

using Accord.Statistics.Distributions.Univariate;

namespace Jube.Engine.Exhaustive.Variables
{
    public class Variable
    {
        public int ExhaustiveSearchInstanceVariableId { get; set; }
        public byte ProcessingTypeId { get; set; }
        public string ValueJsonPath { get; set; }
        public string Name { get; set; }
        public double Mean { get; set; }
        public double Mode { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }
        public double Kurtosis { get; set; }
        public double Skewness { get; set; }
        public double Iqr { get; set; }
        public double Sd { get; set; }
        public int DistinctCount { get; set; }
        public byte NormalisationType { get; set; }
        public int Bins { get; set; }
        public double Correlation { get; set; }
        public TriangularDistribution TriangularDistribution { get; set; }
    }
}