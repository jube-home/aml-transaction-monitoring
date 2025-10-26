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

    public class CaseWorkflowXPathRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public CaseWorkflowXPathRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseWorkflowXPathRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public IEnumerable<CaseWorkflowXPath> Get()
        {
            return dbContext.CaseWorkflowXPath
                .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId);
        }

        public IEnumerable<CaseWorkflowXPath> GetByCasesWorkflowIdActiveDrillOnly(int casesWorkflowId)
        {
            return dbContext.CaseWorkflowXPath
                .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.Active == 1
                            && w.Drill == 1
                            && w.CaseWorkflowId == casesWorkflowId
                            && (w.Deleted == 0 || w.Deleted == null));
        }

        public IEnumerable<CaseWorkflowXPath> GetByCasesWorkflowGuidActiveDrillOnly(Guid casesWorkflowGuid)
        {
            return dbContext.CaseWorkflowXPath
                .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.Active == 1
                            && w.Drill == 1
                            && w.CaseWorkflow.Guid == casesWorkflowGuid
                            && (w.Deleted == 0 || w.Deleted == null));
        }

        public IEnumerable<CaseWorkflowXPath> GetByCasesWorkflowIdOrderByIdDesc(int casesWorkflowId)
        {
            return dbContext.CaseWorkflowXPath
                .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.CaseWorkflowId == casesWorkflowId && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id);
        }

        public CaseWorkflowXPath GetById(int id)
        {
            return dbContext.CaseWorkflowXPath.FirstOrDefault(w =>
                w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && w.Id == id && (w.Deleted == 0 || w.Deleted == null));
        }

        public CaseWorkflowXPath Insert(CaseWorkflowXPath model)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.Now;
            model.Version = 1;
            model.Guid = Guid.NewGuid();
            model.Id = dbContext.InsertWithInt32Identity(model);
            return model;
        }

        public CaseWorkflowXPath Update(CaseWorkflowXPath model)
        {
            var existing = dbContext.CaseWorkflowXPath
                .FirstOrDefault(w => w.Id
                                     == model.Id
                                     && w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId ==
                                     tenantRegistryId
                                     && (w.Deleted == 0 || w.Deleted == null)
                                     && (w.Locked == 0 || w.Locked == null));

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.Now;

            dbContext.Update(model);

            var config = new MapperConfiguration(cfg => { cfg.CreateMap<CaseWorkflowXPath, CaseWorkflowXPathVersion>(); });
            var mapper = new Mapper(config);

            var audit = mapper.Map<CaseWorkflowXPathVersion>(existing);
            audit.CaseWorkflowXPathId = existing.Id;

            dbContext.Insert(audit);

            return model;
        }

        public void Delete(int id)
        {
            var records = dbContext.CaseWorkflowXPath
                .Where(d => d.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
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
            dbContext.CaseWorkflowXPath
                .Where(d => d.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Update();
        }
    }
}
