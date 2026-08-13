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
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using log4net;
    using Reporting;
    using Repository;

    public class GetByVisualisationRegistryDatasourceCommandExecutionQuery(
        DbContext dbContext,
        string user,
        ILog log,
        bool parserAssertSelectOnly,
        string reportConnectionString = null)
    {
        public async Task<List<IDictionary<string, object>>> ExecuteAsync(int id, Dictionary<string, object> parametersByName, CancellationToken token = default)
        {
            var mergedParametersByName = parametersByName;

            var visualisationRegistryDatasourceRepository = new VisualisationRegistryDatasourceRepository(dbContext, user);
            var visualisationRegistryDatasource = await visualisationRegistryDatasourceRepository.GetByIdActiveOnlyAsync(id, token);

            if (visualisationRegistryDatasource.VisualisationRegistryId != null)
            {
                var visualisationRegistryParameterRepository = new VisualisationRegistryParameterRepository(dbContext, user);
                var visualisationRegistryParameters = await visualisationRegistryParameterRepository.GetByVisualisationRegistryIdOrderByIdAsync(visualisationRegistryDatasource.VisualisationRegistryId.Value, token);

                foreach (var visualisationRegistryParameter in visualisationRegistryParameters)
                {
                    var cleanName = visualisationRegistryParameter.Name.Replace(" ", "_");
                    if (!mergedParametersByName.ContainsKey(cleanName))
                    {
                        var defaultString = visualisationRegistryParameter.DefaultValue;
                        switch (visualisationRegistryParameter.DataTypeId)
                        {
                            case 1:
                            {
                                mergedParametersByName.Add(cleanName, defaultString);
                                break;
                            }
                            case 2:
                            {
                                if (Int32.TryParse(defaultString, out var intValue))
                                {
                                    mergedParametersByName.Add(cleanName, intValue);
                                }

                                break;
                            }
                            case 3:
                            {
                                if (Double.TryParse(defaultString, out var doubleValue))
                                {
                                    mergedParametersByName.Add(cleanName, doubleValue);
                                }

                                break;
                            }
                            case 4:
                            {
                                if (Boolean.TryParse(defaultString, out var dateTimeValue))
                                {
                                    mergedParametersByName.Add(cleanName, dateTimeValue);
                                }

                                break;
                            }
                            case 5:
                            {
                                if (DateTime.TryParse(defaultString, out var dateTimeValue))
                                {
                                    mergedParametersByName.Add(cleanName, dateTimeValue);
                                }

                                break;
                            }
                        }

                    }
                }
            }

            using var postgres = new Postgres(reportConnectionString ?? dbContext.Connection.ConnectionString, log, parserAssertSelectOnly);
            return await postgres.ExecuteByNamedParametersAsync(visualisationRegistryDatasource.Command,
                mergedParametersByName, token).ConfigureAwait(false);
        }
    }
}
