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
    using Context;
    using LinqToDB;
    using Poco;

    public class EntityAnalysisModelDictionaryCsvFileUploadRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelDictionaryCsvFileUploadRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public IEnumerable<EntityAnalysisModelDictionaryCsvFileUpload> Get()
        {
            return dbContext.EntityAnalysisModelDictionaryCsvFileUpload
                .Where(w => w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId);
        }

        public IEnumerable<EntityAnalysisModelDictionaryCsvFileUpload> GetByEntityAnalysisModelDictionaryId(
            int entityAnalysisModelDictionaryId)
        {
            return dbContext.EntityAnalysisModelDictionaryCsvFileUpload
                .Where(w => w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.EntityAnalysisModelDictionary.Id == entityAnalysisModelDictionaryId &&
                            (w.EntityAnalysisModelDictionary.Deleted == 0 ||
                             w.EntityAnalysisModelDictionary.Deleted == null));
        }

        public EntityAnalysisModelDictionaryCsvFileUpload GetById(int id)
        {
            return dbContext.EntityAnalysisModelDictionaryCsvFileUpload.FirstOrDefault(w =>
                w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && w.EntityAnalysisModelDictionary.Id == id && (w.EntityAnalysisModelDictionary.Deleted == 0 ||
                                                                w.EntityAnalysisModelDictionary.Deleted == null));
        }

        public EntityAnalysisModelDictionaryCsvFileUpload Insert(EntityAnalysisModelDictionaryCsvFileUpload model)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.Now;
            model.InheritedId = dbContext.InsertWithInt32Identity(model);
            return model;
        }
        
        public EntityAnalysisModelDictionaryCsvFileUpload
            Update(
                EntityAnalysisModelDictionaryCsvFileUpload
                    model)
        {
            var existing = dbContext.EntityAnalysisModelDictionaryCsvFileUpload
                .FirstOrDefault(w => w.Id
                                     == model.Id
                                     && w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId ==
                                     tenantRegistryId
                                     && (w.EntityAnalysisModelDictionary.Deleted == 0 ||
                                         w.EntityAnalysisModelDictionary.Deleted == null));

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.Now;
            model.InheritedId = existing.Id;

            Delete(existing.Id);

            var id = dbContext.InsertWithInt32Identity(model);

            model.Id = id;

            return model;
        }

        public void Delete(int id)
        {
            var records = dbContext.EntityAnalysisModelDictionaryCsvFileUpload
                .Where(d => d.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && d.Id == id
                            && (d.EntityAnalysisModelDictionary.Deleted == 0 ||
                                d.EntityAnalysisModelDictionary.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Set(s => s.DeletedUser, userName)
                .Update();

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }
    }
}
