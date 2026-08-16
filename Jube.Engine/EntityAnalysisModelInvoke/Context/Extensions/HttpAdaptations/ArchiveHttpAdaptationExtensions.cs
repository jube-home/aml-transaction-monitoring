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
    using Data.Poco;
    using EntityAnalysisModelHttpAdaptation=EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelHttpAdaptation;

    public static class ArchiveHttpAdaptationExtensions
    {
        public static void ArchiveHttpAdaptation(this Context context, EntityAnalysisModelHttpAdaptation modelAdaptation)
        {
            if (!modelAdaptation.ReportTable || context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
            {
                return;
            }

            var adaptation = context.EntityAnalysisModelInstanceEntryPayload.HttpAdaptation[modelAdaptation.Name];

            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
            {
                ProcessingTypeId = 9,
                Key = modelAdaptation.Name,
                KeyValueFloat = adaptation.Value,
                EntityAnalysisModelInstanceEntryGuid = context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
            });

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating {modelAdaptation.Id} has archived the HTTP Adaptation response for {modelAdaptation.Name} with a Value of {adaptation.Value?.ToString(CultureInfo.InvariantCulture) ?? "null"} to the SQL report payload.");
            }
        }
    }
}
