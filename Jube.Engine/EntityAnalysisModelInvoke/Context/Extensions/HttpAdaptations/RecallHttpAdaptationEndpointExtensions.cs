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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.HttpAdaptations
{
    using System.Globalization;
    using System.Threading.Tasks;
    using HttpAdaptationProtocol;
    using HttpAdaptationProtocol.Parsing;
    using Newtonsoft.Json;
    using EntityAnalysisModelHttpAdaptation=EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelHttpAdaptation;

    public static class RecallHttpAdaptationEndpointExtensions
    {
        public static async Task<Adaptation> RecallHttpEndpointAsync(this Context context,
            EntityAnalysisModelHttpAdaptation modelAdaptation, JsonSerializerSettings jsonSerializerSettings)
        {
            var rawBody = await modelAdaptation.PostAsync(context.EntityAnalysisModelInstanceEntryPayload, jsonSerializerSettings, context.Log).ConfigureAwait(false);

            var adaptation = rawBody.ParseAdaptationResponse(context.Log);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating {modelAdaptation.Id} has called the HTTP Adaptation endpoint with a Value of {adaptation.Value?.ToString(CultureInfo.InvariantCulture) ?? "null"} and Error of {adaptation.Error ?? "none"}.");
            }

            return adaptation;
        }
    }
}
