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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Code;
    using Data.Context;
    using Data.Poco;
    using Data.Query;
    using Data.Repository;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class GetByVisualisationRegistryDatasourceCommandExecutionQueryController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly GetByVisualisationRegistryDatasourceCommandExecutionQuery query;
        private readonly string userName;

        public GetByVisualisationRegistryDatasourceCommandExecutionQueryController(ILog log,
            IHttpContextAccessor httpContextAccessor, DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            this.log = log;

            dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), log);
            permissionValidation = new PermissionValidation(dbContext, userName, log);
            query = dynamicEnvironment.AppSettings("ReportConnectionString") != null ?
                new GetByVisualisationRegistryDatasourceCommandExecutionQuery(dbContext, userName, log, dynamicEnvironment.AppSettings("ReportConnectionString"))
                : new GetByVisualisationRegistryDatasourceCommandExecutionQuery(dbContext, userName, log, dynamicEnvironment.AppSettings("ConnectionString"));
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

        // ReSharper disable once RouteTemplates.RouteParameterIsNotPassedToMethod
        [HttpPost("{id}")]
        public async Task<ActionResult<dynamic>> ExecuteAsync(
            [FromRoute] int id,
            [FromBody] JArray parameters,
            CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        28, 1
                    }))
                {
                    return Forbid();
                }

                var parametersByParsedId = parameters.ToDictionary(parameter => (int)parameter.SelectToken("id"), parameter => parameter.SelectToken("value"));
                var parametersByName = await ParametersByNameAsync(id, token, parametersByParsedId);

                string error = null;
                var values = new List<IDictionary<string, object>>();
                var sw = new Stopwatch();
                try
                {
                    sw.Start();
                    values = await query.ExecuteAsync(id, parametersByName, token).ConfigureAwait(false);
                    sw.Stop();
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    error = ex.ToString();
                }

                await StoreAuditAsync(id, parameters, token, values, error, sw, parametersByParsedId);

                return values;
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        private async Task StoreAuditAsync(int id, JArray parameters, CancellationToken token, List<IDictionary<string, object>> values, string error, Stopwatch sw, Dictionary<int, JToken> paramsByParsedId)
        {
            var visualisationRegistryDatasourceExecutionLog =
                new VisualisationRegistryDatasourceExecutionLog
                {
                    Records = values.Count,
                    Error = error,
                    ResponseTime = (int)sw.ElapsedMilliseconds,
                    VisualisationRegistryDatasourceId = id,
                    CreatedDate = DateTime.Now,
                    CreatedUser = userName
                };

            var visualisationRegistryDatasourceExecutionLogRepository
                = new VisualisationRegistryDatasourceExecutionLogRepository(dbContext);

            visualisationRegistryDatasourceExecutionLog =
                await visualisationRegistryDatasourceExecutionLogRepository.InsertAsync(
                    visualisationRegistryDatasourceExecutionLog, token);

            var visualisationRegistryDatasourceExecutionLogParameterRepository
                = new VisualisationRegistryDatasourceExecutionLogParameterRepository(dbContext);

            for (var i = 0; i < parameters.Count; i++)
            {
                var visualisationRegistryDatasourceExecutionLogParameter = new VisualisationRegistryDatasourceExecutionLogParameter
                {
                    Value = paramsByParsedId.ElementAt(i).Value.ToString(),
                    VisualisationRegistryDatasourceExecutionLogId =
                        visualisationRegistryDatasourceExecutionLog.Id,
                    VisualisationRegistryParameterId = paramsByParsedId.ElementAt(i).Key
                };

                await visualisationRegistryDatasourceExecutionLogParameterRepository
                    .InsertAsync(visualisationRegistryDatasourceExecutionLogParameter, token);
            }
        }

        private async Task<Dictionary<string, object>> ParametersByNameAsync(int id, CancellationToken token, Dictionary<int, JToken> paramsByParsedId)
        {
            var visualisationRegistryParameterRepository = new VisualisationRegistryParameterRepository(dbContext, userName);
            var visualisationRegistryParameters = await visualisationRegistryParameterRepository.GetByVisualisationRegistryDatasourceIdAsync(id, token);

            var parametersByName = new Dictionary<string, object>();
            foreach (var parameter in paramsByParsedId)
            {
                var visualisationRegistry = visualisationRegistryParameters.FirstOrDefault(f => f.Id == parameter.Key);
                if (visualisationRegistry == null)
                {
                    continue;
                }

                var cleanName = visualisationRegistry.Name.Replace(" ", "_");

                switch (visualisationRegistry.DataTypeId)
                {
                    case 1:
                        parametersByName.Add(cleanName, parameter.Value == null ? visualisationRegistry.DefaultValue : parameter.Value.ToString());
                        break;
                    case 2:
                        parametersByName.Add(cleanName, parameter.Value == null ? Int32.Parse(visualisationRegistry.DefaultValue) : Int32.Parse(parameter.Value.ToString()));
                        break;
                    case 3:
                        parametersByName.Add(cleanName, parameter.Value == null ? Double.Parse(visualisationRegistry.DefaultValue) : Double.Parse(parameter.Value.ToString()));
                        break;
                    case 4:
                        parametersByName.Add(cleanName, parameter.Value == null ? DateTime.Now.AddDays(Int32.Parse(visualisationRegistry.DefaultValue) * -1) : DateTime.Parse(parameter.Value.ToString()));
                        break;
                    case 5:
                        parametersByName.Add(cleanName, parameter.Value == null ? Byte.Parse(visualisationRegistry.DefaultValue) : Byte.Parse(parameter.Value.ToString()));
                        break;
                    default:
                        parametersByName.Add(cleanName, parameter.Value == null ? visualisationRegistry.DefaultValue : parameter.Value.ToString());
                        break;
                }
            }

            return parametersByName;
        }
    }
}
