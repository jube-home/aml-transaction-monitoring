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

using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository.Interface;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class ExhaustiveSearchInstanceVariableAnomalyRepository : IGenericRepository
    {
        private readonly DbContext _dbContext;

        public ExhaustiveSearchInstanceVariableAnomalyRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public int Insert(object arg)
        {
            return _dbContext.InsertWithInt32Identity((ExhaustiveSearchInstanceVariableAnomaly)arg);
        }
    }
}