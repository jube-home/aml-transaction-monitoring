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

namespace Jube.Engine.Sanctions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Models;

    public static class SanctionAggregationCalculator
    {
        private const byte Sum = 1;
        private const byte Average = 2;
        private const byte Count = 3;
        private const byte Max = 4;
        private const byte Min = 5;
        private const byte First = 6;
        private const byte Last = 7;
        private const byte Confidence = 8;

        private const double DefaultClosenessWeight = 0.5;
        private const ConfidenceBlendMode DefaultBlendMode = ConfidenceBlendMode.Multiplicative;

        public static double? Calculate(byte aggregationTypeId, IReadOnlyList<SanctionEntryReturn> sanctionEntryReturns)
        {
            return aggregationTypeId switch
            {
                Sum => CalculateSum(sanctionEntryReturns),
                Average => CalculateAverage(sanctionEntryReturns),
                Count => CalculateCount(sanctionEntryReturns),
                Max => CalculateMax(sanctionEntryReturns),
                Min => CalculateMin(sanctionEntryReturns),
                First => CalculateFirst(sanctionEntryReturns),
                Last => CalculateLast(sanctionEntryReturns),
                Confidence => CalculateConfidence(sanctionEntryReturns),
                _ => CalculateAverage(sanctionEntryReturns)
            };
        }

        public static double? CalculateSum(IReadOnlyCollection<SanctionEntryReturn> sanctionEntryReturns)
        {
            return sanctionEntryReturns is { Count: > 0 } ? sanctionEntryReturns.Sum(s => s.LevenshteinDistance) : null;
        }

        public static double? CalculateCount(IReadOnlyCollection<SanctionEntryReturn> sanctionEntryReturns)
        {
            return sanctionEntryReturns is { Count: > 0 } ? sanctionEntryReturns.Count : null;
        }

        public static double? CalculateMax(IReadOnlyCollection<SanctionEntryReturn> sanctionEntryReturns)
        {
            return sanctionEntryReturns is { Count: > 0 } ? sanctionEntryReturns.Max(s => s.LevenshteinDistance) : null;
        }

        public static double? CalculateMin(IReadOnlyCollection<SanctionEntryReturn> sanctionEntryReturns)
        {
            return sanctionEntryReturns is { Count: > 0 } ? sanctionEntryReturns.Min(s => s.LevenshteinDistance) : null;
        }

        public static double? CalculateFirst(IReadOnlyList<SanctionEntryReturn> sanctionEntryReturns)
        {
            return sanctionEntryReturns is { Count: > 0 } ? sanctionEntryReturns[0].LevenshteinDistance : null;
        }

        public static double? CalculateLast(IReadOnlyList<SanctionEntryReturn> sanctionEntryReturns)
        {
            return sanctionEntryReturns is { Count: > 0 } ? sanctionEntryReturns[^1].LevenshteinDistance : null;
        }

        public static double? CalculateAverage(IReadOnlyCollection<SanctionEntryReturn> sanctionEntryReturns)
        {
            if (sanctionEntryReturns is not { Count: > 0 })
            {
                return null;
            }

            var sum = sanctionEntryReturns.Sum(s => s.LevenshteinDistance);

            return sum == 0 ? 0 : (double)sum / sanctionEntryReturns.Count;
        }

        public static double? CalculateConfidence(IReadOnlyCollection<SanctionEntryReturn> sanctionEntryReturns)
        {
            const int skewReliabilityFloor = 3;
            const int skewReliabilityCeiling = 12;
            const double separationMidpointZ = 1.5;

            if (sanctionEntryReturns is not { Count: > 0 })
            {
                return null;
            }

            var n = sanctionEntryReturns.Count;
            var distances = sanctionEntryReturns.Select(s => (double)s.LevenshteinDistance).ToList();

            var min = distances.Min();
            var closeness = CalculateCloseness(min);

            if (n == 1)
            {
                return Math.Round(closeness, 4);
            }

            var mean = distances.Average();
            var stdDev = CalculateSampleStdDev(distances, mean, n);

            if (stdDev == 0)
            {
                return Math.Round(closeness, 4);
            }

            var separation = CalculateSeparation(mean, min, stdDev, separationMidpointZ);
            var skewWeight = CalculateSkewWeight(distances, mean, n, skewReliabilityFloor, skewReliabilityCeiling);

            var standout = separation * skewWeight;

            return Math.Round(CombineConfidence(closeness, standout, DefaultBlendMode, DefaultClosenessWeight), 4);
        }

        private static double CalculateCloseness(double minDistance)
        {
            return 1.0 / (1.0 + minDistance);
        }

        private static double CalculateSampleStdDev(List<double> distances, double mean, int n)
        {
            var variance = distances.Sum(d => (d - mean) * (d - mean)) / (n - 1);
            return Math.Sqrt(variance);
        }

        private static double CalculateSeparation(double mean, double min, double stdDev, double midpointZ)
        {
            var zScore = (mean - min) / stdDev;
            return 1.0 / (1.0 + Math.Exp(-zScore + midpointZ));
        }

        private static double CalculateSkewWeight(
            List<double> distances, double mean, int n, int reliabilityFloor, int reliabilityCeiling)
        {
            var m2 = distances.Sum(d => (d - mean) * (d - mean)) / n;

            if (m2 == 0)
            {
                return 1.0;
            }

            var m3 = distances.Sum(d => (d - mean) * (d - mean) * (d - mean)) / n;
            var g1 = m3 / Math.Pow(m2, 1.5);
            var adjustedSkewness = Math.Sqrt(n * (n - 1.0)) / (n - 2.0) * g1;

            var rawWeight = 0.5 + 0.5 * Math.Clamp((-adjustedSkewness + 1) / 3, 0, 1);

            var reliability = Math.Clamp(
                (n - reliabilityFloor) / (double)(reliabilityCeiling - reliabilityFloor), 0, 1);

            return 1.0 - reliability * (1.0 - rawWeight);
        }

        private static double CombineConfidence(
            double closeness, double standout, ConfidenceBlendMode blendMode, double closenessWeight)
        {
            return blendMode switch
            {
                ConfidenceBlendMode.WeightedAverage =>
                    closenessWeight * closeness + (1.0 - closenessWeight) * standout,
                _ => closeness * standout
            };
        }

        private enum ConfidenceBlendMode
        {
            Multiplicative,
            WeightedAverage
        }
    }
}
