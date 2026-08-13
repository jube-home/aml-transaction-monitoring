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

namespace Jube.App.Code.WatcherDispatch
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Cache;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.SignalR;
    using Newtonsoft.Json.Linq;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using signalr;
    using StackExchange.Redis;
    using TaskCancellation;

    public class Relay
    {
        private IModel channel;
        private ConnectionMultiplexer connectionMultiplexer;
        public Task ConnectToAmqpForActivationWatcherStreamingTask;
        public Task ConnectToSignalRForActivationWatcherStreamingTask;
        private EventingBasicConsumer consumer;
        private DynamicEnvironment dynamicEnvironment;
        private ILog log;
        private IConnection rabbitMqConnection;
        public bool Ready;
        private ITaskCoordinator taskCoordinator;
        private IHubContext<WatcherHub> watcherHub;

        public Task StartAsync(IHubContext<WatcherHub> watcherHubContext,
            DynamicEnvironment dynamicEnvironmentContext, ILog logContext, IConnection rabbitMqConnectionContext,
            TaskCoordinator taskCoordinatorContext, CacheService cacheService)
        {
            watcherHub = watcherHubContext;
            log = logContext;
            dynamicEnvironment = dynamicEnvironmentContext;
            taskCoordinator = taskCoordinatorContext;
            connectionMultiplexer = cacheService.ConnectionMultiplexer;

            if (dynamicEnvironment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (!dynamicEnvironment.AppSettings("StreamingActivationWatcher")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.CompletedTask;
                }

                rabbitMqConnection = rabbitMqConnectionContext;

                ConnectToAmqpForActivationWatcherStreamingTask = taskCoordinator.RunAsync("ConnectToAmqpForActivationWatcherStreamingTask", ConnectToAmqpForActivationWatcherStreamingAsync);
            }
            else
            {
                if (dynamicEnvironment.AppSettings("StreamingActivationWatcher")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    ConnectToSignalRForActivationWatcherStreamingTask = taskCoordinator.RunAsync("ConnectToRedisForActivationWatcherStreamingTask", ConnectToRedisForActivationWatcherStreamingAsync);
                }
            }

            Ready = true;

            return Task.CompletedTask;
        }

        private async Task EventHandlerRedisAsync(string tenantRegistryId, string payload, CancellationToken cancellationToken = default)
        {
            try
            {
                if (log.IsInfoEnabled)
                {
                    log.Info("Activation Relay: String representation of body received is " + payload + " .");
                }

                await watcherHub.Clients.Group("Tenant_" + tenantRegistryId)
                    .SendAsync("ReceiveMessage", "RealTime", payload, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                log.Error(ex.ToString());
            }
        }

        private async Task EventHandlerSignalRAsync(BasicDeliverEventArgs e, CancellationToken cancellationToken = default)
        {
            try
            {
                if (log.IsInfoEnabled)
                {
                    log.Info("Activation Relay: Message Received.");
                }

                var bodyString = Encoding.UTF8.GetString(e.Body.ToArray());

                if (log.IsInfoEnabled)
                {
                    log.Info("Activation Relay: String representation of body received is " + bodyString + " .");
                }

                var json = JObject.Parse(bodyString);
                var tenantRegistryId = (json.SelectToken("tenantRegistryId") ?? 0).Value<string>();

                await watcherHub.Clients.Group("Tenant_" + tenantRegistryId)
                    .SendAsync("ReceiveMessage", "RealTime", bodyString, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                log.Error($"EventHandlerSignalRAsync: has produced an error {ex}");
            }
        }

        private Task ConnectToRedisForActivationWatcherStreamingAsync(CancellationToken token = default)
        {
            var subscriber = connectionMultiplexer.GetSubscriber();
            var redisChannel = RedisChannel.Pattern("ActivationWatcher*");
            token.Register(() => subscriber.Unsubscribe(redisChannel));

 #pragma warning disable VSTHRD101
 #pragma warning disable AsyncFixer03
            return subscriber.SubscribeAsync(redisChannel, async (channel, value) =>
            {
                await EventHandlerRedisAsync(channel.ToString().Split(':')[1], value, token);
            });
 #pragma warning restore AsyncFixer03
 #pragma warning restore VSTHRD101
        }

        private Task ConnectToAmqpForActivationWatcherStreamingAsync(CancellationToken token = default)
        {
            try
            {
                channel = rabbitMqConnection.CreateModel();
                channel.ExchangeDeclare("jubeActivations", ExchangeType.Fanout);

                var rabbitMqQueueName = channel.QueueDeclare();
                channel.QueueBind(rabbitMqQueueName, "jubeActivations", "");

                consumer = new EventingBasicConsumer(channel);
                consumer.Received += (o, ea) =>
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await EventHandlerSignalRAsync(ea, token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info($"Could not relay event with exception {ex}.");
                            }
                        }
                    }, token);
                };

                var basicConsume = channel.BasicConsume(rabbitMqQueueName, true, consumer);
                token.Register(() =>
                {
                    try
                    {
                        channel.BasicCancel(basicConsume);
                        channel.Close();
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error during RabbitMQ teardown: " + ex);
                    }
                });
            }
            catch (OperationCanceledException ex)
            {
                log.Info($"Graceful Cancellation ConnectToAmqpForActivationWatcherStreaming: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                log.Error($"ConnectToAmqpForActivationWatcherStreaming: has produced an error {ex}");
            }

            return Task.CompletedTask;
        }
    }
}
