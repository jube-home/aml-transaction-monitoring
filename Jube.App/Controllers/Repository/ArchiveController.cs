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

namespace Jube.App.Controllers.Repository
{
    using System.Threading;
    using System.Threading.Tasks;
    using Code;
    using Data.Context;
    using Data.Repository;
    using Dto;
    using DynamicEnvironment;
    using FluentValidation;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Validators;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ArchiveController : Controller
    {
        private readonly DbContext dbContext;
        private readonly PermissionValidation permissionValidation;
        private readonly ArchiveRepository repository;
        private readonly string userName;
        private readonly IValidator<ArchiveTagDto> validator;

        public ArchiveController(ILog log, DynamicEnvironment dynamicEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), log);
            repository = new ArchiveRepository(dbContext);
            permissionValidation = new PermissionValidation(dbContext, userName, log);

            validator = new ArchiveTagDtoValidator();
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

        [HttpPut("tag")]
        public async Task<ActionResult<CaseNoteDto>> TagAsync([FromBody] ArchiveTagDto model, CancellationToken token = default)
        {
            if (!permissionValidation.Validate(new[]
                {
                    1
                }))
            {
                return Forbid();
            }

            var results = await validator.ValidateAsync(model, token);

            if (!results.IsValid)
            {
                return BadRequest();
            }

            var archive = await repository
                .UpdateTagsByEntityAnalysisModelInstanceEntryGuidAsync(
                    model.EntityAnalysisModelInstanceEntryGuid,
                    model.Tag,
                    token).ConfigureAwait(false);

            var caseRepository = new CaseRepository(dbContext);
            await caseRepository.UpdateArchiveJsonAsync(model.EntityAnalysisModelInstanceEntryGuid, archive.Json, token);

            var archiveTagRepository = new ArchiveTagRepository(dbContext, userName);
            await archiveTagRepository.MergeTagsAsync(model.EntityAnalysisModelInstanceEntryGuid, model.Tag, token);

            return Ok();
        }
    }
}
