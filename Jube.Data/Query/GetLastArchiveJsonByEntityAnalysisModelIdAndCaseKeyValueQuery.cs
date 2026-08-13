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

namespace Jube.Data.Query
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Extension;
    using ResilientNpgsqlConnection;
    using ResilientNpgsqlConnection.Extensions.Jube.ResilientNpgsqlConnection;

    public class GetLastArchiveJsonByEntityAnalysisModelIdAndCaseKeyValueQuery(DbContext dbContext)
    {
        public async Task<(string Json, Guid EntityAnalysisModelInstanceEntryGuid)?> ExecuteAsync(
            int entityAnalysisModelId, string caseKey, string caseKeyValue, CancellationToken token = default)
        {
            const string sql = """
                                select "Json", "EntityAnalysisModelInstanceEntryGuid"
                                from "Archive"
                                where "EntityAnalysisModelId" = @entityAnalysisModelId
                                  and ("Json" -> 'payload' ->> @caseKey) = @caseKeyValue
                                order by "CreatedDate" desc
                                limit 1
                                """;

            await using var command = new ResilientNpgsqlCommand((ResilientNpgsqlConnection)dbContext.Connection, sql);
            command.Parameters.AddWithValue("@entityAnalysisModelId", entityAnalysisModelId);
            command.Parameters.AddWithValue("@caseKey", caseKey);
            command.Parameters.AddWithValue("@caseKeyValue", caseKeyValue);
            await command.PrepareAsync(token).ConfigureAwait(false);

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            if (!await reader.ReadAsync(token).ConfigureAwait(false))
            {
                return null;
            }

            return (reader.GetValue(0).AsString(), reader.GetValue(1).AsGuid());
        }
    }
}
