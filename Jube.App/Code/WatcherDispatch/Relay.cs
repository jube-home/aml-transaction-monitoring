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
    using Data.Context;
    using Data.Repository;
    using DynamicEnvironment;
    using LinqToDB.Configuration;
    using log4net;
    using Microsoft.AspNetCore.SignalR;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using Npgsql;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using signalr;

    public class Relay
    {
        private DefaultContractResolver contractResolver;
        private DynamicEnvironment dynamicEnvironment;
        private ILog log;
        private IModel rabbitMqChannel;
        private IHubContext<WatcherHub> watcherHub;
        private bool Stopping { get; set; }

        public void Start(IHubContext<WatcherHub> watcherHubContext,
            DynamicEnvironment dynamicEnvironmentContext, ILog logContext, IModel rabbitMqChannelContext,
            DefaultContractResolver contractResolverContext)
        {
            watcherHub = watcherHubContext;
            log = logContext;
            dynamicEnvironment = dynamicEnvironmentContext;
            contractResolver = contractResolverContext;

            if (dynamicEnvironment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rabbitMqChannel = rabbitMqChannelContext;
                ConnectToAmqp();
            }
            else
            {
                if (dynamicEnvironment.AppSettings("StreamingActivationWatcher")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    var fromDatabaseNotifications = new Thread(ConnectToDatabaseNotifications);
                    fromDatabaseNotifications.Start();
                }
                else
                {
                    if (!dynamicEnvironment.AppSettings("ActivationWatcherAllowPersist")
                            .Equals("True", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    var fromDbContext = new Thread(FromDbContext);
                    fromDbContext.Start();
                }
            }
        }

        private void EventHandlerDatabase(string payload)
        {
            try
            {
                if (log.IsInfoEnabled)
                {
                    log.Info("Activation Relay: String representation of body received is " + payload + " .");
                }

                var json = JObject.Parse(payload);
                var tenantRegistryId = (json.SelectToken("tenantRegistryId") ?? 0).Value<string>();

                watcherHub.Clients.Group("Tenant_" + tenantRegistryId)
                    .SendAsync("ReceiveMessage", "RealTime", payload);
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
        }

        private void EventHandlerSignalR(object sender, BasicDeliverEventArgs e)
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

                watcherHub.Clients.Group("Tenant_" + tenantRegistryId)
                    .SendAsync("ReceiveMessage", "RealTime", bodyString);
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
        }

        private void ConnectToDatabaseNotifications()
        {
            try
            {
                var connection = new NpgsqlConnection(dynamicEnvironment.AppSettings("ConnectionString"));
                try
                {
                    connection.Open();

                    connection.Notification += (_, e)
                        => EventHandlerDatabase(e.Payload);

                    using (var cmd = new NpgsqlCommand("LISTEN activation", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    while (true)
                    {
                        connection.Wait();
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Streaming Activations Database: Has created an exception as {ex}.");
                }
                finally
                {
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                log.Error("Dispatch to SignalR: Error making connections for the Activation Watcher relay " + ex +
                          ".");
            }
        }

        private void ConnectToAmqp()
        {
            try
            {
                rabbitMqChannel.ExchangeDeclare("jubeActivations", ExchangeType.Fanout);

                var rabbitMqQueueName = rabbitMqChannel.QueueDeclare();
                rabbitMqChannel.QueueBind(rabbitMqQueueName, "jubeActivations", "");

                var consumer = new EventingBasicConsumer(rabbitMqChannel);
                consumer.Received += EventHandlerSignalR;

                rabbitMqChannel.BasicConsume(rabbitMqQueueName, true, consumer);
            }
            catch (Exception ex)
            {
                log.Error("Dispatch to SignalR: Error making connections for the Activation Watcher relay " + ex +
                          ".");
            }
        }

        private void FromDbContext()
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Data Connection DbContext: Is about to attempt construction of database context with {dynamicEnvironment.AppSettings("ConnectionString")}.");
            }

            var builder = new LinqToDbConnectionOptionsBuilder();
            builder.UsePostgreSQL(dynamicEnvironment.AppSettings("ConnectionString"));
            var connection = builder.Build<DbContext>();

            var dbContext = new DbContext(connection);

            if (log.IsInfoEnabled)
            {
                log.Info("Data Connection DbContext: Database context has been constructed.  Returning database context.");
            }

            var activationWatcherRepository = new ActivationWatcherRepository(dbContext);

            var lastActivationWatcher = activationWatcherRepository.GetLast();
            var lastActivationWatcherId = 0;

            if (lastActivationWatcher != null)
            {
                lastActivationWatcherId = lastActivationWatcher.Id;
            }

            while (!Stopping)
            {
                foreach (var activationWatcher in activationWatcherRepository.GetAllSinceId(lastActivationWatcherId,
                             100))
                {
                    lastActivationWatcherId = activationWatcher.Id;

                    var stringRepresentationOfObj = JsonConvert.SerializeObject(activationWatcher,
                        new JsonSerializerSettings
                        {
                            ContractResolver = contractResolver
                        });

                    watcherHub.Clients.Group("Tenant_" + 1)
                        .SendAsync("ReceiveMessage", "RealTime", stringRepresentationOfObj);
                }

                Thread.Sleep(Int32.Parse(dynamicEnvironment.AppSettings("WaitPollFromActivationWatcherTable")));
            }
        }
    }
}
