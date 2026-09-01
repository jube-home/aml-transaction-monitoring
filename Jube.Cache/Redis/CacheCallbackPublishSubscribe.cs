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

namespace Jube.Cache.Redis
{
    using System.Collections.Concurrent;
    using System.Net;
    using log4net;
    using ResilientRedisConnection;
    using StackExchange.Redis;
    using TaskCancellation;

    public class CacheCallbackPublishSubscribe
    {
        public readonly Task CallbackRemoveSubscriptionTask;
        public readonly ConcurrentDictionary<Guid, TaskCompletionSource<Callback.Callback>> Callbacks;

        public readonly Task CallbackSetSubscriptionTask;
        private readonly int callbackTimeout;
        public readonly Task CallbackTimeoutTask;
        private readonly ConnectionMultiplexer connectionMultiplexer;
        private readonly string localCacheInstanceGuidString;
        private readonly ILog log;
        private readonly IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase;

        public CacheCallbackPublishSubscribe(ConnectionMultiplexer connectionMultiplexer,
            IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
            ConcurrentDictionary<Guid, TaskCompletionSource<Callback.Callback>> callbacks,
            int callbackTimeout,
            ILog log,
            TaskCoordinator taskCoordinator)
        {
            this.connectionMultiplexer = connectionMultiplexer;
            this.resilientRedisResilientRedisDatabase = resilientRedisResilientRedisDatabase;
            Callbacks = callbacks;
            this.log = log;
            this.callbackTimeout = callbackTimeout;
            localCacheInstanceGuidString = Guid.NewGuid().ToString("N");
            CallbackSetSubscriptionTask = taskCoordinator.RunAsync("CallbackListenAddAsync", _ => StartCallbackSubscriptionAddAsync(taskCoordinator.CancellationToken));
            CallbackRemoveSubscriptionTask = taskCoordinator.RunAsync("CallbackListenRemoveAsync", _ => StartCallbackSubscriptionRemoveAsync(taskCoordinator.CancellationToken));
            CallbackTimeoutTask = taskCoordinator.RunAsync("CallbackListenRemoveAsync", _ => StartCallbackTimeoutAsync(taskCoordinator.CancellationToken));
        }

        private async Task<Task> StartCallbackTimeoutAsync(CancellationToken token)
        {
            const int pollDelay = 1000;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Callback Timeout Management: Starting to inspect pending callbacks.");
                    }

                    var threshold = DateTime.UtcNow.AddMilliseconds(-callbackTimeout);

                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"Callback Timeout Management: Threshold for timeout is {threshold} and it has been offset from now by {callbackTimeout} ms.");
                    }

                    foreach (var pendingCallback in Callbacks)
                    {
                        var tcs = pendingCallback.Value;

                        if (!tcs.Task.IsCompletedSuccessfully)
                        {
                            continue;
                        }

 #pragma warning disable VSTHRD003
                        var callback = await tcs.Task;//We know it is complete,  as above.
 #pragma warning restore VSTHRD003

                        if (callback.CreatedDate > threshold)
                        {
                            continue;
                        }

                        if (log.IsDebugEnabled)
                        {
                            log.Debug($"Callback Timeout Management: Expired callback {pendingCallback.Key} found.");
                        }

                        Callbacks.TryRemove(pendingCallback);

                        if (log.IsDebugEnabled)
                        {
                            log.Debug(
                                $"Callback Timeout Management: Expired callback {pendingCallback.Key} removed..");
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.Error(
                        $"Callback Timeout Management: Has created an error trying to timeout callbacks {ex}.");
                }

                await Task.Delay(pollDelay, token);
            }
            return Task.FromResult(Task.CompletedTask);
        }

        private void AddToDictionaryFromSubscription(string channel, byte[] value)
        {
            try
            {
                var splits = channel.Split(":");
                if (splits[1] == Dns.GetHostName() && splits[2] == localCacheInstanceGuidString)
                {
                    return;
                }

                var guid = Guid.Parse(splits[3]);
                AddToDictionary(value, guid);
            }
            catch (Exception ex)
            {
                log.Info($"Failed to parse message from channel {channel} with error {ex}.");
            }
        }
        private void AddToDictionary(byte[] value, Guid guid)
        {

            var callback = new Callback.Callback
            {
                CreatedDate = DateTime.UtcNow,
                Payload = value
            };

            var tcs = Callbacks.GetOrAdd(guid, _ => new TaskCompletionSource<Callback.Callback>(TaskCreationOptions.RunContinuationsAsynchronously));
            tcs.TrySetResult(callback);
        }

        private void RemoveFromDictionaryFromSubscription(string channel, RedisValue value)
        {
            try
            {
                var splits = channel.Split(":");

                if (splits[1] == Dns.GetHostName() && splits[2] == localCacheInstanceGuidString)
                {
                    return;
                }

                var guid = Guid.Parse(value.ToString());
                Callbacks.TryRemove(guid, out _);
            }
            catch (Exception ex)
            {
                log.Info($"Failed to parse redis value to guid {value} with error {ex}.");
            }
        }

        private async Task StartCallbackSubscriptionAddAsync(CancellationToken token = default)
        {
            var subscriber = connectionMultiplexer.GetSubscriber();
            var channel = RedisChannel.Pattern("CallbackSet:*");

            try
            {
                await subscriber.SubscribeAsync(channel, (ch, msg) => AddToDictionaryFromSubscription(ch, msg));
                log.Info("Subscribed to Redis callbacks.");

                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (OperationCanceledException)
                {
                    log.Info("Graceful cancellation of Redis callback subscription.");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Redis callback subscription error: {ex}");
            }
            finally
            {
                await subscriber.UnsubscribeAsync(channel);
                log.Info("Unsubscribed from Redis callbacks.");
            }
        }

        private async Task StartCallbackSubscriptionRemoveAsync(CancellationToken token = default)
        {
            var subscriber = connectionMultiplexer.GetSubscriber();
            var channel = RedisChannel.Pattern("CallbackRemove:*");

            try
            {
                await subscriber.SubscribeAsync(channel, (ch, msg) => RemoveFromDictionaryFromSubscription(ch, msg));
                log.Info("Subscribed to Redis callbacks.");

                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (OperationCanceledException)
                {
                    log.Info("Graceful cancellation of Redis callback subscription.");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Redis callback subscription error: {ex}");
            }
            finally
            {
                await subscriber.UnsubscribeAsync(channel);
                log.Info("Unsubscribed from Redis callbacks.");
            }
        }

        public async Task PublishAsync(byte[] json, Guid entityAnalysisModelInstanceEntryGuid, CancellationToken token = default)
        {
            try
            {
                AddToDictionary(json, entityAnalysisModelInstanceEntryGuid);

                await resilientRedisResilientRedisDatabase.PublishAsync(
                    RedisChannel.Pattern($"CallbackSet:{Dns.GetHostName()}:{localCacheInstanceGuidString}:{entityAnalysisModelInstanceEntryGuid:N}"),
                    json);
            }
            catch (Exception ex)
            {
                log.Error($"Cache SQL: Has created an exception as {ex}.");
            }
        }

        public async Task DeleteAsync(Guid entityAnalysisModelInstanceEntryGuid, CancellationToken token = default)
        {
            try
            {
                Callbacks.TryRemove(entityAnalysisModelInstanceEntryGuid, out _);

                await resilientRedisResilientRedisDatabase.PublishAsync(
                    RedisChannel.Pattern($"CallbackRemove:{Dns.GetHostName()}:{localCacheInstanceGuidString}")
                    , new RedisValue(entityAnalysisModelInstanceEntryGuid.ToString("N")));
            }
            catch (Exception ex)
            {
                log.Error($"Cache SQL: Has created an exception as {ex}.");
            }
        }
    }
}
