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

namespace Jube.App.Controllers.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Code;
    using Data.Context;
    using Data.Query;
    using Data.Repository;
    using Dto.Requests;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CompletionsController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string userName;

        public CompletionsController(ILog log, IHttpContextAccessor httpContextAccessor,
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

        [HttpGet("ByCaseWorkflowId")]
        public ActionResult<List<CompletionDto>> GetByCaseWorkflowId(int caseWorkflowId)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        25, 17, 20
                    }))
                {
                    return Forbid();
                }

                var caseWorkflowRepository = new CaseWorkflowRepository(dbContext, userName);
                var entityAnalysisModelId = caseWorkflowRepository.GetById(caseWorkflowId).EntityAnalysisModelId;

                if (entityAnalysisModelId == null)
                {
                    return NotFound();
                }

                var completionDtos = CompletionDtos(entityAnalysisModelId.Value, 5, true);

                return Ok(completionDtos);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCaseWorkflowGuidIncludingDeleted")]
        public ActionResult<List<CompletionDto>> GetByCaseWorkflowGuidIncludingDeleted(Guid caseWorkflowGuid)
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

                var caseWorkflowRepository = new CaseWorkflowRepository(dbContext, userName);
                var entityAnalysisModelId =
                    caseWorkflowRepository.GetByGuidIncludingDeleted(caseWorkflowGuid).EntityAnalysisModelId;

                if (entityAnalysisModelId == null)
                {
                    return NotFound();
                }

                var completionDtos = CompletionDtos(entityAnalysisModelId.Value, 5, true);

                return Ok(completionDtos);
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(204);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCaseWorkflowIdIncludingDeleted")]
        public ActionResult<List<CompletionDto>> GetByCaseWorkflowIdIncludingDeleted(int caseWorkflowId)
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

                var caseWorkflowRepository = new CaseWorkflowRepository(dbContext, userName);
                var entityAnalysisModelId =
                    caseWorkflowRepository.GetByIdIncludingDeleted(caseWorkflowId).EntityAnalysisModelId;

                if (entityAnalysisModelId == null)
                {
                    return NotFound();
                }

                var completionDtos = CompletionDtos(entityAnalysisModelId.Value, 5, true);

                return Ok(completionDtos);
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(204);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByEntityAnalysisModelId")]
        public ActionResult<List<CompletionDto>> GetByEntityAnalysisModelId(int entityAnalysisModelId)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        16
                    }))
                {
                    return Forbid();
                }

                var completionDtos = CompletionDtos(entityAnalysisModelId, 6, true);

                return Ok(completionDtos);
            }
            catch (Exception e)
            {
                log.Error(e);
                throw;
            }
        }

        [HttpGet("ByEntityAnalysisModelIdParseTypeId")]
        public ActionResult<List<CompletionDto>> GetByEntityAnalysisModelIdParseTypeId(int entityAnalysisModelId,
            int parseTypeId)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        8, 10, 13, 14, 17, 20, 26
                    }))
                {
                    return Forbid();
                }

                var completionDtos = CompletionDtos(entityAnalysisModelId, parseTypeId, false);

                return Ok(completionDtos);
            }
            catch (Exception e)
            {
                log.Error(e);
                throw;
            }
        }

        private List<CompletionDto> CompletionDtos(int entityAnalysisModelId, int parseTypeId, bool reporting)
        {
            var getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                = new GetModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext, userName);

            var completionDtos = getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                .Execute(entityAnalysisModelId, parseTypeId, reporting)
                .Select(field => new CompletionDto
                {
                    Score = 1000,
                    Name = field.Name,
                    Value = field.Value,
                    Field = field.ValueSqlPath,
                    Meta = $"{field.Name}:{field.JQueryBuilderDataType}",
                    Group = field.Group,
                    DataType = field.JQueryBuilderDataType,
                    XPath = field.ValueJsonPath
                })
                .ToList();

            return completionDtos;
        }
    }
}
