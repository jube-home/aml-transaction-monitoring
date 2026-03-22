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

namespace Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Tokenisation;

    public static class EntityAnalysisModelInstanceEntryPayloadExtensions
    {
        public static string ReplaceTokens(this EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayload, string existing)
        {
            var lookup = new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Payload"] = key => entityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(key, out var v) ? v.ToString() : null,
                ["Abstraction"] = key => entityAnalysisModelInstanceEntryPayload.Abstraction.TryGetValue(key, out var v) ? v.ToString(CultureInfo.InvariantCulture) : null,
                ["Dictionary"] = key => entityAnalysisModelInstanceEntryPayload.Dictionary.TryGetValue(key, out var v) ? v.ToString(CultureInfo.InvariantCulture) : null,
                ["TtlCounter"] = key => entityAnalysisModelInstanceEntryPayload.TtlCounter.TryGetValue(key, out var v) ? v.ToString(CultureInfo.InvariantCulture) : null,
                ["Sanction"] = key => entityAnalysisModelInstanceEntryPayload.Sanction.TryGetValue(key, out var v) ? v.ToString(CultureInfo.InvariantCulture) : null,
                ["HttpAdaptation"] = key => entityAnalysisModelInstanceEntryPayload.HttpAdaptation.TryGetValue(key, out var v) ? v.ToString(CultureInfo.InvariantCulture) : null,
                ["ExhaustiveAdaptation"] = key => entityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation.TryGetValue(key, out var v) ? v.ToString(CultureInfo.InvariantCulture) : null,
                ["Activation"] = key => entityAnalysisModelInstanceEntryPayload.Activation.ContainsKey(key) ? "true" : "false"
            };

            var result = existing;

            foreach (var token in Tokenisation.ReturnTokens(existing))
            {
                var splits = token.Split('.', 2);
                if (splits.Length < 2)
                {
                    continue;
                }

                if (!lookup.TryGetValue(splits[0], out var resolver))
                {
                    continue;
                }

                var value = resolver(splits[1]);
                if (value is null)
                {
                    continue;
                }

                result = result.Replace($"[@{token}@]", value);
            }

            return result;
        }
    }
}
