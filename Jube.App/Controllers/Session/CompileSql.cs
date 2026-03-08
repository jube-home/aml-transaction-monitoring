namespace Jube.App.Controllers.Session
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Code.QueryBuilder;
    using Data.Context;
    using Data.Poco;
    using Data.Reporting;
    using Data.Repository;
    using log4net;
    using Newtonsoft.Json;

    public static class CompileSql
    {
        public static async Task<SessionCaseSearchCompiledSql> CompileAsync(DbContext dbContext,
            SessionCaseSearchCompiledSql model, string userName, ILog log, string reportConnectionString = null, CancellationToken token = default)
        {
            var filterJsonRule = JsonConvert.DeserializeObject<Rule>(model.FilterJson);
            var filterRule = await Parser.CreateAsync(filterJsonRule, dbContext, model.CaseWorkflowGuid, userName, token).ConfigureAwait(false);

            var selectTokensRule = JsonConvert.DeserializeObject<Rule>(model.SelectJson);
            var selectRule =
                await Parser.CreateAsync(selectTokensRule, dbContext, model.CaseWorkflowGuid, userName, token).ConfigureAwait(false);

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

            var caseWorkflowXPathRepository = new CaseWorkflowXPathRepository(dbContext, userName);
            var caseWorkflowXPaths = (await caseWorkflowXPathRepository.GetByCasesWorkflowGuidActiveOnlyAsync(model.CaseWorkflowGuid, token)).ToList();

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

                if (rule.Id is not ("Id" or "CaseKey" or "CaseKeyValue" or "CaseWorkflowStatus" or "Locked" or "Diary" or "ClosedStatusId" or "Priority"))
                {
                    if (caseWorkflowXPaths.FirstOrDefault(w => w.XPath.Equals(rule.Id, StringComparison.CurrentCultureIgnoreCase)) == null)
                    {
                        continue;
                    }
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
                return await repository.InsertAsync(model, token);
            }

            filterRule.Tokens.Add(model.CaseWorkflowGuid);
            var positionCaseWorkflowGuid = filterRule.Tokens.Count;

            filterRule.Tokens.Add(userName);
            var positionUser = filterRule.Tokens.Count;

            model.SelectSqlSearch = "select " + String.Join(",", columnsSelect);

            model.WhereSql = "from \"Case\",\"CaseWorkflow\",\"EntityAnalysisModel\",\"TenantRegistry\"," +
                             "\"CaseWorkflowStatus\",\"UserInTenant\",\"CaseWorkflowRole\",\"CaseWorkflowStatusRole\"," +
                             "(select \"RoleRegistry\".\"Guid\" from \"RoleRegistry\",\"UserRegistry\" where \"RoleRegistry\".\"Id\" = \"UserRegistry\".\"RoleRegistryId\" and (\"RoleRegistry\".\"Deleted\" = 0 or \"RoleRegistry\".\"Deleted\" IS NULL) and \"UserRegistry\".\"Name\" = (@" + positionUser + ")) \"RoleRegistry\"" +
                             " where \"EntityAnalysisModel\".\"Id\" = \"CaseWorkflow\".\"EntityAnalysisModelId\"" +
                             " and \"EntityAnalysisModel\".\"TenantRegistryId\" = \"TenantRegistry\".\"Id\"" +
                             " and \"UserInTenant\".\"TenantRegistryId\" = \"TenantRegistry\".\"Id\"" +
                             " and \"Case\".\"CaseWorkflowGuid\" = \"CaseWorkflow\".\"Guid\"" +
                             " and (\"Case\".\"CaseWorkflowStatusGuid\" = \"CaseWorkflowStatus\".\"Guid\"" +
                             " and (\"CaseWorkflowStatus\".\"Deleted\" = 0" +
                             " or \"CaseWorkflowStatus\".\"Deleted\" IS null) ) and " + filterRule.Sql +
                             " and (\"CaseWorkflow\".\"Guid\" = uuid(@" + positionCaseWorkflowGuid + ") " +
                             " and (\"CaseWorkflowRole\".\"CaseWorkflowGuid\" = \"CaseWorkflow\".\"Guid\" and \"CaseWorkflowRole\".\"RoleRegistryGuid\" = \"RoleRegistry\".\"Guid\" and (\"CaseWorkflowRole\".\"Deleted\" = 0 or \"CaseWorkflowRole\".\"Deleted\" IS NULL)) " +
                             " and (\"CaseWorkflowStatusRole\".\"CaseWorkflowStatusGuid\" = \"CaseWorkflowStatus\".\"Guid\" and \"CaseWorkflowStatusRole\".\"RoleRegistryGuid\" = \"RoleRegistry\".\"Guid\" and (\"CaseWorkflowStatusRole\".\"Deleted\" = 0 or \"CaseWorkflowStatusRole\".\"Deleted\" IS NULL)) " +
                             " and (\"CaseWorkflow\".\"Deleted\" = 0 or \"CaseWorkflow\".\"Deleted\" is null))" +
                             " and \"UserInTenant\".\"User\" = (@" + positionUser + ")";

            model.OrderSql = "order by " + String.Join(",", columnsOrder);

            model.FilterTokens = JsonConvert.SerializeObject(filterRule.Tokens);

            try
            {
                var postgres = new Postgres(reportConnectionString ?? dbContext.ConnectionString, log);
                await postgres.PrepareAsync(model.SelectSqlSearch + " " + model.WhereSql + " " + model.OrderSql,
                    filterRule.Tokens, token).ConfigureAwait(false);
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

            return await repository.InsertAsync(model, token);
        }
    }
}
