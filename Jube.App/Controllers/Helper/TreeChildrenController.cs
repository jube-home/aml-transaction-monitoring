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
        [Route("RequestXPath")]
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetEntityAnalysisModelRequestXPathsAsync(int id, CancellationToken token = default)
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
                var entityAnalysisModelRequestXPaths = await repository.GetByEntityAnalysisModelIdOrderByIdAsync(id, token).ConfigureAwait(false);

                return Ok(entityAnalysisModelRequestXPaths.Select(entry => new EntityAnalysisModelTreeChildDto
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
        public async Task<ActionResult<List<VisualisationRegistryTreeChildDto>>> GetVisualisationRegistryDatasourceAsync(int id, CancellationToken token = default)
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
                return Ok((await repository.GetByVisualisationRegistryIdOrderByPriorityAsync(id, token))
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
        public async Task<ActionResult<List<RoleRegistryTreeChildDto>>> GetUserRegistryAsync(Guid guid, CancellationToken token = default)
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
                return Ok((await repository.GetByRoleRegistryGuidAsync(guid, token))
                    .Select(entry => new RoleRegistryTreeChildDto
                    {
                        Color = entry.Active == 1 ? "green" : "red",
                        Key = entry.Id,
                        Name = entry.Name,
                        RoleRegistryGuid = entry.RoleRegistryGuid
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
        public async Task<ActionResult<List<RoleRegistryTreeChildDto>>> GetRolePermissionRegistryAsync(int id, CancellationToken token = default)
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

                var getRoleRegistryPermissionByRoleRegistryId = new GetRoleRegistryPermissionByRoleRegistryGuidQuery(dbContext, userName);

                return (await getRoleRegistryPermissionByRoleRegistryId.ExecuteAsync(id, token)).Select(s
                    => new RoleRegistryTreeChildDto
                    {
                        Key = s.Id,
                        Name = s.Name,
                        Color = s.Active ? "green" : "red",
                        RoleRegistryGuid = s.RoleRegistryGuid
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
        public async Task<ActionResult<List<VisualisationRegistryTreeChildDto>>> GetVisualisationRegistryParameterAsync(int id, CancellationToken token = default)
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
                return Ok((await repository.GetByVisualisationRegistryIdOrderByIdAsync(id, token))
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
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetEntityAnalysisModelInlineFunctionsAsync(int id, CancellationToken token = default)
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
                var entityAnalysisModelInlineFunctions = await repository.GetByEntityAnalysisModelIdOrderByIdAsync(id, token).ConfigureAwait(false);

                return Ok(entityAnalysisModelInlineFunctions.Select(entry => new EntityAnalysisModelTreeChildDto
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
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetEntityAnalysisModelTagsAsync(int id, CancellationToken token = default)
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
                var entityAnalysisModelTags = await repository.GetByEntityAnalysisModelIdOrderByIdAsync(id, token).ConfigureAwait(false);

                return Ok(entityAnalysisModelTags.Select(entry => new EntityAnalysisModelTreeChildDto
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
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetEntityAnalysisModelGatewayRulesAsync(int id, CancellationToken token = default)
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
                var entityAnalysisModelGatewayRules = await repository.GetByEntityAnalysisModelIdOrderByPriorityAsync(id, token).ConfigureAwait(false);

                return Ok(entityAnalysisModelGatewayRules.Select(entry => new EntityAnalysisModelTreeChildDto
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
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetEntityAnalysisModelExhaustiveSearchInstancesAsync(int id, CancellationToken token = default)
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
                var entityAnalysisModelExhaustiveSearchInstances = await repository.GetByEntityAnalysisModelIdOrderByIdAsync(id, token).ConfigureAwait(false);

                return Ok(
                    entityAnalysisModelExhaustiveSearchInstances.Select(entry => new EntityAnalysisModelTreeChildDto
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
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetReprocessingAsync(int id, CancellationToken token = default)
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
                return Ok((await repository.GetByEntityAnalysisModelIdAsync(id, token))
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
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetEntityAnalysisModelAdaptationsAsync(int id, CancellationToken token = default)
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
                var entityAnalysisModelAdaptations = await repository.GetByEntityAnalysisModelIdOrderByIdAsync(id, token).ConfigureAwait(false);

                return Ok(entityAnalysisModelAdaptations.Select(entry => new EntityAnalysisModelTreeChildDto
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
        public async Task<ActionResult<List<CaseWorkflowDto>>> GetCaseWorkflowsAsync(int id, CancellationToken token = default)
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
                return Ok((await repository.GetByEntityAnalysisModelIdOrderByIdAsync(id, token))
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
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetEntityAnalysisModelAbstractionCalculationsAsync(int id, CancellationToken token = default)
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
                var entityAnalysisModelAbstractionCalculations = await repository.GetByEntityAnalysisModelIdOrderByIdDescAsync(id, token).ConfigureAwait(false);

                return Ok(entityAnalysisModelAbstractionCalculations.Select(entry => new EntityAnalysisModelTreeChildDto
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
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetEntityAnalysisModelAbstractionRulesAsync(int id, CancellationToken token = default)
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
                var entityAnalysisModelAbstractionRules = await repository.GetByEntityAnalysisModelIdOrderByIdDescAsync(id, token).ConfigureAwait(false);

                return Ok(entityAnalysisModelAbstractionRules.Select(entry => new EntityAnalysisModelTreeChildDto
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
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetActivationRuleAsync(int id, CancellationToken token = default)
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
                return Ok((await repository.GetByEntityAnalysisModelIdOrderByPriorityDescAsync(id, token))
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
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetEntityAnalysisModelTtlCountersAsync(int id, CancellationToken token = default)
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
                var entityAnalysisModelTtlCounters = await repository.GetByEntityAnalysisModelIdOrderByIdAsync(id, token).ConfigureAwait(false);

                return Ok(entityAnalysisModelTtlCounters.Select(entry => new EntityAnalysisModelTreeChildDto
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
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetEntityAnalysisModelInlineScriptsAsync(int id, CancellationToken token = default)
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
                var entityAnalysisModelInlineScripts = await repository.GetByEntityAnalysisModelIdOrderByIdAsync(id, token).ConfigureAwait(false);

                return Ok(entityAnalysisModelInlineScripts.Select(entry => new EntityAnalysisModelTreeChildDto
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
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetEntityAnalysisModelSanctionsAsync(int id, CancellationToken token = default)
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
                var entityAnalysisModelSanctions = await repository.GetByEntityAnalysisModelIdOrderByIdAsync(id, token).ConfigureAwait(false);

                return Ok(entityAnalysisModelSanctions.Select(entry => new EntityAnalysisModelTreeChildDto
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
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetListAsync(Guid guid, CancellationToken token = default)
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
                return Ok((await repository.GetByEntityAnalysisModelGuidAsync(guid, token))
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
        public async Task<ActionResult<List<EntityAnalysisModelTreeChildDto>>> GetDictionaryAsync(Guid guid, CancellationToken token = default)
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
                return Ok((await repository.GetByEntityAnalysisModelGuidAsync(guid, token))
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
        public async Task<ActionResult<List<CaseWorkflowXPathDto>>> GetCaseWorkflowXPathAsync(int key, CancellationToken token = default)
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
                return Ok((await repository.GetByCasesWorkflowIdOrderByIdDescAsync(key, token))
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
        public async Task<ActionResult<List<CaseWorkflowFormDto>>> GetCaseWorkflowFormAsync(int key, CancellationToken token = default)
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
                return Ok((await repository.GetByCasesWorkflowIdOrderByIdAsync(key, token))
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
        public async Task<ActionResult<List<CaseWorkflowActionDto>>> GetCaseWorkflowActionAsync(int key, CancellationToken token = default)
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
                return Ok((await repository.GetByCasesWorkflowIdOrderByIdAsync(key, token))
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
        public async Task<ActionResult<List<CaseWorkflowMacroDto>>> GetCaseWorkflowMacroAsync(int key, CancellationToken token = default)
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
                return Ok((await repository.GetByCasesWorkflowIdOrderByIdAsync(key, token))
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
        public async Task<ActionResult<List<CaseWorkflowFilterDto>>> GetCaseWorkflowFilterAsync(int key, CancellationToken token = default)
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
                return Ok((await repository.GetByCasesWorkflowIdOrderByIdAsync(key, token))
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
        public async Task<ActionResult<List<CaseWorkflowDisplayDto>>> GetCaseWorkflowDisplayAsync(int key, CancellationToken token = default)
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
                return Ok((await repository.GetByCasesWorkflowIdOrderByIdAsync(key, token))
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
        public async Task<ActionResult<List<CaseWorkflowStatusDto>>> GetCaseWorkflowStatusAsync(int key, CancellationToken token = default)
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
                return Ok((await repository.GetByCasesWorkflowIdOrderByPriorityAsync(key, token))
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
