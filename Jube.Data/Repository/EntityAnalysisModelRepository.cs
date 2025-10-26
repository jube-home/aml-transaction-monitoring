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

    public class EntityAnalysisModelRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public EntityAnalysisModelRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IEnumerable<EntityAnalysisModel> Get()
        {
            return dbContext.EntityAnalysisModel.Where(w =>
                (w.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && (w.Deleted == null || w.Deleted == 0));
        }

        public EntityAnalysisModel GetById(int id)
        {
            return dbContext.EntityAnalysisModel.FirstOrDefault(w
                => (w.TenantRegistryId == tenantRegistryId || !w.TenantRegistryId.HasValue)
                   && w.Id == id && (w.Deleted == null || w.Deleted == 0));
        }

        public EntityAnalysisModel Insert(EntityAnalysisModel model)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.TenantRegistryId = tenantRegistryId;
            model.Version = 1;
            model.CreatedDate = DateTime.Now;
            model.Id = dbContext.InsertWithInt32Identity(model);
            return model;
        }

        public EntityAnalysisModel Update(EntityAnalysisModel model)
        {
            var existing = dbContext.EntityAnalysisModel
                .FirstOrDefault(w => w.Id
                                     == model.Id
                                     && (w.TenantRegistryId == tenantRegistryId || !w.TenantRegistryId.HasValue)
                                     && (w.Deleted == 0 || w.Deleted == null)
                                     && (w.Locked == 0 || w.Locked == null));

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.TenantRegistryId = tenantRegistryId;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.Now;
            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;

            dbContext.Update(model);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<EntityAnalysisModel, EntityAnalysisModelVersion>();
            });
            var mapper = new Mapper(config);

            var audit = mapper.Map<EntityAnalysisModelVersion>(existing);
            audit.EntityAnalysisModelId = existing.Id;

            dbContext.Insert(audit);

            return model;
        }

        public void Delete(int id)
        {
            var records = dbContext.EntityAnalysisModel
                .Where(d => (d.TenantRegistryId == tenantRegistryId || !d.TenantRegistryId.HasValue)
                            && d.Id == id
                            && (d.Deleted == 0 || d.Deleted == null)
                            && (d.Locked == 0 || d.Locked == null))
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
            dbContext.EntityAnalysisModel
                .Where(d => d.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Update();
        }
    }
}
