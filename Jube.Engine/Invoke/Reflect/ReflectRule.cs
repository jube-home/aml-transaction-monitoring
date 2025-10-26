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

namespace Jube.Engine.Invoke.Reflect
{
    using System.Collections.Generic;
    using Dictionary;
    using log4net;
    using Model;
    using Model.Processing.Payload;

    public static class ReflectRule
    {
        public static bool Execute(EntityAnalysisModelAbstractionRule abstractionRule,
            EntityAnalysisModel entityAnalysisModel, DictionaryNoBoxing fields,
            Dictionary<string, int> ttlCounters, PooledDictionary<string, double> entityInstanceEntryDictionaryKvPs, ILog log)
        {
            var matched = abstractionRule.AbstractionRuleCompileDelegate(fields, ttlCounters,
                entityAnalysisModel.EntityAnalysisModelLists, entityInstanceEntryDictionaryKvPs, log);
            return matched;
        }

        public static bool Execute(EntityAnalysisModelActivationRule activationRule,
            EntityAnalysisModel entityAnalysisModel,
            EntityAnalysisModelInstanceEntryPayload payload, PooledDictionary<string, double> entityInstanceEntryDictionaryKvPs,
            ILog log)
        {
            var matched = activationRule.ActivationRuleCompileDelegate(payload.Payload,
                payload.TtlCounter, payload.Abstraction,
                payload.HttpAdaptation, payload.ExhaustiveAdaptation, entityAnalysisModel.EntityAnalysisModelLists,
                payload.AbstractionCalculation,
                payload.Sanction, entityInstanceEntryDictionaryKvPs, log);
            return matched;
        }

        public static double Execute(EntityAnalysisModelAbstractionCalculation functionRule,
            EntityAnalysisModel entityAnalysisModel,
            EntityAnalysisModelInstanceEntryPayload payload, PooledDictionary<string, double> entityInstanceEntryDictionaryKvPs,
            ILog log)
        {
            var matched = functionRule.FunctionCalculationCompileDelegate(payload.Payload,
                payload.TtlCounter, payload.Abstraction,
                entityAnalysisModel.EntityAnalysisModelLists,
                entityInstanceEntryDictionaryKvPs, log);

            return matched;
        }

        public static object Execute(EntityAnalysisModelInlineFunction entityAnalysisModelInlineFunction,
            EntityAnalysisModel entityAnalysisModel,
            EntityAnalysisModelInstanceEntryPayload payload,
            PooledDictionary<string, double> entityInstanceEntryDictionaryKvPs, ILog log)
        {
            var matched = entityAnalysisModelInlineFunction.FunctionCalculationCompileDelegate(payload.Payload,
                entityAnalysisModel.EntityAnalysisModelLists, entityInstanceEntryDictionaryKvPs, log);

            return matched;
        }
    }
}
