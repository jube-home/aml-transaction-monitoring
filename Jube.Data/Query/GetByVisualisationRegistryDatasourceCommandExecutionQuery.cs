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
    using System.Linq;
    using System.Threading.Tasks;
    using Context;
    using FluentMigrator.Runner;
    using Poco;
    using Reporting;
    using Repository;

    public class GetByVisualisationRegistryDatasourceCommandExecutionQuery(
        DbContext dbContext,
        string connectionString,
        string user)
    {
        public async Task<dynamic> ExecuteAsync(int id, Dictionary<int, object> parametersById)
        {
            var values = new List<IDictionary<string, object>>();
            var visualisationRegistryDatasourceRepository =
                new VisualisationRegistryDatasourceRepository(dbContext, user);
            var visualisationRegistryDatasource = visualisationRegistryDatasourceRepository.GetById(id);

            var visualisationRegistryParameterRepository =
                new VisualisationRegistryParameterRepository(dbContext, user);

            if (visualisationRegistryDatasource.VisualisationRegistryId == null)
            {
                return values;
            }

            var visualisationRegistryParameter
                = visualisationRegistryParameterRepository
                    .GetByVisualisationRegistryIdOrderById(visualisationRegistryDatasource.VisualisationRegistryId.Value);

            var parametersByName = visualisationRegistryParameter.ToDictionary(
                parameter => parameter.Name.Replace(" ", "_"), parameter =>
                    parametersById.TryGetValue(parameter.Id, out var value)
                        ? value
                        : parameter.DefaultValue);

            var sw = new StopWatch();
            sw.Start();

            string error = null;
            try
            {
                var postgres = new Postgres(connectionString);
                values = await postgres.ExecuteByNamedParametersAsync(visualisationRegistryDatasource.Command,
                    parametersByName).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                error = ex.ToString();
            }

            sw.Stop();

            var visualisationRegistryDatasourceExecutionLog =
                new VisualisationRegistryDatasourceExecutionLog
                {
                    Records = values.Count,
                    Error = error,
                    ResponseTime = sw.ElapsedTime().Milliseconds,
                    VisualisationRegistryDatasourceId = visualisationRegistryDatasource.Id,
                    CreatedDate = DateTime.Now,
                    CreatedUser = user
                };

            var visualisationRegistryDatasourceExecutionLogRepository
                = new VisualisationRegistryDatasourceExecutionLogRepository(dbContext);

            visualisationRegistryDatasourceExecutionLog =
                visualisationRegistryDatasourceExecutionLogRepository.Insert(
                    visualisationRegistryDatasourceExecutionLog);

            var visualisationRegistryDatasourceExecutionLogParameterRepository
                = new VisualisationRegistryDatasourceExecutionLogParameterRepository(dbContext);

            foreach (var visualisationRegistryDatasourceExecutionLogParameter in parametersById.Select(parameter =>
                         new VisualisationRegistryDatasourceExecutionLogParameter
                         {
                             Value = parameter.Value.ToString(),
                             VisualisationRegistryDatasourceExecutionLogId =
                                 visualisationRegistryDatasourceExecutionLog.Id,
                             VisualisationRegistryParameterId = parameter.Key
                         }))
            {
                visualisationRegistryDatasourceExecutionLogParameterRepository
                    .Insert(visualisationRegistryDatasourceExecutionLogParameter);
            }

            return values;
        }
    }
}
