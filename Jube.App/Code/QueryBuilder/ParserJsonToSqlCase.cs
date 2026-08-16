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

namespace Jube.App.Code.QueryBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Query;
    using Data.Repository;

    public class ParserJsonToSqlCase
    {
        public readonly List<Rule> Rules = new List<Rule>();
        public readonly List<object> Tokens = new List<object>();
        private IEnumerable<GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery.Dto> completionDto;
        public string Sql;

        public static async Task<ParserJsonToSqlCase> CreateAsync(Rule rule, DbContext dbContext, Guid caseWorkflowGuid, string userName, CancellationToken token = default)
        {
            var parser = new ParserJsonToSqlCase();
            parser.completionDto = await parser.GetCompletionsAsync(dbContext, caseWorkflowGuid, userName, token).ConfigureAwait(false);
            parser.ExtractRule(rule);
            return parser;
        }

        private async Task<IEnumerable<GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery.Dto>> GetCompletionsAsync(DbContext dbContext,
            Guid caseWorkflowGuid, string userName, CancellationToken token = default)
        {
            var caseWorkflowRepository = new CaseWorkflowRepository(dbContext, userName);

            var entityAnalysisModelId =
                (await caseWorkflowRepository.GetByGuidIncludingDeletedAsync(caseWorkflowGuid, token)).EntityAnalysisModelId;

            var getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                = new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext, userName);

            if (entityAnalysisModelId != null)
            {
                return await getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                    .ExecuteAsync(entityAnalysisModelId.Value, 5, true, token).ConfigureAwait(false);
            }

            throw new Exception(
                $"Could not lookup model for Case Workflow Id {caseWorkflowGuid} and therefore could not lookup completions.");
        }

        private void ExtractRule(Rule ruleChild)
        {
            ProcessChildrenRules(ruleChild);

            if (ValidateRuleNotNull(ruleChild))
            {
                return;
            }

            AddRule(ruleChild);
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

            if (ruleChild.Id == "CaseWorkflowStatusGuid" && ruleChild.Operator != "order")
            {
                Sql += ruleChild.Operator switch
                {
                    "equal" => $"{field} = uuid(@{Tokens.Count})",
                    "not_equal" => $"not {field} = uuid(@{Tokens.Count})",
                    "order" => ruleChild.Operator,
                    _ => throw new InvalidOperationException($"Invalid SQL operator {ruleChild.Operator}.")
                };

                return;
            }

            Sql += ruleChild.Operator switch
            {
                "equal" => $"{field} = (@{Tokens.Count})",
                "not_equal" => $"not {field} = (@{Tokens.Count})",
                "less" => $"{field} < (@{Tokens.Count})",
                "less_or_equal" => $"{field} <= (@{Tokens.Count})",
                "greater" => $"{field} >= (@{Tokens.Count})",
                "greater_or_equal" => $"{field} >= (@{Tokens.Count})",
                "like" => $"{field} like (@{Tokens.Count})",
                "not_like" => $"not {field} like (@{Tokens.Count})",
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

        private void AddRule(Rule ruleChild)
        {
            Rules.Add(ruleChild);
        }

        private void AddToken(Rule ruleChild)
        {
            switch (ruleChild.Type)
            {
                case "integer":
                    Tokens.Add(Int32.Parse(ruleChild.Value));
                    break;
                case "double":
                    Tokens.Add(Double.Parse(ruleChild.Value));
                    break;
                case "string":
                    Tokens.Add(ruleChild.Value);
                    break;
                case "datetime":
                    var date = DateTimeOffset.TryParse(ruleChild.Value, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeLocal, out var dto)
                        ? dto.UtcDateTime
                        : DateTime.UtcNow;
                    Tokens.Add(date);
                    break;
                case "boolean":
                    Tokens.Add(Boolean.Parse(ruleChild.Value));
                    break;
                default:
                    Tokens.Add(ruleChild.Value);
                    break;
            }
        }

        private string ReturnField(string id)
        {
            if (IsCaseField(id) != null)
            {
                return $"\"Case\".\"{id}\"";
            }

            if (IsCaseWorkflowStatusField(id) != null)
            {
                return $"\"CaseWorkflowStatus\".\"{id}\"";
            }

            var matched = completionDto.FirstOrDefault(f => f.Name == id);

            if (matched != null)
            {
                return matched.ValueSqlPath;
            }

            throw new InvalidOperationException($"Not found {id} in completions list.");
        }

        private static string IsCaseWorkflowStatusField(string name)
        {
            return name switch
            {
                "Priority" => name,
                "CaseWorkflowStatus" => name,
                _ => null
            };
        }

        private static string IsCaseField(string name)
        {
            return name switch
            {
                "Id" => name,
                "EntityAnalysisModelInstanceEntryGuid" => name,
                "DiaryDate" => name,
                "CaseWorkflowId" => name,
                "CaseWorkflowStatusGuid" => name,
                "CreatedDate" => name,
                "Locked" => name,
                "LockedUser" => name,
                "LockedDate" => name,
                "ClosedStatusId" => name,
                "ClosedDate" => name,
                "ClosedUser" => name,
                "CaseKey" => name,
                "Diary" => name,
                "DiaryUser" => name,
                "Rating" => name,
                "CaseKeyValue" => name,
                "ClosedStatusMigrationDate" => name,
                _ => null
            };
        }
    }
}
