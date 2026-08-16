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

namespace Jube.Data.Repository
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Poco;

    public class EntityAnalysisModelSynchronisationErrorRepository(DbContext dbContext)
    {
        public enum EntityAnalysisModelSynchronisationErrorStepEnum
        {
            Models = 1,
            Lists = 2,
            Dictionaries = 3,
            RequestXPath = 4,
            InlineScripts = 5,
            InlineFunctions = 6,
            ParseIndexCache = 7,
            GatewayRules = 8,
            Sanctions = 9,
            AbstractionRules = 10,
            AbstractionCalculation = 11,
            TtlCounters = 12,
            HttpAdaptation = 13,
            ExhaustiveSearchInstances = 14,
            ActivationRules = 15,
            Tags = 16,
            Suppression = 17,
            ActivationRuleSuppression = 18,
            ApiUsers = 19
        }

        public Task InsertAsync(EntityAnalysisModelSynchronisationErrorStepEnum synchronisationStepId, string errorMessage, CancellationToken token = default)
        {
            return dbContext.InsertAsync(new EntityAnalysisModelSynchronisationError
            {
                SynchronisationStepId = (int)synchronisationStepId,
                ErrorMessage = errorMessage,
                CreatedDate = DateTime.UtcNow
            }, token: token);
        }
    }
}
