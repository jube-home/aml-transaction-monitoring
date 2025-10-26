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
    using System.Globalization;
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
    public class CaseController : Controller
    {
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseRepository repositoryCase;
        private readonly CaseEventRepository repositoryCaseEvent;
        private readonly string userName;
        private readonly IValidator<CaseDto> validator;

        public CaseController(ILog log, DynamicEnvironment dynamicEnvironment
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
                cfg.CreateMap<Case, CaseDto>();
                cfg.CreateMap<CaseDto, Case>();
                cfg.CreateMap<List<Case>, List<CaseDto>>()
                    .ForMember("Item", opt => opt.Ignore());
            });
            mapper = new Mapper(config);
            repositoryCase = new CaseRepository(dbContext, userName);
            repositoryCaseEvent = new CaseEventRepository(dbContext, userName);
            validator = new CaseDtoValidator();
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

        [HttpGet]
        public ActionResult<List<CaseDto>> Get()
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

                return Ok(mapper.Map<List<CaseDto>>(repositoryCase.Get()));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("{id:int}")]
        public ActionResult<CaseDto> GetByCaseId(int id)
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

                return Ok(mapper.Map<CaseDto>(
                    repositoryCase.GetById(id)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(CaseDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<CaseDto> CreateCase([FromBody] CaseDto model)
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
                if (results.IsValid)
                {
                    return Ok(repositoryCase.Insert(mapper.Map<Case>(model)));
                }

                return BadRequest(results);
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpPut]
        [ProducesResponseType(typeof(CaseDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<CaseDto> UpdateCase([FromBody] CaseDto model)
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

                var existing = repositoryCase.GetById(model.Id);

                var caseEvents = new List<CaseEvent>();

                if (existing.ClosedStatusId is 0 or 1 or 2)
                {
                    switch (model.ClosedStatusId)
                    {
                        case 3:
                            existing.ClosedUser = userName;
                            existing.ClosedDate = DateTime.Now;

                            caseEvents.Add(new CaseEvent
                                {
                                    CaseEventTypeId = 5,
                                    CaseId = existing.Id,
                                    CaseKey = existing.CaseKey,
                                    CaseKeyValue = existing.CaseKeyValue,
                                    After = model.ClosedDate.ToString(CultureInfo.InvariantCulture),
                                    CreatedDate = existing.ClosedDate,
                                    CreatedUser = userName
                                }
                            );
                            break;
                        case 2:
                            caseEvents.Add(new CaseEvent
                                {
                                    CaseEventTypeId = 12,
                                    CaseId = existing.Id,
                                    CaseKey = existing.CaseKey,
                                    CaseKeyValue = existing.CaseKeyValue,
                                    Before = existing.ClosedDate.ToString(),
                                    After = model.ClosedDate.ToString(CultureInfo.InvariantCulture),
                                    CreatedDate = DateTime.Now,
                                    CreatedUser = userName
                                }
                            );
                            break;
                        case 1:
                            caseEvents.Add(new CaseEvent
                                {
                                    CaseEventTypeId = 13,
                                    CaseId = existing.Id,
                                    CaseKey = existing.CaseKey,
                                    CaseKeyValue = existing.CaseKeyValue,
                                    Before = existing.ClosedDate.ToString(),
                                    After = model.ClosedDate.ToString(CultureInfo.InvariantCulture),
                                    CreatedDate = DateTime.Now,
                                    CreatedUser = userName
                                }
                            );
                            break;
                    }
                }
                else
                {
                    switch (model.ClosedStatusId)
                    {
                        case 2:
                            caseEvents.Add(new CaseEvent
                                {
                                    CaseEventTypeId = 12,
                                    CaseId = existing.Id,
                                    CaseKey = existing.CaseKey,
                                    CaseKeyValue = existing.CaseKeyValue,
                                    Before = existing.ClosedDate.ToString(),
                                    After = model.ClosedDate.ToString(CultureInfo.InvariantCulture),
                                    CreatedDate = DateTime.Now,
                                    CreatedUser = userName
                                }
                            );
                            break;
                        case 1:
                            caseEvents.Add(new CaseEvent
                                {
                                    CaseEventTypeId = 13,
                                    CaseId = existing.Id,
                                    CaseKey = existing.CaseKey,
                                    CaseKeyValue = existing.CaseKeyValue,
                                    Before = existing.ClosedDate.ToString(),
                                    After = model.ClosedDate.ToString(CultureInfo.InvariantCulture),
                                    CreatedDate = DateTime.Now,
                                    CreatedUser = userName
                                }
                            );
                            break;
                    }
                }

                existing.ClosedStatusId = model.ClosedStatusId;

                if (existing.LockedUser != model.LockedUser)
                {
                    caseEvents.Add(new CaseEvent
                        {
                            CaseEventTypeId = 14,
                            CaseId = existing.Id,
                            CaseKey = existing.CaseKey,
                            CaseKeyValue = existing.CaseKeyValue,
                            Before = existing.LockedUser,
                            After = model.LockedUser,
                            CreatedDate = DateTime.Now,
                            CreatedUser = userName
                        }
                    );
                }

                existing.LockedUser = model.LockedUser;

                if (existing.Locked is 0 or null)
                {
                    if (existing.Locked == 1)
                    {
                        caseEvents.Add(new CaseEvent
                            {
                                CaseEventTypeId = 6,
                                CaseId = existing.Id,
                                CaseKey = existing.CaseKey,
                                CaseKeyValue = existing.CaseKeyValue,
                                CreatedDate = DateTime.Now,
                                CreatedUser = userName
                            }
                        );

                        existing.LockedDate = DateTime.Now;
                        existing.LockedUser = userName;
                    }
                    else
                    {
                        existing.LockedDate = null;
                    }
                }

                existing.Locked = (byte)(model.Locked ? 1 : 0);

                if (existing.DiaryDate != model.DiaryDate)
                {
                    caseEvents.Add(new CaseEvent
                        {
                            CaseEventTypeId = 10,
                            CaseId = existing.Id,
                            CaseKey = existing.CaseKey,
                            CaseKeyValue = existing.LockedUser,
                            Before = existing.DiaryDate.ToString(),
                            After = model.DiaryDate.ToString(CultureInfo.InvariantCulture),
                            CreatedDate = DateTime.Now,
                            CreatedUser = userName
                        }
                    );
                }

                existing.DiaryDate = model.DiaryDate;

                if (existing.Diary is 0 or null)
                {
                    if (model.Diary)
                    {
                        caseEvents.Add(new CaseEvent
                            {
                                CaseEventTypeId = 7,
                                CaseId = existing.Id,
                                CaseKey = existing.CaseKey,
                                CaseKeyValue = existing.LockedUser,
                                CreatedDate = DateTime.Now,
                                CreatedUser = userName
                            }
                        );

                        existing.DiaryUser = userName;
                    }
                }

                existing.Diary = (byte)(model.Diary ? 1 : 0);

                if (existing.CaseWorkflowStatusGuid != model.CaseWorkflowStatusGuid)
                {
                    caseEvents.Add(new CaseEvent
                        {
                            CaseEventTypeId = 9,
                            CaseId = existing.Id,
                            CaseKey = existing.CaseKey,
                            CaseKeyValue = existing.LockedUser,
                            Before = existing.CaseWorkflowStatusGuid.ToString(),
                            After = model.CaseWorkflowStatusGuid.ToString(),
                            CreatedDate = DateTime.Now,
                            CreatedUser = userName
                        }
                    );

                    if (model.Payload != null)
                    {
                        var jObject = JObject.Parse(model.Payload);

                        var values = new Dictionary<string, string>();
                        foreach (var (key, value) in jObject)
                        {
                            if (value != null)
                            {
                                values.Add(key, value.ToString());
                            }
                        }

                        var caseWorkflowStatusRepository =
                            new CaseWorkflowStatusRepository(dbContext, userName);

                        var caseWorkflowStatus =
                            caseWorkflowStatusRepository.GetByGuid(model.CaseWorkflowStatusGuid);

                        if (caseWorkflowStatus.EnableNotification == 1 ||
                            caseWorkflowStatus.EnableHttpEndpoint == 1)
                        {
                            if (caseWorkflowStatus.EnableNotification == 1)
                            {
                                var notification = new Notification(log, dynamicEnvironment);
                                notification.Send(caseWorkflowStatus.NotificationTypeId ?? 1,
                                    caseWorkflowStatus.NotificationDestination,
                                    caseWorkflowStatus.NotificationSubject,
                                    caseWorkflowStatus.NotificationBody, values);
                            }

                            if (caseWorkflowStatus.EnableHttpEndpoint == 1)
                            {
                                var sendHttpEndpoint = new SendHttpEndpoint();
                                if (caseWorkflowStatus.HttpEndpointTypeId != null)
                                {
                                    sendHttpEndpoint.Send(caseWorkflowStatus.HttpEndpoint,
                                        caseWorkflowStatus.HttpEndpointTypeId.Value
                                        , values);
                                }
                            }
                        }
                    }
                }

                existing.CaseWorkflowStatusGuid = model.CaseWorkflowStatusGuid;

                if (existing.Rating != model.Rating)
                {
                    caseEvents.Add(new CaseEvent
                        {
                            CaseEventTypeId = 11,
                            CaseId = existing.Id,
                            CaseKey = existing.CaseKey,
                            CaseKeyValue = existing.LockedUser,
                            Before = existing.Rating.ToString(),
                            After = model.Rating.ToString(),
                            CreatedDate = DateTime.Now,
                            CreatedUser = userName
                        }
                    );
                }

                existing.Rating = model.Rating;

                existing = repositoryCase.Update(existing);

                repositoryCaseEvent.BulkInsert(caseEvents);

                return Ok(existing);
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
    }
}
