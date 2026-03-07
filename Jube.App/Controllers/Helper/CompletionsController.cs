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
    using System.Threading;
    using System.Threading.Tasks;
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

            dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), log);
            permissionValidation = new PermissionValidation(dbContext, userName, log);
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
        public async Task<ActionResult<List<CompletionDto>>> GetByCaseWorkflowIdAsync(int caseWorkflowId, CancellationToken token = default)
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
                var entityAnalysisModelId = (await caseWorkflowRepository.GetByIdAsync(caseWorkflowId, token)).EntityAnalysisModelId;

                if (entityAnalysisModelId == null)
                {
                    return NotFound();
                }

                var completionDtos = await CompletionDtosAsync(entityAnalysisModelId.Value, 5, true, token);

                return Ok(completionDtos);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCaseWorkflowGuidIncludingDeleted")]
        public async Task<ActionResult<List<CompletionDto>>> GetByCaseWorkflowGuidIncludingDeletedAsync(Guid caseWorkflowGuid, CancellationToken token = default)
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
                    (await caseWorkflowRepository.GetByGuidIncludingDeletedAsync(caseWorkflowGuid, token)).EntityAnalysisModelId;

                if (entityAnalysisModelId == null)
                {
                    return NotFound();
                }

                var completionDtos = await CompletionDtosAsync(entityAnalysisModelId.Value, 5, true, token);

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
        public async Task<ActionResult<List<CompletionDto>>> GetByCaseWorkflowIdIncludingDeletedAsync(int caseWorkflowId, CancellationToken token = default)
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
                    (await caseWorkflowRepository.GetByIdIncludingDeletedAsync(caseWorkflowId, token)).EntityAnalysisModelId;

                if (entityAnalysisModelId == null)
                {
                    return NotFound();
                }

                var completionDtos = await CompletionDtosAsync(entityAnalysisModelId.Value, 5, true, token);

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
        public async Task<ActionResult<List<CompletionDto>>> GetByEntityAnalysisModelIdAsync(int entityAnalysisModelId, CancellationToken token = default)
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

                var completionDtos = await CompletionDtosAsync(entityAnalysisModelId, 6, true, token).ConfigureAwait(false);

                return Ok(completionDtos);
            }
            catch (Exception e)
            {
                log.Error(e);
                throw;
            }
        }

        [HttpGet("ByEntityAnalysisModelIdParseTypeId")]
        public async Task<ActionResult<List<CompletionDto>>> GetByEntityAnalysisModelIdParseTypeIdAsync(int entityAnalysisModelId,
            int parseTypeId, CancellationToken token = default)
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

                var completionDtos = await CompletionDtosAsync(entityAnalysisModelId, parseTypeId, false, token);

                return Ok(completionDtos);
            }
            catch (Exception e)
            {
                log.Error(e);
                throw;
            }
        }

        private async Task<List<CompletionDto>> CompletionDtosAsync(int entityAnalysisModelId, int parseTypeId, bool reporting, CancellationToken token = default)
        {
            var getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                = new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext, userName);

            var modelFields = await getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                .ExecuteAsync(entityAnalysisModelId, parseTypeId, reporting, token).ConfigureAwait(false);

            return modelFields.Select(field => new CompletionDto
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
        }
    }
}
