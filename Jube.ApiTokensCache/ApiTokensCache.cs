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

namespace Jube.ApiTokensCache
{
    using System.Collections.Concurrent;
    using Jube.Cache;
    using Jube.Cache.Redis.CacheUserRegistryApiKey.Events;
    using Jube.Data.Context;
    using Jube.Data.Repository;
    using Jube.DynamicEnvironment;
    using Jube.TaskCancellation;
    using log4net;

    public class ApiTokensCache
    {
        private readonly ConcurrentDictionary<string, byte> apiKeys = new ConcurrentDictionary<string, byte>();
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        public ApiTokensCache(ILog log, DynamicEnvironment dynamicEnvironment, CacheService cacheService)
        {
            this.log = log;
            this.dynamicEnvironment = dynamicEnvironment;
            cacheService.CacheUserRegistryApiKeyRepository.OnCaseUserRegistryApiKeySetEvent += OnCaseUserRegistryApiKeySetEvent;
            cacheService.CacheUserRegistryApiKeyRepository.OnCaseUserRegistryApiKeyRemoveEvent += OnCaseUserRegistryApiKeyRemoveEvent;
        }

        private void OnCaseUserRegistryApiKeySetEvent(object? sender, CaseUserRegistryKeyEventArguments e)
        {
            apiKeys.TryAdd(e.ApiKeyHash, 0);
        }
        
        private void OnCaseUserRegistryApiKeyRemoveEvent(object? sender, CaseUserRegistryKeyEventArguments e)
        {
            apiKeys.TryRemove(e.ApiKeyHash, out _);
        }

        public string ValidateApiKeyAndReturnAuthenticatedUser(string apiKey)
        {
            if (!ApiKeyHelper.TryParse(apiKey,dynamicEnvironment.AppSettings("ApiHmacKey"), out var components))
            {
                throw new Exception("Parse Error.");
            }

            if (components != null && apiKeys.ContainsKey(components.ApiKeyHash))
            {
                return components.UserName;
            }

            throw new Exception("Not found");
        }

        public async Task StartAsync(TaskCoordinator taskCoordinator)
        {
            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            var repository = new UserRegistryApiKeyRepository(dbContext);

            try
            {
                while (!taskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    var allKeysFromRepository = (await repository.GetAllAsync(taskCoordinator.CancellationToken))
                        .Select(s => s.ApiKey)
                        .ToHashSet();

                    var newApiKeys = allKeysFromRepository.Where(k => !apiKeys.ContainsKey(k));
                    var deletedApiKeys = apiKeys.Keys.Where(k => !allKeysFromRepository.Contains(k)).ToList();

                    foreach (var key in newApiKeys)
                    {
                        apiKeys.TryAdd(key, 0);
                    }

                    foreach (var key in deletedApiKeys)
                    {
                        apiKeys.TryRemove(key, out _);
                    }

                    await Task.Delay(60000, taskCoordinator.CancellationToken);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw;
            }
        }
    }
}
