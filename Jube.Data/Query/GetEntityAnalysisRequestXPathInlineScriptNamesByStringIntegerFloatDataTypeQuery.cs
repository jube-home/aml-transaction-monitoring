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

namespace Jube.Data.Query
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Repository;
    using SyntaxTree;

    public class GetEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public async Task<IEnumerable<Dto>> ExecuteAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            var searchKeys = new List<Dto>();

            var entityAnalysisModelRequestXpath = new EntityAnalysisModelRequestXPathRepository(dbContext, tenantRegistryId);
            var entityAnalysisModelInlineScriptRepository = new EntityAnalysisModelInlineScriptRepository(dbContext, tenantRegistryId);
            var entityAnalysisModelInlineScripts = await entityAnalysisModelInlineScriptRepository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);
            var entityAnalysisInlineScriptRepository = new EntityAnalysisInlineScriptRepository(dbContext);

            var entityAnalysisModelRequestXpaths = await entityAnalysisModelRequestXpath
                .GetByEntityAnalysisModelIdByDataTypeAsync(entityAnalysisModelId, token, 1, 3);

            var entityAnalysisModelRequestXpathsStaging = entityAnalysisModelRequestXpaths
                .Select(s => new Dto
                {
                    Name = s.Name,
                    DataTypeId = s.DataTypeId ?? 1,
                    SearchKey = s.SearchKey == 1
                });

            searchKeys.AddRange(searchKeys.Union(entityAnalysisModelRequestXpathsStaging));

            foreach (var entityAnalysisModelInlineScript in entityAnalysisModelInlineScripts)
            {
                if (!entityAnalysisModelInlineScript.EntityAnalysisInlineScriptId.HasValue)
                {
                    continue;
                }

                var entityAnalysisInlineScript = await entityAnalysisInlineScriptRepository
                    .GetByIdAsync(entityAnalysisModelInlineScript.EntityAnalysisInlineScriptId.Value, token);

                var inlineScriptKeys = SyntaxTreeHelpers
                    .GetPublicProperties(entityAnalysisInlineScript.Code, entityAnalysisInlineScript.LanguageId == 2)
                    .Where(w => w.Value.DataTypeId == 1 || w.Value.DataTypeId == 3)
                    .Select(s => new Dto
                    {
                        Name = s.Key,
                        DataTypeId = s.Value.DataTypeId,
                        SearchKey = s.Value.SearchKey
                    });

                searchKeys.AddRange(inlineScriptKeys);
            }

            var entityAnalysisInlineFunctionRepository = new EntityAnalysisModelInlineFunctionRepository(dbContext);
            var entityAnalysisModelInlineFunctions = (await entityAnalysisInlineFunctionRepository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token)).ToList();

            var entityAnalysisModelInlineFunctionsStaging = entityAnalysisModelInlineFunctions.Where(w => w.ReturnDataTypeId == 1)
                .Select(s => new Dto
                {
                    Name = s.Name,
                    DataTypeId = s.ReturnDataTypeId ?? 1
                });

            searchKeys.AddRange(entityAnalysisModelInlineFunctionsStaging);
            searchKeys = searchKeys.OrderBy(o => o.Name).ToList();

            return searchKeys;
        }

        public class Dto
        {
            public string Name { get; set; }
            public int DataTypeId { get; set; }
            public bool SearchKey { get; set; }
        }
    }
}
