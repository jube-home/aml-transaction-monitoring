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

    public class EntityAnalysisModelHttpAdaptationRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelHttpAdaptationRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelHttpAdaptationRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public EntityAnalysisModelHttpAdaptationRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IEnumerable<EntityAnalysisModelHttpAdaptation> Get()
        {
            return dbContext.EntityAnalysisModelHttpAdaptation
                .Where(w =>
                    w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue
                );
        }

        public IEnumerable<EntityAnalysisModelHttpAdaptation> GetByEntityAnalysisModelIdOrderById(int entityAnalysisModelId)
        {
            return dbContext.EntityAnalysisModelHttpAdaptation
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id);
        }

        public EntityAnalysisModelHttpAdaptation GetById(int id)
        {
            return dbContext.EntityAnalysisModelHttpAdaptation.FirstOrDefault(w =>
                (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Id == id && (w.Deleted == 0 || w.Deleted == null));
        }

        public EntityAnalysisModelHttpAdaptation Insert(EntityAnalysisModelHttpAdaptation model)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.Now;
            model.Version = 1;
            model.Id = dbContext.InsertWithInt32Identity(model);
            return model;
        }

        public EntityAnalysisModelHttpAdaptation Update(EntityAnalysisModelHttpAdaptation model)
        {
            var existing = dbContext.EntityAnalysisModelHttpAdaptation
                .FirstOrDefault(w => w.Id
                                     == model.Id
                                     && (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                                         !tenantRegistryId.HasValue)
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

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<EntityAnalysisModelHttpAdaptation, EntityAnalysisModelHttpAdaptationVersion>();
            });
            var mapper = new Mapper(config);

            var audit = mapper.Map<EntityAnalysisModelHttpAdaptationVersion>(existing);
            audit.EntityAnalysisModelHttpAdaptationId = existing.Id;

            dbContext.Insert(audit);

            return model;
        }

        public void Delete(int id)
        {
            var records = dbContext.EntityAnalysisModelHttpAdaptation
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
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
            dbContext.EntityAnalysisModelHttpAdaptation
                .Where(d =>
                    d.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Update();
        }
    }
}
