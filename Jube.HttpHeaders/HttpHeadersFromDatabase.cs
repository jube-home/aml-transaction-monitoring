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

namespace Jube.HttpHeaders
{
    using Data.Context;
    using Data.Repository;
    using log4net;

    public class HttpHeadersFromDatabase
    {
        public readonly IReadOnlyDictionary<string, string> HeaderValues;

        public HttpHeadersFromDatabase(string connectionString, ILog log, CancellationToken token = default)
        {
            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(connectionString, log);
            var httpHeaderRepository = new HttpHeaderRepository(dbContext);
            HeaderValues = httpHeaderRepository.GetAsync(token)
                .GetAwaiter().GetResult()
                .ToDictionary(x => x.Header, x => x.Value);
        }
    }
}
