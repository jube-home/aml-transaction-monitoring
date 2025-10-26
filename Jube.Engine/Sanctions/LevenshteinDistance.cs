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
    using System.Linq;
    using System.Threading.Tasks;
    using Fastenshtein;

    public static class LevenshteinDistance
    {
        public static List<SanctionEntryReturn> CheckMultipartString(string multiPartString, int distance,
            Dictionary<int, SanctionEntryDto> sanctionsEntries)
        {
            var sanctionsEntriesReturn = new ConcurrentDictionary<int, SanctionEntryReturn>();

            if (String.IsNullOrWhiteSpace(multiPartString) || sanctionsEntries.Count == 0)
            {
                return [];
            }

            // Pre-clean and split the input string to unique parts
            var multiPartStrings = multiPartString
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(Clean)
                .Distinct()
                .ToArray();

            if (multiPartStrings.Length == 0)
            {
                return [];
            }

            Parallel.ForEach(sanctionsEntries.Values, entry =>
            {
                var sanctionValues = entry.SanctionElementValue
                    .Select(Clean)
                    .Distinct()
                    .ToArray();

                for (var dist = 0; dist <= distance; dist++)
                {
                    // All input words must have at least one close enough match in the entry values
                    var allWordsMatch = multiPartStrings.All(inputPart =>
                        sanctionValues.Any(sValue =>
                            Levenshtein.Distance(inputPart, sValue) <= dist));

                    if (!allWordsMatch)
                    {
                        continue;
                    }
                    
                    sanctionsEntriesReturn.TryAdd(entry.SanctionEntryId,
                        new SanctionEntryReturn
                        {
                            SanctionEntryDto = entry,
                            LevenshteinDistance = dist
                        });
                    break;
                }
            });

            return sanctionsEntriesReturn.Values.ToList();
        }

        public static string Clean(string raw)
        {
            var value = raw;
            value = value.Replace(",", "");
            value = value.Replace(" ", "");
            value = value.ToLower();
            return value;
        }
    }
}
