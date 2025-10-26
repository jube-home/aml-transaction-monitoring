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
    using System.Net;
    using AutoMapper;
    using Code;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Dto;
    using DynamicEnvironment;
    using FluentValidation;
    using FluentValidation.Results;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;
    using Validators;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CaseWorkflowFormEntryController : Controller
    {
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseWorkflowFormEntryRepository repository;
        private readonly string userName;
        private readonly IValidator<CaseWorkflowFormEntryDto> validator;

        public CaseWorkflowFormEntryController(ILog log,
            DynamicEnvironment dynamicEnvironment, IHttpContextAccessor httpContextAccessor)
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
                cfg.CreateMap<CaseWorkflowFormEntry, CaseWorkflowFormEntryDto>();
                cfg.CreateMap<CaseWorkflowFormEntryDto, CaseWorkflowFormEntry>();
                cfg.CreateMap<List<CaseWorkflowFormEntry>, List<CaseWorkflowFormEntryDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            mapper = new Mapper(config);
            repository = new CaseWorkflowFormEntryRepository(dbContext, userName);
            validator = new CaseWorkflowFormEntryDtoValidator();
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
        [ProducesResponseType(typeof(CaseWorkflowFormEntryDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<CaseWorkflowFormEntryDto> Create([FromBody] CaseWorkflowFormEntryDto model)
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

                var results = validator.Validate(model);

                if (!results.IsValid)
                {
                    return BadRequest(results);
                }

                var entry = repository.Insert(mapper.Map<CaseWorkflowFormEntry>(model));

                if (model.Payload == null)
                {
                    return Ok(entry);
                }

                var repositoryCaseWorkflowFormEntryValue =
                    new CaseWorkflowFormEntryValueRepository(dbContext, userName);

                var jObject = JObject.Parse(model.Payload);

                var values = new Dictionary<string, string>();
                foreach (var (key, value) in jObject)
                {
                    if (value == null)
                    {
                        continue;
                    }

                    values.Add(key, value.ToString());

                    if (key == "CaseKey")
                    {
                        continue;
                    }

                    var caseWorkflowFormEntryValue = new CaseWorkflowFormEntryValue
                    {
                        CaseWorkflowFormEntryId = entry.Id,
                        Name = key,
                        Value = value.ToString()
                    };

                    repositoryCaseWorkflowFormEntryValue.Insert(caseWorkflowFormEntryValue);
                }

                var caseWorkflowFormRepository = new CaseWorkflowFormRepository(dbContext, userName);

                var caseWorkflowForm = caseWorkflowFormRepository.GetById(model.CaseWorkflowFormId);

                if (caseWorkflowForm.EnableNotification != 1 && caseWorkflowForm.EnableHttpEndpoint != 1)
                {
                    return Ok(entry);
                }

                if (caseWorkflowForm.EnableNotification == 1)
                {
                    var notification = new Notification(log, dynamicEnvironment);
                    notification.Send(caseWorkflowForm.NotificationTypeId ?? 1,
                        caseWorkflowForm.NotificationDestination,
                        caseWorkflowForm.NotificationSubject,
                        caseWorkflowForm.NotificationBody, values);
                }

                if (caseWorkflowForm.EnableHttpEndpoint != 1)
                {
                    return Ok(entry);
                }

                var sendHttpEndpoint = new SendHttpEndpoint();
                if (caseWorkflowForm.HttpEndpointTypeId != null)
                {
                    sendHttpEndpoint.Send(caseWorkflowForm.HttpEndpoint,
                        caseWorkflowForm.HttpEndpointTypeId.Value
                        , values);
                }

                return Ok(entry);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet("ByCaseKeyValue")]
        public ActionResult<List<CaseWorkflowFormEntryDto>> GetByCaseKeyValue(string key, string value)
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

                return Ok(mapper.Map<List<CaseWorkflowFormEntry>>(repository.GetByCaseKeyValue(key, value)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
