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
    using System;
    using System.Collections.Generic;
    using AutoMapper;
    using Code;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Dto;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class VisualisationRegistryDatasourceSeriesController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly VisualisationRegistryDatasourceSeriesRepository repository;
        private readonly string userName;

        public VisualisationRegistryDatasourceSeriesController(ILog log
            , IHttpContextAccessor httpContextAccessor, DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            this.log = log;

            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            permissionValidation = new PermissionValidation(dbContext, userName);

            repository = new VisualisationRegistryDatasourceSeriesRepository(dbContext, userName);
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<VisualisationRegistryDatasourceSeriesDto, VisualisationRegistryDatasourceSeries>();
                cfg.CreateMap<VisualisationRegistryDatasourceSeries, VisualisationRegistryDatasourceSeriesDto>();
                cfg.CreateMap<List<VisualisationRegistryDatasourceSeries>,
                        List<VisualisationRegistryDatasourceSeriesDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            mapper = new Mapper(config);
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

        [HttpGet("ByVisualisationRegistryDatasourceId/{id:int}")]
        public ActionResult<List<VisualisationRegistryDatasourceSeriesDto>>
            GetByVisualisationRegistryDatasourceId(int id)
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

                return Ok(mapper.Map<List<VisualisationRegistryDatasourceSeriesDto>>(
                    repository.GetByVisualisationRegistryDatasourceId(id)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
