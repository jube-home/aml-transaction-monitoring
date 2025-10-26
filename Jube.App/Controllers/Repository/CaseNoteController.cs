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
    using FluentValidation;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;
    using Validators;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CaseNoteController : Controller
    {
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseNoteRepository repository;
        private readonly string userName;
        private readonly IValidator<CaseNoteDto> validator;

        public CaseNoteController(ILog log,
            DynamicEnvironment dynamicEnvironment
            , IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            this.log = log;

            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            permissionValidation = new PermissionValidation(dbContext, userName);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CaseNote, CaseNoteDto>();
                cfg.CreateMap<CaseNoteDto, CaseNote>();
                cfg.CreateMap<List<CaseNote>, List<CaseNoteDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            mapper = new Mapper(config);
            repository = new CaseNoteRepository(dbContext, userName);
            validator = new CaseNoteDtoValidator();
            this.dynamicEnvironment = dynamicEnvironment;
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
        public ActionResult<CaseNoteDto> Insert([FromBody] CaseNoteDto model)
        {
            if (!permissionValidation.Validate(new[]
                {
                    1
                }))
            {
                return Forbid();
            }

            var results = validator.Validate(model);

            if (!results.IsValid)
            {
                return Ok(repository.Insert(mapper.Map<CaseNote>(model)));
            }

            var jObject = JObject.Parse(model.Payload);

            var values = new Dictionary<string, string>();
            foreach (var (key, value) in jObject)
            {
                if (value != null)
                {
                    values.Add(key, value.ToString());
                }
            }

            var caseWorkflowActionRepository = new CaseWorkflowActionRepository(dbContext, userName);

            var caseWorkflowAction = caseWorkflowActionRepository.GetById(model.ActionId);

            if (caseWorkflowAction.EnableNotification != 1 && caseWorkflowAction.EnableHttpEndpoint != 1)
            {
                return Ok(repository.Insert(mapper.Map<CaseNote>(model)));
            }

            if (caseWorkflowAction.EnableNotification == 1)
            {
                var notification = new Notification(log, dynamicEnvironment);
                notification.Send(caseWorkflowAction.NotificationTypeId ?? 1,
                    caseWorkflowAction.NotificationDestination,
                    caseWorkflowAction.NotificationSubject,
                    caseWorkflowAction.NotificationBody, values);
            }

            if (caseWorkflowAction.EnableHttpEndpoint != 1)
            {
                return Ok(repository.Insert(mapper.Map<CaseNote>(model)));
            }

            var sendHttpEndpoint = new SendHttpEndpoint();
            if (caseWorkflowAction.HttpEndpointTypeId != null)
            {
                sendHttpEndpoint.Send(caseWorkflowAction.HttpEndpoint,
                    caseWorkflowAction.HttpEndpointTypeId.Value
                    , values);
            }

            return Ok(repository.Insert(mapper.Map<CaseNote>(model)));
        }

        [HttpGet("ByCaseKeyValue")]
        public ActionResult<List<CaseNoteDto>> GetByCaseKeyValue(string key, string value)
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

                return Ok(mapper.Map<List<CaseNote>>(repository.GetByCaseKeyValue(key, value)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
