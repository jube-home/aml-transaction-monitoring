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
    using Dto;
    using Dto.TreeChildren;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/TreeChildren")]
    [Authorize]
    public class EntityAnalysisModelTreeChildrenController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string userName;

        public EntityAnalysisModelTreeChildrenController(ILog log,
            IHttpContextAccessor httpContextAccessor, DynamicEnvironment dynamicEnvironment)
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

        [HttpGet]
        [Route("RequestXPath")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetRequestXPath(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        7
                    }))
                {
                    return Forbid();
                }

                var repository = new EntityAnalysisModelRequestXPathRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("VisualisationRegistryDatasource")]
        public ActionResult<List<VisualisationRegistryTreeChildDto>> GetVisualisationRegistryDatasource(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        33
                    }))
                {
                    return Forbid();
                }

                var repository = new VisualisationRegistryDatasourceRepository(dbContext, userName);
                return Ok(repository.GetByVisualisationRegistryIdOrderById(id)
                    .Select(entry => new VisualisationRegistryTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        VisualisationRegistryId = entry.VisualisationRegistryId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("UserRegistry")]
        public ActionResult<List<RoleRegistryTreeChildDto>> GetUserRegistry(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        35
                    }))
                {
                    return Forbid();
                }

                var repository = new UserRegistryRepository(dbContext, userName);
                return Ok(repository.GetByRoleRegistryId(id)
                    .Select(entry => new RoleRegistryTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        RoleRegistryId = entry.RoleRegistryId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("RoleRegistryPermission")]
        public ActionResult<List<RoleRegistryTreeChildDto>> GetRolePermissionRegistry(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        36
                    }))
                {
                    return Forbid();
                }

                var getRoleRegistryPermissionByRoleRegistryId =
                    new GetRoleRegistryPermissionByRoleRegistryIdQuery(dbContext, userName);

                return getRoleRegistryPermissionByRoleRegistryId.Execute(id).Select(s
                    => new RoleRegistryTreeChildDto
                    {
                        Key = s.Id,
                        Name = s.Name,
                        Color = s.Active ? "green" : "red",
                        RoleRegistryId = s.RoleRegistryId
                    }).ToList();
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("VisualisationRegistryParameter")]
        public ActionResult<List<VisualisationRegistryTreeChildDto>> GetVisualisationRegistryParameter(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        32
                    }))
                {
                    return Forbid();
                }

                var repository = new VisualisationRegistryParameterRepository(dbContext, userName);
                return Ok(repository.GetByVisualisationRegistryIdOrderById(id)
                    .Select(entry => new VisualisationRegistryTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        VisualisationRegistryId = entry.VisualisationRegistryId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("InlineFunction")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetInlineFunction(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        8
                    }))
                {
                    return Forbid();
                }

                var repository = new EntityAnalysisModelInlineFunctionRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("Tag")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetTag(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        37
                    }))
                {
                    return Forbid();
                }

                var repository = new EntityAnalysisModelTagRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("GatewayRule")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetGatewayRule(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        10
                    }))
                {
                    return Forbid();
                }

                var repository = new EntityAnalysisModelGatewayRuleRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("Exhaustive")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetExhaustive(int id)
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

                var repository = new ExhaustiveSearchInstanceRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("Reprocessing")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetReprocessing(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        26
                    }))
                {
                    return Forbid();
                }

                var repository = new EntityAnalysisModelReprocessingRuleRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelId(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("Adaptation")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetAdaptation(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        15
                    }))
                {
                    return Forbid();
                }

                var repository = new EntityAnalysisModelHttpAdaptationRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflow")]
        public ActionResult<List<CaseWorkflowDto>> GetCaseWorkflows(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        18, 19, 20, 21, 22, 23, 24, 25
                    }))
                {
                    return Forbid();
                }

                var repository = new CaseWorkflowRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("AbstractionCalculation")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetAbstractionCalculation(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        14
                    }))
                {
                    return Forbid();
                }

                var repository = new EntityAnalysisModelAbstractionCalculationRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderByIdDesc(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("AbstractionRule")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetAbstractionRule(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        13, 14
                    }))
                {
                    return Forbid();
                }

                var repository = new EntityAnalysisModelAbstractionRuleRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderByIdDesc(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("ActivationRule")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetActivationRule(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        17
                    }))
                {
                    return Forbid();
                }

                var repository = new EntityAnalysisModelActivationRuleRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderByIdDesc(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }


        [HttpGet]
        [Route("TTLCounter")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetTtlCounter(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        12
                    }))
                {
                    return Forbid();
                }

                var repository = new EntityAnalysisModelTtlCounterRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("InlineScript")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetInlineScript(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        9
                    }))
                {
                    return Forbid();
                }

                var repository = new EntityAnalysisModelInlineScriptRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("Sanctions")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetSanction(int id)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        11
                    }))
                {
                    return Forbid();
                }

                var repository = new EntityAnalysisModelSanctionRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelIdOrderById(id)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelId = entry.EntityAnalysisModelId
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("List")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetList(Guid guid)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        3
                    }))
                {
                    return Forbid();
                }

                var repository = new EntityAnalysisModelListRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelGuid(guid)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelGuid = entry.EntityAnalysisModelGuid
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("Dictionary")]
        public ActionResult<List<EntityAnalysisModelTreeChildDto>> GetDictionary(Guid guid)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        4
                    }))
                {
                    return Forbid();
                }

                var repository = new EntityAnalysisModelDictionaryRepository(dbContext, userName);
                return Ok(repository.GetByEntityAnalysisModelGuid(guid)
                    .Select(entry => new EntityAnalysisModelTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        EntityAnalysisModelGuid = entry.EntityAnalysisModelGuid
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflowXPath")]
        public ActionResult<List<CaseWorkflowStatusDto>> GetCaseWorkflowXPath(int key)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        20
                    }))
                {
                    return Forbid();
                }

                var repository = new CaseWorkflowXPathRepository(dbContext, userName);
                return Ok(repository.GetByCasesWorkflowIdOrderByIdDesc(key)
                    .Select(entry => new CasesWorkflowTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        CasesWorkflowId = entry.CaseWorkflowId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflowForm")]
        public ActionResult<List<CaseWorkflowStatusDto>> GetCaseWorkflowForm(int key)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        21
                    }))
                {
                    return Forbid();
                }

                var repository = new CaseWorkflowFormRepository(dbContext, userName);
                return Ok(repository.GetByCasesWorkflowIdOrderById(key)
                    .Select(entry => new CasesWorkflowTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        CasesWorkflowId = entry.CaseWorkflowId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflowAction")]
        public ActionResult<List<CaseWorkflowActionDto>> GetCaseWorkflowAction(int key)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        22
                    }))
                {
                    return Forbid();
                }

                var repository = new CaseWorkflowActionRepository(dbContext, userName);
                return Ok(repository.GetByCasesWorkflowIdOrderById(key)
                    .Select(entry => new CasesWorkflowTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        CasesWorkflowId = entry.CaseWorkflowId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflowMacro")]
        public ActionResult<List<CaseWorkflowMacroDto>> GetCaseWorkflowMacro(int key)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        24
                    }))
                {
                    return Forbid();
                }

                var repository = new CaseWorkflowMacroRepository(dbContext, userName);
                return Ok(repository.GetByCasesWorkflowIdOrderById(key)
                    .Select(entry => new CasesWorkflowTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        CasesWorkflowId = entry.CaseWorkflowId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflowFilter")]
        public ActionResult<List<CaseWorkflowFilterDto>> GetCaseWorkflowFilter(int key)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        25
                    }))
                {
                    return Forbid();
                }

                var repository = new CaseWorkflowFilterRepository(dbContext, userName);
                return Ok(repository.GetByCasesWorkflowIdOrderById(key)
                    .Select(entry => new CasesWorkflowTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        CasesWorkflowId = entry.CaseWorkflowId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflowDisplay")]
        public ActionResult<List<CaseWorkflowStatusDto>> GetCaseWorkflowDisplay(int key)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        23
                    }))
                {
                    return Forbid();
                }

                var repository = new CaseWorkflowDisplayRepository(dbContext, userName);
                return Ok(repository.GetByCasesWorkflowIdOrderById(key)
                    .Select(entry => new CasesWorkflowTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        CasesWorkflowId = entry.CaseWorkflowId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("CaseWorkflowStatus")]
        public ActionResult<List<CaseWorkflowStatusDto>> GetCaseWorkflowStatus(int key)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        19
                    }))
                {
                    return Forbid();
                }

                var repository = new CaseWorkflowStatusRepository(dbContext, userName);
                return Ok(repository.GetByCasesWorkflowIdOrderById(key)
                    .Select(entry => new CasesWorkflowTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        CasesWorkflowId = entry.CaseWorkflowId ?? 0
                    }).ToList());
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
