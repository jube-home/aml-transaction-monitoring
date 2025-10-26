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

    public class VisualisationRegistryRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public VisualisationRegistryRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public VisualisationRegistryRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public IEnumerable<VisualisationRegistry> GetOrderById()
        {
            return dbContext.VisualisationRegistry.Where(w =>
                    w.TenantRegistryId == tenantRegistryId
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id);
        }

        public VisualisationRegistry GetByGuid(Guid guid)
        {
            return dbContext.VisualisationRegistry.FirstOrDefault(w => w.Guid == guid
                                                                       && w.TenantRegistryId == tenantRegistryId
                                                                       && (w.Deleted == 0 || w.Deleted == null));
        }

        public VisualisationRegistry GetById(int id)
        {
            return dbContext.VisualisationRegistry.FirstOrDefault(w => w.Id == id
                                                                       && w.TenantRegistryId == tenantRegistryId
                                                                       && (w.Deleted == 0 || w.Deleted == null));
        }


        public IEnumerable<VisualisationRegistry> GetByShowInDirectory()
        {
            return dbContext.VisualisationRegistry.Where(w => w.ShowInDirectory == 1
                                                              && w.Active == 1
                                                              && w.TenantRegistryId == tenantRegistryId
                                                              && (w.Deleted == 0 || w.Deleted == null));
        }


        public VisualisationRegistry Insert(VisualisationRegistry model)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.Now;
            model.TenantRegistryId = tenantRegistryId;
            model.Version = 1;
            model.Id = dbContext.InsertWithInt32Identity(model);

            return model;
        }

        public VisualisationRegistry Update(VisualisationRegistry model)
        {
            var existing = dbContext.VisualisationRegistry.FirstOrDefault(w => w.Id
                                                                               == model.Id
                                                                               && w.TenantRegistryId == tenantRegistryId
                                                                               && (w.Deleted == 0 || w.Deleted == null)
                                                                               && (w.Locked == 0 || w.Locked == null));

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.CreatedUser = userName;
            model.CreatedDate = DateTime.Now;
            model.TenantRegistryId = tenantRegistryId;
            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;

            dbContext.Update(model);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<VisualisationRegistry, VisualisationRegistryVersion>();
            });
            var mapper = new Mapper(config);

            var audit = mapper.Map<VisualisationRegistryVersion>(existing);
            audit.VisualisationRegistryId = existing.Id;

            return model;
        }

        public void Delete(int id)
        {
            var record = dbContext.VisualisationRegistry
                .FirstOrDefault(u => u.TenantRegistryId == tenantRegistryId
                                     && u.Id == id);

            if (record == null)
            {
                throw new KeyNotFoundException();
            }

            record.DeletedUser = userName;
            record.DeletedDate = DateTime.Now;
            record.Deleted = 1;
            dbContext.Update(record);
        }

        public void DeleteByTenantRegistryIdOutsideOfInstance(int tenantRegistryIdOutsideOfInstance, int importId)
        {
            dbContext.VisualisationRegistry
                .Where(d => d.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                            && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Update();
        }
    }
}
