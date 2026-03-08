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
    using Poco;

    public class ExhaustiveSearchInstanceRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public ExhaustiveSearchInstanceRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public ExhaustiveSearchInstanceRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public ExhaustiveSearchInstanceRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public Task<ExhaustiveSearchInstance> GetByNameEntityAnalysisModelIdAsync(string name, int entityAnalysisModelId, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstance
                .FirstOrDefaultAsync(f =>
                    f.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                    && f.EntityAnalysisModelId == entityAnalysisModelId
                    && (f.Deleted == 0 || f.Deleted == null)
                    && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<ExhaustiveSearchInstance>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.ExhaustiveSearchInstance
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId).ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<ExhaustiveSearchInstance>> GetByEntityAnalysisModelIdOrderByIdAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            return await dbContext.ExhaustiveSearchInstance.Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token).ConfigureAwait(false);
        }

        public async Task<IEnumerable<ExhaustiveSearchInstance>> GetByEntityAnalysisModelIdOrderByNameAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            return await dbContext.ExhaustiveSearchInstance.Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId
                    && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Name).ToListAsync(token).ConfigureAwait(false);
        }

        public Task<ExhaustiveSearchInstance> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstance.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Id == id && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<ExhaustiveSearchInstance> InsertAsync(ExhaustiveSearchInstance model, CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.Now;
            model.UpdatedDate = DateTime.Now;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public async Task<ExhaustiveSearchInstance> UpdateAsync(ExhaustiveSearchInstance model, CancellationToken token = default)
        {
            var existing = dbContext.ExhaustiveSearchInstance
                .FirstOrDefault(w => w.Id
                                     == model.Id
                                     && (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                                         !tenantRegistryId
                                             .HasValue)
                                     && (w.StatusId == 0 || w.StatusId == null)
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

            await dbContext.UpdateAsync(model, token: token).ConfigureAwait(false);

            var mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ExhaustiveSearchInstance, ExhaustiveSearchInstanceVersion>();
            }));

            var audit = mapper.Map<ExhaustiveSearchInstanceVersion>(existing);
            audit.ExhaustiveSearchInstanceId = existing.Id;

            await dbContext.InsertAsync(audit, token: token).ConfigureAwait(false);

            return model;
        }


        public Task StopAsync(Guid exhaustiveSearchInstanceGuid, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstance
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Guid == exhaustiveSearchInstanceGuid
                    && (d.Deleted == 0 || d.Deleted == null)
                    && d.StatusId != 19)
                .Set(s => s.UpdatedDate, DateTime.Now)
                .Set(s => s.StatusId, (byte)18)
                .UpdateAsync(token);
        }

        public async Task<bool> IsStoppingOrStoppedAsync(int id, CancellationToken token = default)
        {
            return await dbContext.ExhaustiveSearchInstance.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && (w.StatusId == 18 || w.StatusId == 19)
                && w.Id == id && (w.Deleted == 0 || w.Deleted == null), token).ConfigureAwait(false) == null;
        }

        public async Task UpdateStatusAsync(int id, byte statusId, CancellationToken token = default)
        {
            var records = await dbContext.ExhaustiveSearchInstance
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.UpdatedDate, DateTime.Now)
                .Set(s => s.StatusId, statusId)
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public async Task UpdateCompletedAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.ExhaustiveSearchInstance
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.UpdatedDate, DateTime.Now)
                .Set(s => s.CompletedDate, DateTime.Now)
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public async Task UpdateModelsAsync(int id, int models, CancellationToken token = default)
        {
            var records = await dbContext.ExhaustiveSearchInstance
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Models, models)
                .Set(s => s.UpdatedDate, DateTime.Now)
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public async Task UpdateModelsSinceBestAsync(int id, int modelsSinceBest, CancellationToken token = default)
        {
            var records = await dbContext.ExhaustiveSearchInstance
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ModelsSinceBest, modelsSinceBest)
                .Set(s => s.UpdatedDate, DateTime.Now)
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public async Task UpdateBestScoreAsync(int id, double score, int topologyComplexity, CancellationToken token = default)
        {
            var records = await dbContext.ExhaustiveSearchInstance
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Score, score)
                .Set(s => s.TopologyComplexity, topologyComplexity)
                .Set(s => s.UpdatedDate, DateTime.Now)
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public async Task DeleteAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.ExhaustiveSearchInstance
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Locked == 0 || d.Locked == null)
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Set(s => s.DeletedUser, userName)
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId, CancellationToken token = default)
        {
            return dbContext.ExhaustiveSearchInstance
                .Where(d =>
                    d.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .UpdateAsync(token);
        }
    }
}
