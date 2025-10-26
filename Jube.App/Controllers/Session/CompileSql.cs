namespace Jube.App.Controllers.Session
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Code.QueryBuilder;
    using Data.Context;
    using Data.Poco;
    using Data.Reporting;
    using Data.Repository;
    using Newtonsoft.Json;

    public static class CompileSql
    {
        public static async Task<SessionCaseSearchCompiledSql> Compile(DbContext dbContext,
            SessionCaseSearchCompiledSql model, string userName)
        {
            var filterJsonRule = JsonConvert.DeserializeObject<Rule>(model.FilterJson);
            var filterRule = new Parser(filterJsonRule, dbContext, model.CaseWorkflowGuid, userName);
            var selectTokensRule = JsonConvert.DeserializeObject<Rule>(model.SelectJson);
            var selectRule =
                new Parser(selectTokensRule, dbContext, model.CaseWorkflowGuid, userName);

            model.SelectSqlDisplay = "select \"Case\".\"Id\" as \"Id\"," +
                                     "\"Case\".\"EntityAnalysisModelInstanceEntryGuid\" as \"EntityAnalysisModelInstanceEntryGuid\"," +
                                     "\"Case\".\"DiaryDate\" as \"DiaryDate\"," +
                                     "\"Case\".\"CaseWorkflowGuid\" as \"CaseWorkflowGuid\"," +
                                     "\"Case\".\"CaseWorkflowStatusGuid\" as \"CaseWorkflowStatusGuid\"," +
                                     "\"Case\".\"CreatedDate\" as \"CreatedDate\"," +
                                     "\"Case\".\"Locked\" as \"Locked\"," +
                                     "\"Case\".\"LockedUser\" as \"LockedUser\"," +
                                     "\"Case\".\"LockedDate\" as \"LockedDate\"," +
                                     "\"Case\".\"ClosedStatusId\" as \"ClosedStatusId\"," +
                                     "\"Case\".\"ClosedDate\" as \"ClosedDate\"," +
                                     "\"Case\".\"ClosedUser\" as \"ClosedUser\"," +
                                     "\"Case\".\"CaseKey\" as \"CaseKey\"," +
                                     "\"Case\".\"Diary\" as \"Diary\"," +
                                     "\"Case\".\"DiaryUser\" as \"DiaryUser\"," +
                                     "\"Case\".\"Rating\" as \"Rating\"," +
                                     "\"Case\".\"CaseKeyValue\" as \"CaseKeyValue\"," +
                                     "\"Case\".\"ClosedStatusMigrationDate\" as \"ClosedStatusMigrationDate\"," +
                                     "\"Case\".\"Json\" as \"Json\"," +
                                     "\"CaseWorkflow\".\"EnableVisualisation\" as \"EnableVisualisation\"," +
                                     "\"CaseWorkflow\".\"VisualisationRegistryGuid\" as \"VisualisationRegistryGuid\"," +
                                     "\"CaseWorkflowStatus\".\"ForeColor\" as \"ForeColor\"," +
                                     "\"CaseWorkflowStatus\".\"BackColor\" as \"BackColor\" ";

            var columnsSelect = new List<string>
            {
                "\"Case\".\"Id\" as \"Id\"",
                "\"CaseWorkflowStatus\".\"BackColor\" as \"BackColor\"",
                "\"CaseWorkflowStatus\".\"ForeColor\" as \"ForeColor\""
            };

            var columnsOrder = new List<string>();
            foreach (var rule in selectRule.Rules)
            {
                if (rule.Field == null || rule.Id == null)
                {
                    continue;
                }

                var convertedColumnSelectField = rule.Id switch
                {
                    "Locked" => $"case when {rule.Field} = 1 then 'Yes' else 'No' end",
                    "Diary" => $"case when {rule.Field} = 1 then 'Yes' else 'No' end",
                    "ClosedStatusId" => "case " + $"when {rule.Field} = 0 then 'Open' " +
                                        $"when {rule.Field} = 1 then 'Suspend Open' " +
                                        $"when {rule.Field} = 2 then 'Suspend Closed' " +
                                        $"when {rule.Field} = 3 then 'Closed' " +
                                        $"when {rule.Field} = 4 then 'Suspend Bypass' " + "end",
                    "Priority" => "case " + $"when {rule.Field} = 1 then 'Ultra High' " +
                                  $"when {rule.Field} = 2 then 'High' " +
                                  $"when {rule.Field} = 3 then 'Normal' " +
                                  $"when {rule.Field} = 4 then 'Low' " +
                                  $"when {rule.Field} = 5 then 'Ultra Low' " + "end",
                    _ => rule.Field
                };

                columnsOrder.Add(rule.Field + " " + rule.Value);

                if (rule.Id == "Id")
                {
                    continue;
                }

                if (rule.Id.Contains('.'))
                {
                    columnsSelect.Add(convertedColumnSelectField
                                      + " as \"" + rule.Id.Replace(".", "") + "\"");
                }
                else
                {
                    columnsSelect.Add(convertedColumnSelectField
                                      + " as \"" + rule.Id + "\"");
                }
            }

            var repository = new SessionCaseSearchCompiledSqlRepository(dbContext, userName);
            if (filterRule.Tokens == null)
            {
                return repository.Insert(model);
            }

            filterRule.Tokens.Add(model.CaseWorkflowGuid);
            var positionCaseWorkflowGuid = filterRule.Tokens.Count;

            filterRule.Tokens.Add(userName);
            var positionUser = filterRule.Tokens.Count;

            model.SelectSqlSearch = "select " + String.Join(",", columnsSelect);

            model.WhereSql = "from \"Case\",\"CaseWorkflow\",\"EntityAnalysisModel\",\"TenantRegistry\"," +
                             "\"CaseWorkflowStatus\",\"UserInTenant\"" +
                             " where \"EntityAnalysisModel\".\"Id\" = \"CaseWorkflow\".\"EntityAnalysisModelId\"" +
                             " and \"EntityAnalysisModel\".\"TenantRegistryId\" = \"TenantRegistry\".\"Id\"" +
                             " and \"UserInTenant\".\"TenantRegistryId\" = \"TenantRegistry\".\"Id\"" +
                             " and \"Case\".\"CaseWorkflowGuid\" = \"CaseWorkflow\".\"Guid\"" +
                             " and (\"Case\".\"CaseWorkflowStatusGuid\" = \"CaseWorkflowStatus\".\"Guid\"" +
                             " and (\"CaseWorkflowStatus\".\"Deleted\" = 0" +
                             " or \"CaseWorkflowStatus\".\"Deleted\" IS null) ) and " + filterRule.Sql +
                             " and (\"CaseWorkflow\".\"Guid\" = uuid(@" + positionCaseWorkflowGuid +
                             ") and (\"CaseWorkflow\".\"Deleted\" = 0 or \"CaseWorkflow\".\"Deleted\" is null))" +
                             " and \"UserInTenant\".\"User\" = (@" + positionUser + ")";

            model.OrderSql = "order by " + String.Join(",", columnsOrder);

            model.FilterTokens = JsonConvert.SerializeObject(filterRule.Tokens);

            try
            {
                var postgres = new Postgres(dbContext.ConnectionString);
                await postgres.PrepareAsync(model.SelectSqlSearch + " " + model.WhereSql + " " + model.OrderSql,
                    filterRule.Tokens).ConfigureAwait(false);
                model.Prepared = 1;
            }
            catch (Exception e)
            {
                model.Prepared = 0;
                model.Error = e.Message;
            }

            if (model.Rebuild == 1)
            {
                model.RebuildDate = DateTime.Now;
            }

            return repository.Insert(model);
        }
    }
}
