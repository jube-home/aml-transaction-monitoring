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

namespace Jube.App.Controllers.Query
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Code;
    using Data.Context;
    using Data.Poco;
    using Data.Query;
    using Data.Reporting;
    using Data.Repository;
    using Dto;
    using DynamicEnvironment;
    using FluentValidation;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;
    using Validators;

    [Route("api/[controller]")]
    [Authorize]
    public class GetEntityAnalysisModelSampleController : Controller
    {
        private readonly DbContext dbContext;
        private readonly EntityAnalysisModelRepository entityAnalysisModelRepository;
        private readonly EntityAnalysisModelSampleExecutionLogRepository entityAnalysisModelSampleExecutionLogRepository;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly Postgres postgres;
        private readonly GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery query;
        private readonly string userName;
        private readonly IValidator<EntityAnalysisModelSampleOptionsDto> validator;

        public GetEntityAnalysisModelSampleController(
            ILog log,
            IHttpContextAccessor httpContextAccessor,
            DynamicEnvironment dynamicEnvironment)
        {
            userName = httpContextAccessor.HttpContext?.User.Identity?.Name;
            this.log = log;

            dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            permissionValidation = new PermissionValidation(dbContext, userName, log);

            validator = new EntityAnalysisModelSampleOptionsDtoValidator();

            var connString = dynamicEnvironment.AppSettings("ReportConnectionString")
                             ?? dynamicEnvironment.AppSettings("ConnectionString");

            postgres = new Postgres(connString, log, dynamicEnvironment.AppSettings("ParserAssertSelectOnly").Equals("True", StringComparison.OrdinalIgnoreCase));

            entityAnalysisModelRepository = new EntityAnalysisModelRepository(dbContext, userName);
            entityAnalysisModelSampleExecutionLogRepository = new EntityAnalysisModelSampleExecutionLogRepository(dbContext);
            query = new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext, userName);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dbContext.Close();
                dbContext.Dispose();
            }
            base.Dispose(disposing);
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteAsync(
            [FromBody] EntityAnalysisModelSampleOptionsDto model,
            CancellationToken token = default)
        {
            if (!permissionValidation.Validate(new[]
                {
                    40
                }))
            {
                return Forbid();
            }

            var results = await validator.ValidateAsync(model, token);
            if (!results.IsValid)
            {
                return BadRequest(results);
            }

            var entityAnalysisModel = await entityAnalysisModelRepository.GetByGuidAsync(model.EntityAnalysisModelGuid, token);

            if (entityAnalysisModel == null)
            {
                return NotFound();
            }

            var entityAnalysisModelSampleExecutionLog = new EntityAnalysisModelSampleExecutionLog
            {
                CreatedDate = DateTime.UtcNow,
                CreatedUser = userName,
                Sample = model.Sample,
                DateFrom = model.DateFrom?.UtcDateTime,
                DateTo = model.DateTo?.UtcDateTime,
                EntityAnalysisModelId = entityAnalysisModel.Id
            };

            var sw = new Stopwatch();
            sw.Start();
            try
            {
                var completions = (await query.ExecuteAsync(entityAnalysisModel.Id, 5, true, token)).ToArray();

                var jsonObjects = await postgres.ExecuteReturnOnlyJsonFromArchiveSampleAsync(
                        entityAnalysisModel.Id, model.Sample, model.DateFrom?.UtcDateTime, model.DateTo?.UtcDateTime, token)
                    .ConfigureAwait(false);

                var csv = BuildCsv(completions, jsonObjects);
                sw.Stop();

                entityAnalysisModelSampleExecutionLog.RowCount = jsonObjects.Count;
                entityAnalysisModelSampleExecutionLog.ResponseTime = sw.ElapsedMilliseconds;
                entityAnalysisModelSampleExecutionLog.InError = 0;

                await entityAnalysisModelSampleExecutionLogRepository.InsertAsync(entityAnalysisModelSampleExecutionLog, token);

                return File(
                    Encoding.UTF8.GetBytes(csv),
                    "text/csv",
                    $"sample_{entityAnalysisModel.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
            }
            catch (Exception ex)
            {
                entityAnalysisModelSampleExecutionLog.InError = 1;
                entityAnalysisModelSampleExecutionLog.ErrorStack = ex.ToString();

                await entityAnalysisModelSampleExecutionLogRepository.InsertAsync(entityAnalysisModelSampleExecutionLog, token);

                log.Error(ex);
                return StatusCode(500);
            }
        }

        private string BuildCsv(
            GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery.Dto[] completions,
            IEnumerable<JObject> jsonObjects)
        {
            var sb = new StringBuilder();

            var staticColumns = new[]
            {
                "EntityAnalysisModelInstanceEntryGuid", "ResponseElevation", "PrevailingEntityAnalysisModelActivationRuleId", "EntityAnalysisModelGuid", "EntityAnalysisModelActivationRuleCount", "CreatedDate", "ReferenceDate"
            };

            sb.AppendLine(String.Join(",",
                staticColumns.Select(QuoteCsvField)
                    .Concat(completions.Select(c => QuoteCsvField(c.Name)))));

            foreach (var jObject in jsonObjects)
            {
                var staticFields = new[]
                {
                    QuoteCsvField(jObject["entityAnalysisModelInstanceEntryGuid"]?.Value<string>() ?? String.Empty), jObject["responseElevation"]?["value"]?.Value<double>().ToString("G", CultureInfo.InvariantCulture) ?? "0", jObject["prevailingEntityAnalysisModelActivationRuleId"]?.Value<int>().ToString() ?? "0", QuoteCsvField(jObject["entityAnalysisModelGuid"]?.Value<string>() ?? String.Empty), jObject["entityAnalysisModelActivationRuleCount"]?.Value<int>().ToString() ?? "0", jObject["createdDate"]?.Value<DateTime>().ToString("O", CultureInfo.InvariantCulture) ?? String.Empty, jObject["referenceDate"]?.Value<DateTime>().ToString("O", CultureInfo.InvariantCulture) ?? String.Empty
                };

                var payloadFields = completions.Select(completion =>
                    ExtractValue(completion, jObject.SelectToken(completion.ValueJsonPath)));

                sb.AppendLine(String.Join(",", staticFields.Concat(payloadFields)));
            }

            return sb.ToString();
        }

        private string ExtractValue(
            GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery.Dto completion,
            JToken jToken)
        {
            if (jToken == null)
            {
                return completion.DataTypeId switch
                {
                    1 => QuoteCsvField(String.Empty),
                    2 or 3 or 6 or 7 => "0",
                    4 => String.Empty,
                    5 => "0",
                    _ => String.Empty
                };
            }

            try
            {
                if (!completion.Group.Equals("payload", StringComparison.OrdinalIgnoreCase))
                {
                    return jToken.Value<double>().ToString("G", CultureInfo.InvariantCulture);
                }

                return completion.DataTypeId switch
                {
                    1 => QuoteCsvField(jToken.Value<string>() ?? String.Empty),
                    2 => jToken.Value<int>().ToString(),
                    3 or 6 or 7 => jToken.Value<double>().ToString("G", CultureInfo.InvariantCulture),
                    4 => jToken.Value<DateTime>().ToString("O", CultureInfo.InvariantCulture),
                    5 => "1",
                    _ => QuoteCsvField(jToken.Value<string>() ?? String.Empty)
                };
            }
            catch (Exception e)
            {
                log.Info($"Type coercion failed for field '{completion.Name}' " +
                         $"(DataTypeId={completion.DataTypeId}, token='{jToken}', " +
                         $"tokenType={jToken.Type}): {e.Message}");

                return completion.DataTypeId == 1 ? QuoteCsvField(String.Empty) : "0";
            }
        }

        private static string QuoteCsvField(string value)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
