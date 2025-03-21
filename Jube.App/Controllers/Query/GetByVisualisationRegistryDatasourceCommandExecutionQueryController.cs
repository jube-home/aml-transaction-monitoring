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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Jube.App.Code;
using Jube.Data.Context;
using Jube.Data.Query;
using Jube.Engine.Helpers;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.App.Controllers.Query
{
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
            IHttpContextAccessor httpContextAccessor, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            this.log = log;

            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            permissionValidation = new PermissionValidation(dbContext, userName);

            if (dynamicEnvironment.AppSettings("ReportConnectionString") != null)
            {
                query = new GetByVisualisationRegistryDatasourceCommandExecutionQuery(dbContext, 
                    dynamicEnvironment.AppSettings("ReportConnectionString"),userName);
            }
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

        [HttpPost("{id}")]
        public async Task<ActionResult<dynamic>> ExecuteAsync()
        {
            try
            {
                if (!permissionValidation.Validate(new[] { 28, 1 })) return Forbid();

                var idFromRoute = Request.RouteValues["id"]?.ToString();
                if (idFromRoute != null)
                {
                    var idParsedToInt = int.Parse(idFromRoute);

                    var ms = new MemoryStream();
                    await Request.Body.CopyToAsync(ms);

                    var payloadString = Encoding.UTF8.GetString(ms.ToArray());
                    var jArray = JsonConvert.DeserializeObject<JArray>(payloadString);

                    var parameters = new Dictionary<int, object>();
                    if (jArray != null)
                        foreach (var param in jArray)
                        {
                            var value = param.SelectToken("value");
                            var id = param.SelectToken("id");

                            if (value != null && id != null)
                            {
                                switch (value.Type)
                                {
                                    case JTokenType.String:
                                        parameters.Add(int.Parse(id.ToString()),
                                            value.ToString());
                                        break;
                                    case JTokenType.Integer:
                                        parameters.Add(int.Parse(id.ToString()),
                                            int.Parse(value.ToString()));
                                        break;
                                    case JTokenType.None:
                                    case JTokenType.Object:
                                    case JTokenType.Array:
                                    case JTokenType.Constructor:
                                    case JTokenType.Property:
                                    case JTokenType.Comment:
                                    case JTokenType.Float:
                                    case JTokenType.Boolean:
                                    case JTokenType.Null:
                                    case JTokenType.Undefined:
                                    case JTokenType.Date:
                                    case JTokenType.Raw:
                                    case JTokenType.Bytes:
                                    case JTokenType.Guid:
                                    case JTokenType.Uri:
                                    case JTokenType.TimeSpan:
                                    default:
                                    {
                                        if (id.Type == JTokenType.Float)
                                            parameters.Add(int.Parse(id.ToString()),
                                                double.Parse(value.ToString()));
                                        else
                                            parameters.Add(int.Parse(id.ToString()),
                                                value.ToString());
                                        break;
                                    }
                                }
                            }
                        }

                    return Ok(await query.ExecuteAsync(idParsedToInt, parameters));
                }

                return StatusCode(500);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}