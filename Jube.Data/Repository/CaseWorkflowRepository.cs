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
    using AutoMapper;
    using Context;
    using LinqToDB;
    using Poco;

    public class CaseWorkflowRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public CaseWorkflowRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseWorkflowRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public IEnumerable<CaseWorkflow> Get()
        {
            return dbContext.CaseWorkflow
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId);
        }

        public IEnumerable<CaseWorkflow> GetByEntityAnalysisModelIdOrderById(int entityAnalysisModelId)
        {
            return dbContext.CaseWorkflow
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.EntityAnalysisModelId == entityAnalysisModelId &&
                            (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id);
        }

        public IEnumerable<CaseWorkflow> GetByEntityAnalysisModelIdActiveOnly(int entityAnalysisModelId)
        {
            return dbContext.CaseWorkflow
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.Active == 1
                            && w.EntityAnalysisModelId == entityAnalysisModelId
                            && (w.Deleted == 0 || w.Deleted == null));
        }

        public IEnumerable<CaseWorkflow> GetByEntityAnalysisModelGuidActiveOnly(Guid entityAnalysisModelGuid)
        {
            return dbContext.CaseWorkflow
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.Active == 1
                            && w.EntityAnalysisModel.Guid == entityAnalysisModelGuid
                            && (w.EntityAnalysisModel.Deleted == 0 || w.EntityAnalysisModel.Deleted == null)
                            && (w.Deleted == 0 || w.Deleted == null));
        }

        public CaseWorkflow GetById(int id)
        {
            return dbContext.CaseWorkflow.FirstOrDefault(w =>
                w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && w.Id == id && (w.Deleted == 0 || w.Deleted == null));
        }

        public CaseWorkflow GetByIdIncludingDeleted(int id)
        {
            return dbContext.CaseWorkflow.FirstOrDefault(w =>
                w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && w.Id == id);
        }

        public CaseWorkflow GetByGuidIncludingDeleted(Guid guid)
        {
            return dbContext.CaseWorkflow.FirstOrDefault(w =>
                w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && (w.EntityAnalysisModel.Deleted == 0 || w.EntityAnalysisModel.Deleted == null)
                && w.Guid == guid);
        }

        public CaseWorkflow Insert(CaseWorkflow model)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.Now;
            model.Version = 1;
            model.Id = dbContext.InsertWithInt32Identity(model);
            return model;
        }

        public CaseWorkflow Update(CaseWorkflow model)
        {
            var existing = dbContext.CaseWorkflow
                .FirstOrDefault(w => w.Id
                                     == model.Id
                                     && w.EntityAnalysisModel.TenantRegistryId ==
                                     tenantRegistryId
                                     && (w.Deleted == 0 || w.Deleted == null)
                                     && (w.Locked == 0 || w.Locked == null));

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.Now;

            dbContext.Update(model);

            var config = new MapperConfiguration(cfg => { cfg.CreateMap<CaseWorkflow, CaseWorkflowVersion>(); });
            var mapper = new Mapper(config);

            var audit = mapper.Map<CaseWorkflowVersion>(existing);
            audit.CaseWorkflowId = existing.Id;

            dbContext.Insert(audit);

            return model;
        }

        public void Delete(int id)
        {
            var records = dbContext.CaseWorkflow
                .Where(d => d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && d.Id == id
                            && (d.Locked == 0 || d.Locked == null)
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Set(s => s.DeletedUser, userName)
                .Update();

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public void DeleteByTenantRegistryIdOutsideOfInstance(int tenantRegistryIdOutsideOfInstance, int importId)
        {
            dbContext.CaseWorkflow
                .Where(d => d.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Update();
        }
    }
}
