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

namespace Jube.Data.Context
{
    using LinqToDB.Configuration;
    using LinqToDB.DataProvider.PostgreSQL;
    using log4net;
    using ResilientNpgsqlConnection;

    public static class DataConnectionDbContext
    {
        public static DbContext GetResilientDbContextDataConnection(string connectionString, ILog log)
        {
            var builder = new LinqToDbConnectionOptionsBuilder();
            builder.UseConnectionFactory(
                PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v95),
                () => new ResilientNpgsqlConnection(connectionString, log)
            );
            var connection = builder.Build<DbContext>();
            return new DbContext(connection);
        }

        public static DbContext GetNgpsqlDbContextDataConnection(string connectionString)
        {
            var builder = new LinqToDbConnectionOptionsBuilder();
            builder.UsePostgreSQL(connectionString);
            var connection = builder.Build<DbContext>();
            return new DbContext(connection);
        }
    }
}
