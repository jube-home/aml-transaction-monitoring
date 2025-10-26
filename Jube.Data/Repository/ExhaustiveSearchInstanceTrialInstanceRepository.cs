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
    using System.Linq;
    using Context;
    using LinqToDB;
    using Poco;

    public class ExhaustiveSearchInstanceTrialInstanceRepository(DbContext dbContext)
    {

        public ExhaustiveSearchInstanceTrialInstance Insert(ExhaustiveSearchInstanceTrialInstance model)
        {
            model.Id = dbContext.InsertWithInt32Identity(model);
            return model;
        }

        public IQueryable<ExhaustiveSearchInstanceTrialInstance> GetByExhaustiveSearchInstanceIdOrderById(
            int exhaustiveSearchInstanceId)
        {
            return dbContext.ExhaustiveSearchInstanceTrialInstance.Where(w
                    => w.ExhaustiveSearchInstanceId == exhaustiveSearchInstanceId)
                .OrderBy(o => o.Id);
        }

        public void DeleteByTenantRegistryIdOutsideOfInstance(int tenantRegistryIdOutsideOfInstance, int importId)
        {
            dbContext.ExhaustiveSearchInstanceTrialInstance
                .Where(d =>
                    d.ExhaustiveSearchInstance.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Update();
        }
    }
}
