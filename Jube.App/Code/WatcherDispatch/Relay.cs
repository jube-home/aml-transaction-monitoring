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
    using Data.Context;
    using Data.Repository;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.SignalR;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using Npgsql;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using signalr;
    using TaskCancellation;

    public class Relay
    {
        private IModel channel;
        public Task ConnectToAmqpForActivationWatcherStreamingTask;
        public Task ConnectToDatabaseForActivationWatcherStreamingTask;
        private EventingBasicConsumer consumer;
        private DefaultContractResolver contractResolver;
        private DynamicEnvironment dynamicEnvironment;
        private ILog log;
        private IConnection rabbitMqConnection;
        public bool Ready;
        public Task StreamingActivationWatcherFromDatabaseTableTask;
        private ITaskCoordinator taskCoordinator;
        private IHubContext<WatcherHub> watcherHub;

        public Task StartAsync(IHubContext<WatcherHub> watcherHubContext,
            DynamicEnvironment dynamicEnvironmentContext, ILog logContext, IConnection rabbitMqConnectionContext,
            DefaultContractResolver contractResolverContext, TaskCoordinator taskCoordinatorContext)
        {
            watcherHub = watcherHubContext;
            log = logContext;
            dynamicEnvironment = dynamicEnvironmentContext;
            contractResolver = contractResolverContext;
            taskCoordinator = taskCoordinatorContext;

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
                    ConnectToDatabaseForActivationWatcherStreamingTask = taskCoordinator.RunAsync("ConnectToDatabaseForActivationWatcherStreamingTask", ConnectToDatabaseForActivationWatcherStreamingAsync);
                }
                else
                {
                    if (dynamicEnvironment.AppSettings("ActivationWatcherAllowPersist")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                    {
                        StreamingActivationWatcherFromDatabaseTableTask = taskCoordinator.RunAsync("StreamingActivationWatcherFromDatabaseTableTask", StreamingActivationWatcherFromDatabaseTableAsync);
                    }
                }
            }

            Ready = true;

            return Task.CompletedTask;
        }

        private async Task EventHandlerDatabaseAsync(string payload, CancellationToken cancellationToken = default)
        {
            try
            {
                if (log.IsInfoEnabled)
                {
                    log.Info("Activation Relay: String representation of body received is " + payload + " .");
                }

                var json = JObject.Parse(payload);
                var tenantRegistryId = (json.SelectToken("tenantRegistryId") ?? 0).Value<string>();

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

        private async Task ConnectToDatabaseForActivationWatcherStreamingAsync(CancellationToken token = default)
        {
            var connection = new NpgsqlConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            try
            {
                await connection.OpenAsync(token).ConfigureAwait(false);

                connection.Notification += (sender, e) =>
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await EventHandlerDatabaseAsync(e.Payload, token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // Ignore
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Error processing notification: {ex}");
                        }
                    }, token);
                };


                var cmd = new NpgsqlCommand("LISTEN activation", connection);
                await using (cmd.ConfigureAwait(false))
                {
                    await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                }

                while (true)
                {
                    await connection.WaitAsync(token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                log.Info($"Graceful Cancellation ConnectToDatabaseForActivationWatcherStreamingAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                log.Error($"ConnectToDatabaseForActivationWatcherStreamingAsync: has produced an error {ex}");
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
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

        private async Task StreamingActivationWatcherFromDatabaseTableAsync(CancellationToken token = default)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Data Connection DbContext: Is about to attempt construction of database context with {dynamicEnvironment.AppSettings("ConnectionString")}.");
            }

            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), log);

            if (log.IsInfoEnabled)
            {
                log.Info("Data Connection DbContext: Database context has been constructed.  Returning database context.");
            }

            var activationWatcherRepository = new ActivationWatcherRepository(dbContext);

            var lastActivationWatcher = await activationWatcherRepository.GetLastAsync(token);
            var lastActivationWatcherId = 0;

            if (lastActivationWatcher != null)
            {
                lastActivationWatcherId = lastActivationWatcher.Id;
            }

            while (!token.IsCancellationRequested)
            {
                try
                {
                    foreach (var activationWatcher in await activationWatcherRepository.GetAllSinceIdAsync(lastActivationWatcherId,
                                 100, token))
                    {
                        lastActivationWatcherId = activationWatcher.Id;

                        var stringRepresentationOfObj = JsonConvert.SerializeObject(activationWatcher,
                            new JsonSerializerSettings
                            {
                                ContractResolver = contractResolver
                            });

                        await watcherHub.Clients.Group("Tenant_" + 1)
                            .SendAsync("ReceiveMessage", "RealTime", stringRepresentationOfObj, token).ConfigureAwait(false);
                    }

                    await Task.Delay(Int32.Parse(dynamicEnvironment.AppSettings("WaitPollFromActivationWatcherTable")), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex)
                {
                    log.Info($"Graceful Cancellation StreamingActivationWatcherFromDatabaseTableAsync: has produced an error {ex}");
                }
                catch (Exception ex)
                {
                    log.Error($"StreamingActivationWatcherFromDatabaseTableAsync: has produced an error {ex}");
                    break;
                }
            }
        }
    }
}
