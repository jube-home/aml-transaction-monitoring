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

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.AbstractionRuleCaching
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using EntityAnalysisModel.Models.Models;
    using EntityAnalysisModel=EntityAnalysisModel.EntityAnalysisModel;
    using EntityAnalysisModelAbstractionRule=EntityAnalysisModel.Models.Models.EntityAnalysisModelAbstractionRule;

    public class AbstractionRuleCachingRepository(DbContext dbContext, EntityAnalysisModel entityAnalysisModel)
    {
        public async Task<int> InsertEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesAsync(int entityAnalysisModelsSearchKeyCalculationInstanceId,
            string groupingValue, CancellationToken token = default)
        {
            var value = 0;
            try
            {
                var repository = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceRepository(dbContext);

                var model = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstance
                {
                    EntityAnalysisModelSearchKeyCalculationInstanceId = entityAnalysisModelsSearchKeyCalculationInstanceId,
                    SearchKeyValue = groupingValue,
                    CreatedDate = DateTime.UtcNow
                };

                model = await repository.InsertAsync(model, token).ConfigureAwait(false);

                value = model.Id;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"InsertEntityAnalysisModelsSearchKeyDistinctValueCalculationInstances: has produced an error {ex}");
            }

            return value;
        }

        public async Task UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesEntriesCountAsync(
            int entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId, int count, CancellationToken token = default)
        {
            try
            {
                var repository = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceRepository(dbContext);

                await repository.UpdateEntriesCountAsync(entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId, count, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesEntriesCountAsync: has produced an error {ex}");
            }
        }

        public async Task<int> InsertEntityAnalysisModelsSearchKeyCalculationInstancesAsync(
            DistinctSearchKey distinctSearchKey,
            DateTime toDate, CancellationToken token = default)
        {
            var value = 0;
            try
            {
                var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);

                var model = new EntityAnalysisModelSearchKeyCalculationInstance
                {
                    SearchKey = distinctSearchKey.SearchKey,
                    EntityAnalysisModelGuid = entityAnalysisModel.Instance.Guid,
                    DistinctFetchToDate = toDate,
                    CreatedDate = DateTime.UtcNow
                };

                await repository.InsertAsync(model, token).ConfigureAwait(false);

                value = model.Id;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"InsertEntityAnalysisModelsSearchKeyCalculationInstancesAsync: has produced an error {ex}");
            }

            return value;
        }

        public async Task UpdateEntityAnalysisModelsSearchKeyCalculationInstancesDistinctValuesAsync(
            int entityAnalysisModelsSearchKeyCalculationInstanceId, int count, CancellationToken token = default)
        {
            try
            {
                var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);

                await repository.UpdateDistinctValuesCountAsync(entityAnalysisModelsSearchKeyCalculationInstanceId, count, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"UpdateEntityAnalysisModelsSearchKeyCalculationInstancesDistinctValuesAsync: has produced an error {ex}");
            }
        }

        public async Task UpdateEntityAnalysisModelsSearchKeyCalculationInstancesExpiredSearchKeyCacheCountAsync(
            int entityAnalysisModelsSearchKeyCalculationInstanceId,
            int count, CancellationToken token = default)
        {
            try
            {
                var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);
                await repository.UpdateExpiredSearchKeyCacheCountAsync(entityAnalysisModelsSearchKeyCalculationInstanceId, count, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"UpdateEntityAnalysisModelsSearchKeyCalculationInstancesExpiredSearchKeyCacheCountAsync: has produced an error {ex}");
            }
        }

        public async Task UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesAbstractionRulesMatchesAsync(
            int entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId, CancellationToken token = default)
        {
            try
            {
                var repository = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceRepository(dbContext);

                await repository.UpdateAbstractionRuleMatchesAsync(entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesAbstractionRulesMatchesAsync: has produced an error {ex}");
            }
        }

        public async Task InsertToArchiveAsync(int entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId,
            DistinctSearchKey distinctSearchKey,
            string groupingValue, EntityAnalysisModelAbstractionRule abstractionRule,
            double abstractionValue, CancellationToken token = default)
        {
            try
            {
                var repository = new ArchiveEntityAnalysisModelAbstractionEntryRepository(dbContext);

                var model = new ArchiveEntityAnalysisModelAbstractionEntry
                {
                    EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceId =
                        entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId,
                    SearchKey = distinctSearchKey.SearchKey,
                    SearchValue = groupingValue,
                    Value = abstractionValue,
                    EntityAnalysisModelAbstractionRuleId = abstractionRule.Id,
                    CreatedDate = DateTime.UtcNow
                };

                await repository.InsertAsync(model, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"ReplicateToDatabaseAsync: has produced an error {ex}");
            }
        }

        public async Task UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesCompletedAsync(
            int entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId, CancellationToken token = default)
        {
            try
            {
                var repository = new EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceRepository(dbContext);

                await repository.UpdateCompletedAsync(entityAnalysisModelsSearchKeyDistinctValueCalculationInstanceId, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"UpdateEntityAnalysisModelsSearchKeyDistinctValueCalculationInstancesCompletedAsync: has produced an error {ex}");
            }
        }

        public async Task UpdateEntityAnalysisModelsSearchKeyCalculationInstancesDistinctValuesProcessedValuesCountAsync(
            int entityAnalysisModelsSearchKeyCalculationInstanceId,
            int distinctValuesProcessedValuesCount, CancellationToken token = default)
        {
            try
            {
                var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);
                await repository.UpdateDistinctValuesProcessedValuesCountAsync(entityAnalysisModelsSearchKeyCalculationInstanceId,
                    distinctValuesProcessedValuesCount, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"UpdateEntityAnalysisModelsSearchKeyCalculationInstancesDistinctValuesProcessedValuesCountAsync: has produced an error {ex}");
            }
        }

        public async Task UpdateEntityAnalysisModelsSearchKeyCalculationInstancesCompletedAsync(int entityAnalysisModelsSearchKeyCalculationInstanceId,
            CancellationToken token = default)
        {
            try
            {
                var repository = new EntityAnalysisModelSearchKeyCalculationInstanceRepository(dbContext);
                await repository.UpdateCompletedAsync(entityAnalysisModelsSearchKeyCalculationInstanceId, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"UpdateEntityAnalysisModelsSearchKeyCalculationInstancesCompletedAsync: has produced an error {ex}");
            }
        }
    }
}
