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
    using Code;
    using Data.Context;
    using Data.Poco;
    using Data.Query.CaseQuery;
    using Data.Query.CaseQuery.Dto;
    using Data.Repository;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class GetCaseByIdQueryController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly GetCaseByIdQuery query;
        private readonly CaseEventRepository repository;
        private readonly string userName;

        public GetCaseByIdQueryController(ILog log, IHttpContextAccessor httpContextAccessor,
            DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            this.log = log;

            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));

            permissionValidation = new PermissionValidation(dbContext, userName);
            query = new GetCaseByIdQuery(dbContext, userName);

            repository = new CaseEventRepository(dbContext, userName);
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

        [HttpGet("{id:int}")]
        public ActionResult<CaseQueryDto> Get(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        1
                    }))
                {
                    return Forbid();
                }

                var queryResults = query.Execute(id);

                if (query == null)
                {
                    return NotFound();
                }

                var caseEvent = new CaseEvent
                {
                    CaseId = queryResults.Id,
                    CaseEventTypeId = 4,
                    CaseKey = queryResults.CaseKey,
                    CaseKeyValue = queryResults.CaseKeyValue
                };

                repository.Insert(caseEvent);

                return Ok(queryResults);

            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
