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

namespace Jube.HttpAdaptationProtocol.Parsing
{
    using System;
    using System.Globalization;
    using log4net;
    using Newtonsoft.Json;

    public static class AdaptationResponseParser
    {
        private static readonly JsonSerializerSettings ResponseSerializerSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public static Adaptation ParseAdaptationResponse(this string rawBody, ILog log)
        {
            var trimmed = rawBody?.Trim();

            if (String.IsNullOrEmpty(trimmed))
            {
                return new Adaptation
                {
                    Error = "The HTTP Adaptation endpoint returned an empty response body."
                };
            }

            if (TryParseBareNumber(trimmed, out var bareValue))
            {
                return new Adaptation
                {
                    Value = bareValue
                };
            }

            try
            {
                var adaptation = JsonConvert.DeserializeObject<Adaptation>(trimmed, ResponseSerializerSettings)
                                 ?? new Adaptation();

                if (!String.IsNullOrEmpty(adaptation.Error) && adaptation.Value is not null)
                {
                    adaptation = adaptation with
                    {
                        Value = null
                    };
                }

                return adaptation;
            }
            catch (JsonException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"Http Adaptation: response body could not be parsed as either a bare number or a v1.1 Adaptation object ({ex.Message}). Body: {rawBody}");
                }

                return new Adaptation
                {
                    Error = "The HTTP Adaptation endpoint returned a response body that could not be parsed as a bare number or a protocol Adaptation object."
                };
            }
        }

        private static bool TryParseBareNumber(string trimmed, out double value)
        {
            var candidate = trimmed;

            if (candidate.Length >= 2 && candidate[0] == '[' && candidate[^1] == ']')
            {
                candidate = candidate[1..^1].Trim();
            }

            return Double.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
