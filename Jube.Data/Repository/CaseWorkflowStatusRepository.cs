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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using Context;
    using LinqToDB;
    using Microsoft.Extensions.Logging.Abstractions;
    using Poco;

    public class CaseWorkflowStatusRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public CaseWorkflowStatusRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseWorkflowStatusRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public async Task<IEnumerable<CaseWorkflowStatus>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.CaseWorkflowStatus
                .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .ToListAsync(token);
        }

        public async Task<IEnumerable<CaseWorkflowStatus>> GetByCasesWorkflowIdActiveOnlyAsync(int casesWorkflowId, CancellationToken token = default)
        {
            return await dbContext.CaseWorkflowStatus
                .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (w.CaseWorkflow.EntityAnalysisModel.Deleted == 0 || w.CaseWorkflow.EntityAnalysisModel.Deleted == null)// Added
                            && (w.CaseWorkflow.Deleted == 0 || w.CaseWorkflow.Deleted == null)
                            && w.Active == 1
                            && w.CaseWorkflowId == casesWorkflowId
                            && dbContext.CaseWorkflowStatusRole
                                .Where(r => r.CaseWorkflowStatusGuid == w.Guid
                                            && (r.Deleted == 0 || r.Deleted == null))
                                .Any(r => dbContext.RoleRegistry
                                    .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                                    .Any(rr => dbContext.UserRegistry
                                        .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                            && dbContext.CaseWorkflowRole
                                .Where(r => r.CaseWorkflowGuid == w.CaseWorkflow.Guid
                                            && (r.Deleted == 0 || r.Deleted == null))
                                .Any(r => dbContext.RoleRegistry
                                    .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                                    .Any(rr => dbContext.UserRegistry
                                        .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                            && (w.Deleted == 0 || w.Deleted == null)
                ).ToListAsync(token);
        }

        public async Task<IEnumerable<CaseWorkflowStatus>> GetByCasesWorkflowGuidActiveOnlyAsync(Guid casesWorkflowGuid, CancellationToken token = default)
        {
            return await dbContext.CaseWorkflowStatus
                .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (w.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                                w.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                            && (w.CaseWorkflow.Deleted == 0 || w.CaseWorkflow.Deleted == null)
                            && w.Active == 1
                            && w.CaseWorkflow.Guid == casesWorkflowGuid
                            && (w.Deleted == 0 || w.Deleted == null)
                            && dbContext.CaseWorkflowStatusRole
                                .Where(r => r.CaseWorkflowStatusGuid == w.Guid
                                            && (r.Deleted == 0 || r.Deleted == null))
                                .Any(r => dbContext.RoleRegistry
                                    .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                                    .Any(rr => dbContext.UserRegistry
                                        .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                            && dbContext.CaseWorkflowRole
                                .Where(r => r.CaseWorkflowGuid == w.CaseWorkflow.Guid
                                            && (r.Deleted == 0 || r.Deleted == null))
                                .Any(r => dbContext.RoleRegistry
                                    .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                                    .Any(rr => dbContext.UserRegistry
                                        .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                ).ToListAsync(token);
        }

        public async Task<IEnumerable<CaseWorkflowStatus>> GetByCasesWorkflowIdOrderByIdAsync(int casesWorkflowId, CancellationToken token = default)
        {
            return await dbContext.CaseWorkflowStatus
                .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.CaseWorkflowId == casesWorkflowId && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public async Task<IEnumerable<CaseWorkflowStatus>> GetByCasesWorkflowIdOrderByPriorityAsync(int casesWorkflowId, CancellationToken token = default)
        {
            return await dbContext.CaseWorkflowStatus
                .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.CaseWorkflowId == casesWorkflowId && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Priority).ToListAsync(token);
        }

        public async Task<IEnumerable<CaseWorkflowStatus>> GetByCasesWorkflowGuidAsync(Guid casesWorkflowGuid, CancellationToken token = default)
        {
            return await dbContext.CaseWorkflowStatus
                .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.CaseWorkflow.Guid == casesWorkflowGuid
                            && (w.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                                w.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                            && (w.Deleted == 0 || w.Deleted == null)).ToListAsync(token);
        }

        public Task<CaseWorkflowStatus> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.CaseWorkflowStatus.FirstOrDefaultAsync(w =>
                w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && w.Id == id && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<CaseWorkflowStatus> GetByGuidAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.CaseWorkflowStatus.FirstOrDefaultAsync(w =>
                w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && w.Guid == guid
                && (w.CaseWorkflow.EntityAnalysisModel.Deleted == 0 || w.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public Task<CaseWorkflowStatus> GetByNameCaseWorkflowIdAsync(string name, int caseWorkflowId, CancellationToken token = default)
        {
            return dbContext.CaseWorkflowStatus.FirstOrDefaultAsync(w =>
                w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                & w.CaseWorkflowId == caseWorkflowId
                && w.Name.ToLower() == name.ToLower()
                && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<CaseWorkflowStatus> InsertAsync(CaseWorkflowStatus model, CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.Now;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public async Task<CaseWorkflowStatus> UpdateAsync(CaseWorkflowStatus model, CancellationToken token = default)
        {
            var existing = await dbContext.CaseWorkflowStatus
                .FirstOrDefaultAsync(w => w.Id
                                          == model.Id
                                          && w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId ==
                                          tenantRegistryId
                                          && (w.Deleted == 0 || w.Deleted == null)
                                          && (w.Locked == 0 || w.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.Now;

            await dbContext.UpdateAsync(model, token: token);

            var mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CaseWorkflowStatus, CaseWorkflowStatusVersion>();
            }, NullLoggerFactory.Instance));

            var audit = mapper.Map<CaseWorkflowStatusVersion>(existing);
            audit.CaseWorkflowStatusId = existing.Id;

            await dbContext.InsertAsync(audit, token: token);

            return model;
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.CaseWorkflowStatus
                .Where(d => d.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && d.Id == id
                            && (d.Locked == 0 || d.Locked == null)
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId, CancellationToken token = default)
        {
            return dbContext.CaseWorkflowStatus
                .Where(d => d.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .UpdateAsync(token);
        }
    }
}
