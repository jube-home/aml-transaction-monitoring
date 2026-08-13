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

namespace Jube.Engine.Exhaustive.Utilities.QueryBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Query;

    public class ParserJsonToSqlEntityAnalysisModel
    {
        private readonly List<object> tokens = new List<object>();
        private IEnumerable<GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery.Dto> completionDto;
        public string Sql;

        public static async Task<ParserJsonToSqlEntityAnalysisModel> CreateAsync(Rule rule, DbContext dbContext, int tenantRegistryId, int entityAnalysisModelId, CancellationToken token = default)
        {
            var parser = new ParserJsonToSqlEntityAnalysisModel();
            parser.completionDto = await parser.GetCompletionsAsync(dbContext, tenantRegistryId, entityAnalysisModelId, token).ConfigureAwait(false);
            parser.ExtractRule(rule);
            return parser;
        }

        private Task<IEnumerable<GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery.Dto>> GetCompletionsAsync(DbContext dbContext, int tenantRegistryId, int entityAnalysisModelId,
            CancellationToken token = default)
        {
            var getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                = new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext, tenantRegistryId);

            return getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                .ExecuteAsync(entityAnalysisModelId, 5, true, token);
        }

        private void ExtractRule(Rule ruleChild)
        {
            ProcessChildrenRules(ruleChild);

            if (ValidateRuleNotNull(ruleChild))
            {
                return;
            }

            AddToken(ruleChild);
            ConcatenateSql(ruleChild);
        }

        private void ProcessChildrenRules(Rule ruleChild)
        {
            if (ruleChild?.Rules == null)
            {
                return;
            }

            Sql += "(";
            for (var j = 0; j < ruleChild.Rules.Count; j++)
            {
                ExtractRule(ruleChild.Rules.ElementAt(j));

                if (j < ruleChild.Rules.Count - 1)
                {
                    Sql = Sql + " " + ruleChild.Condition + " ";
                }
            }

            Sql += ")";
        }

        private void ConcatenateSql(Rule ruleChild)
        {
            if (ruleChild == null)
            {
                return;
            }

            var field = ReturnField(ruleChild.Id);

            Sql += ruleChild.Operator switch
            {
                "equal" => $"{field} = (@{tokens.Count})",
                "not_equal" => $"not {field} = (@{tokens.Count})",
                "less" => $"{field} < (@{tokens.Count})",
                "less_or_equal" => $"{field} <= (@{tokens.Count})",
                "greater" => $"{field} >= (@{tokens.Count})",
                "greater_or_equal" => $"{field} >= (@{tokens.Count})",
                "like" => $"{field} like (@{tokens.Count})",
                "not_like" => $"not {field} like (@{tokens.Count})",
                "order" => ruleChild.Operator,
                _ => throw new InvalidOperationException($"Invalid SQL operator {ruleChild.Operator}.")
            };
        }

        private static bool ValidateRuleNotNull(Rule ruleChild)
        {
            if (ruleChild?.Rules != null)
            {
                return true;
            }

            return ruleChild is not { Value: not null, Operator: not null, Field: not null };
        }

        private void AddToken(Rule ruleChild)
        {
            switch (ruleChild.Type)
            {
                case "integer":
                    tokens.Add(Int32.Parse(ruleChild.Value));
                    break;
                case "double":
                    tokens.Add(Double.Parse(ruleChild.Value));
                    break;
                case "string":
                    tokens.Add(ruleChild.Value);
                    break;
                case "datetime":
                    var date = DateTimeOffset.TryParse(ruleChild.Value, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeLocal, out var dto)
                        ? dto.UtcDateTime
                        : DateTime.UtcNow;
                    tokens.Add(date);
                    break;
                case "boolean":
                    tokens.Add(Boolean.Parse(ruleChild.Value));
                    break;
                default:
                    tokens.Add(ruleChild.Value);
                    break;
            }
        }

        private string ReturnField(string id)
        {
            var matched = completionDto.FirstOrDefault(f => f.Name == id);
            return matched != null ? matched.ValueSqlPath : throw new InvalidOperationException($"Not found {id} in completions list.");
        }
    }
}
