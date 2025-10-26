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

    public class ExhaustiveSearchInstanceTrialInstanceVariableRepository(DbContext dbContext)
    {

        public ExhaustiveSearchInstanceTrialInstanceVariable Insert(ExhaustiveSearchInstanceTrialInstanceVariable model)
        {
            model.Id = dbContext.InsertWithInt32Identity(model);
            return model;
        }

        public void DeleteAllByExhaustiveSearchInstanceTrialInstanceId(int id)
        {
            dbContext.ExhaustiveSearchInstanceTrialInstanceVariable
                .Where(w => w.ExhaustiveSearchInstanceTrialInstanceId == id)
                .Delete();
        }

        public void UpdateAsRemovedByExhaustiveSearchInstanceVariableId(int exhaustiveSearchInstanceVariableId,
            int exhaustiveSearchInstanceTrialInstanceId)
        {
            var records = dbContext.ExhaustiveSearchInstanceTrialInstanceVariable
                .Where(u =>
                    u.ExhaustiveSearchInstanceVariableId == exhaustiveSearchInstanceVariableId
                    && u.ExhaustiveSearchInstanceTrialInstanceId == exhaustiveSearchInstanceTrialInstanceId)
                .Set(s => s.Removed, 1)
                .Update();

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public IQueryable<ExhaustiveSearchInstanceTrialInstanceVariable>
            GetByExhaustiveSearchInstanceTrialInstanceIdOrderById(
                int exhaustiveSearchInstanceTrialInstanceId)
        {
            return dbContext.ExhaustiveSearchInstanceTrialInstanceVariable.Where(w =>
                    w.ExhaustiveSearchInstanceTrialInstanceId == exhaustiveSearchInstanceTrialInstanceId)
                .OrderBy(o => o.Id);
        }

        public void DeleteByTenantRegistryIdOutsideOfInstance(int tenantRegistryIdOutsideOfInstance, int importId)
        {
            dbContext.ExhaustiveSearchInstanceTrialInstanceVariable
                .Where(d =>
                    d.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.EntityAnalysisModel
                        .TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.Now)
                .Update();
        }
    }
}
