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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Fastenshtein;
    using Models;

    public sealed class LevenshteinDistance(double? maxDistanceRatio = null, double? maxCoverageRatio = null)
    {
        public List<SanctionEntryReturn> CheckMultipartString(
            string multiPartString,
            int maxDistance,
            ConcurrentDictionary<int, SanctionEntry> sanctionsEntries,
            ConcurrentDictionary<string, byte> stopTokens)
        {
            var sanctionsEntriesReturn = new ConcurrentDictionary<int, SanctionEntryReturn>();

            if (String.IsNullOrWhiteSpace(multiPartString) || sanctionsEntries.Count == 0)
            {
                return [];
            }

            var inputTokens = Tokenize(multiPartString, stopTokens);

            if (inputTokens.Length == 0)
            {
                return [];
            }

            Parallel.ForEach(sanctionsEntries.Values, entry =>
            {
                var entryTokens = entry.SanctionElementValue
                    .SelectMany(value => Tokenize(value, stopTokens))
                    .Distinct()
                    .ToArray();

                if (entryTokens.Length == 0)
                {
                    return;
                }

                var result = ScoreMatch(inputTokens, entryTokens, maxDistance);

                if (result.HasValue)
                {
                    sanctionsEntriesReturn.TryAdd(entry.SanctionEntryId,
                        new SanctionEntryReturn
                        {
                            SanctionEntry = entry,
                            LevenshteinDistance = result.Value
                        });
                }
            });

            return sanctionsEntriesReturn.Values.ToList();
        }

        private int? ScoreMatch(
            string[] inputTokens,
            string[] entryTokens,
            int maxDistance)
        {
            var coverageRatio = (double)inputTokens.Length / entryTokens.Length;

            if (coverageRatio < 0.5)
            {
                return null;
            }

            if (maxCoverageRatio.HasValue && coverageRatio > maxCoverageRatio.Value)
            {
                return null;
            }

            var pairsToMatch = Math.Min(inputTokens.Length, entryTokens.Length);

            var usedEntryIndices = new HashSet<int>();
            var worstDistance = 0;

            foreach (var inputToken in inputTokens.OrderByDescending(t => t.Length).Take(pairsToMatch))
            {
                var bestDistance = Int32.MaxValue;
                var bestIndex = -1;

                for (var i = 0; i < entryTokens.Length; i++)
                {
                    if (usedEntryIndices.Contains(i))
                    {
                        continue;
                    }

                    var distance = new Levenshtein(entryTokens[i]).DistanceFrom(inputToken);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = i;
                    }
                }

                if (bestIndex == -1)
                {
                    return null;
                }

                var tokenThreshold = EffectiveTokenThreshold(
                    inputToken.Length, entryTokens[bestIndex].Length, maxDistance);

                if (bestDistance > tokenThreshold)
                {
                    return null;
                }

                usedEntryIndices.Add(bestIndex);
                worstDistance = Math.Max(worstDistance, bestDistance);
            }

            return worstDistance;
        }

        private int EffectiveTokenThreshold(
            int inputTokenLength, int entryTokenLength, int maxDistance)
        {
            if (!maxDistanceRatio.HasValue)
            {
                return maxDistance;
            }

            var shorterLength = Math.Min(inputTokenLength, entryTokenLength);
            var ratioBound = Math.Max(1, (int)Math.Floor(shorterLength * maxDistanceRatio.Value));

            return Math.Min(maxDistance, ratioBound);
        }

        public static double? ParseNullableDistanceRatio(string rawValue)
        {
            return !String.IsNullOrWhiteSpace(rawValue)
                   && Double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? Math.Clamp(value, 0, 1)
                : null;
        }

        public static double? ParseNullableCoverageRatio(string rawValue)
        {
            return !String.IsNullOrWhiteSpace(rawValue)
                   && Double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? Math.Max(0, value)
                : null;
        }

        private static string[] Tokenize(string raw, ConcurrentDictionary<string, byte> stopTokens)
        {
            return raw
                .Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c)
                            != UnicodeCategory.NonSpacingMark)
                .Aggregate(new StringBuilder(), (sb, c) => sb.Append(c))
                .ToString()
                .ToLowerInvariant()
                .Replace("-", " ")
                .Replace(",", " ")
                .Replace(".", " ")
                .Replace("'", "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 1 && !stopTokens.ContainsKey(t))
                .Distinct()
                .ToArray();
        }
    }
}
