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
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Code;
    using Data.Context;
    using Dto.TreeChildren;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class IconsController : Controller
    {
        private readonly DbContext dbContext;
        private readonly IWebHostEnvironment env;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string userName;

        public IconsController(ILog log, IWebHostEnvironment webHostEnvironment,
            IHttpContextAccessor httpContextAccessor, DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            this.log = log;
            env = webHostEnvironment;

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

        [HttpGet]
        public Task<ActionResult<List<IconDto>>> GetIconsAsync()
        {
            if (!permissionValidation.Validate(new[]
                {
                    24
                }))
            {
                return Task.FromResult<ActionResult<List<IconDto>>>(Forbid());
            }

            try
            {
                var webRoot = env.WebRootPath;
                var directoryPath = Path.Combine(webRoot, "icons");

                return Task.FromResult<ActionResult<List<IconDto>>>(Directory.GetFiles(directoryPath).Select(file => new IconDto
                    {
                        Name = Path.GetFileName(file)
                    })
                    .ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return Task.FromResult<ActionResult<List<IconDto>>>(StatusCode(500));
            }
        }
    }
}
