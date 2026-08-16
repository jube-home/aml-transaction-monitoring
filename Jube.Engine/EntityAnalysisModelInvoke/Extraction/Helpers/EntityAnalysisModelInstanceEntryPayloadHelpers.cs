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

namespace Jube.Engine.EntityAnalysisModelInvoke.Extraction.Helpers
{
    using System;
    using Dictionary;
    using HttpAdaptationProtocol;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
    using EntityAnalysisModel=EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

    public static class EntityAnalysisModelInstanceEntryPayloadHelpers
    {
        public static EntityAnalysisModelInstanceEntryPayload Create(EntityAnalysisModel model, Guid entityAnalysisModelInstanceEntryGuid = default)
        {
            return new EntityAnalysisModelInstanceEntryPayload
            {
                EntityAnalysisModelName = model.Instance.Name,
                EnableRdbmsArchive = model.Flags.EnableRdbmsArchive,
                TenantRegistryId = model.Instance.TenantRegistryId,
                EntityAnalysisModelId = model.Instance.Id,
                EntityAnalysisModelInstanceEntryGuid = entityAnalysisModelInstanceEntryGuid == Guid.Empty ? Guid.NewGuid() : entityAnalysisModelInstanceEntryGuid,
                EntityAnalysisModelGuid = model.Instance.Guid,
                Abstraction = new PooledDictionary<string, double>(model.Collections.ModelAbstractionRules.Count),
                Activation = new PooledDictionary<string, EntityModelActivationRulePayload>(),
                Tag = [],
                Dictionary = new PooledDictionary<string, double>(model.Dependencies.KvpDictionaries.Count),
                TtlCounter = new PooledDictionary<string, double>(model.Collections.ModelTtlCounters.Count),
                Sanction = new PooledDictionary<string, double>(model.Collections.EntityAnalysisModelSanctions.Count),
                AbstractionCalculation =
                    new PooledDictionary<string, double>(model.Collections.EntityAnalysisModelAbstractionCalculations.Count),
                HttpAdaptation = new PooledDictionary<string, Adaptation>(model.Collections.EntityAnalysisModelAdaptations.Count),
                ExhaustiveAdaptation = new PooledDictionary<string, double>(model.Collections.ExhaustiveModels.Count),
                InvokeTaskPerformance = new InvokeTaskPerformance
                {
                    ComputeTimes = new InvokeTasksPerformance()
                },
                CreatedDate = DateTime.UtcNow,
                ArchiveKeys = []
            };
        }
    }
}
