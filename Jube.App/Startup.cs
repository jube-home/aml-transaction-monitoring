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

namespace Jube.App
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using ApiTokensCache;
    using Cache;
    using Cache.Redis.Callback;
    using Code;
    using Code.Jube.WebApp.Code;
    using Code.signalr;
    using Code.WatcherDispatch;
    using DynamicEnvironment;
    using Engine;
    using Engine.Helpers;
    using FluentMigrator.Runner;
    using log4net;
    using Microsoft.AspNetCore.Authentication.Negotiate;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.OpenApi.Models;
    using Middlewares;
    using Middlewares.Extensions;
    using Middlewares.Models;
    using Migrations.Baseline;
    using Newtonsoft.Json.Serialization;
    using Npgsql;
    using RabbitMQ.Client;
    using TaskCancellation;
    using TaskCancellation.Interfaces;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var cancellationTokenProvider = AddSingletonForCancellationToken(services);
            var taskCoordinator = AddSingletonForTaskCoordinator(services, cancellationTokenProvider);
            var contractResolver = AddSingletonForJsonSerializationHelper(services);

            var dynamicEnvironment = AddSingletonForDynamicEnvironmentAndLogging(services, out var log);
            ConfigureThreadPool(dynamicEnvironment, log);
            ValidateConnectionToPostgres(dynamicEnvironment.AppSettings("ConnectionString"), log);

            var callbacks = AddSingletonForCallbacks(services);

            var cacheService = AddSingletonForCacheService(services, callbacks, Int32.Parse(dynamicEnvironment.AppSettings("CallbackTimeout") ?? "10000"),
                taskCoordinator, dynamicEnvironment, log);

            AddSingletonForTokensCache(services, log, dynamicEnvironment, cacheService, taskCoordinator);

            var rabbitMqConnection = AddSingletonForRabbitMqConnection(services, dynamicEnvironment, log);

            AddSingletonForEngine(services, dynamicEnvironment, log, rabbitMqConnection, cacheService, contractResolver, taskCoordinator);
            AddSingletonForIdentity(services);
            ConfigureAuthentication(services, dynamicEnvironment);
            AddGenericServicesRequired(services, dynamicEnvironment);
            AddSwagger(services);
            AddSingletonRelayToBeInstantiatedInConfigureServices(services, dynamicEnvironment);
            WriteWelcomeMessageToConsole();
        }
        
        private static void AddSingletonForTokensCache(IServiceCollection services, ILog log, DynamicEnvironment dynamicEnvironment, CacheService cacheService, TaskCoordinator taskCoordinator)
        {

            var apiTokensCache = new ApiTokensCache(log, dynamicEnvironment, cacheService);
            _ = taskCoordinator.RunAsync("InstantiateApiTokensCache", _ => apiTokensCache.StartAsync(taskCoordinator));
            services.AddSingleton(apiTokensCache);
        }

        private static TaskCoordinator AddSingletonForTaskCoordinator(IServiceCollection services, ICancellationTokenProvider cancellationTokenProvider)
        {
            var taskCoordinator = new TaskCoordinator(cancellationTokenProvider);
            services.AddSingleton(taskCoordinator);
            return taskCoordinator;
        }

        private static ICancellationTokenProvider AddSingletonForCancellationToken(IServiceCollection services)
        {
            ICancellationTokenProvider cancellationTokenProvider = new CancellationTokenProvider();
            services.AddSingleton(cancellationTokenProvider);
            return cancellationTokenProvider;
        }

        private static void WriteWelcomeMessageToConsole()
        {

            Console.WriteLine(@"Copyright (C) 2022-present Jube Holdings Limited.");
            Console.WriteLine(@"");
            Console.WriteLine(@"This software is Jube.  Welcome.");
            Console.WriteLine(@"");
            Console.Write(
                @"Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.");
            Console.WriteLine(
                @"Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.");
            Console.WriteLine(@"");
            Console.WriteLine(
                @"You should have received a copy of the GNU Affero General Public License along with Jube™. If not, see <https://www.gnu.org/licenses/>.");

            Console.WriteLine(@"");
            Console.WriteLine(
                @"If you are seeing this message it means that database migrations have completed and the database is fully configured with required Tables, Indexes and Constraints.");
            Console.WriteLine(@"");
            Console.WriteLine(
                @"Comprehensive documentation is available via https://github.com/jube-home/aml-transaction-monitoring.");
            Console.WriteLine(@"");
            Console.WriteLine(
                @"Use a web browser (e.g. Chrome) to navigate to the user interface via default endpoint https://<ASPNETCORE_URLS Environment Variable>/ (for example https://127.0.0.1:5001/ given ASPNETCORE_URLS=https://127.0.0.1:5001/). The default user name \ password is 'Administrator' \ 'Administrator' but will need to be changed on first use.  Availability of the user interface may be a few moments after this messages as the Kestrel web server starts and endpoint routing is established.");
            Console.WriteLine(@"");
            Console.WriteLine(
                @"The default endpoint for posting example transaction payload is https://<ASPNETCORE_URLS Environment Variable>/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc/.Example JSON payload is available in the documentation via at https://jube-home.github.io/aml-transaction-monitoring/Configuration/Models/Models/.");
            Console.WriteLine();
        }

        private static void AddSingletonRelayToBeInstantiatedInConfigureServices(IServiceCollection services, DynamicEnvironment dynamicEnvironment)
        {
            if (!dynamicEnvironment.AppSettings("StreamingActivationWatcher").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            services.AddSingleton<Relay>();
        }

        private static void AddGenericServicesRequired(IServiceCollection services, DynamicEnvironment dynamicEnvironment)
        {

            services.AddAuthorization();
            services.AddRazorPages();
            services.AddHttpContextAccessor();
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                };
            });
            services.AddMvc();

            if (dynamicEnvironment.AppSettings("RedisBackplane").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSignalR().AddStackExchangeRedis(dynamicEnvironment.AppSettings("RedisConnectionString"));
            }
            else
            {
                services.AddSignalR();
            }

            services.AddEndpointsApiExplorer();
        }

        private static void AddSingletonForIdentity(IServiceCollection services)
        {
            services.AddTransient<IUserStore<ApplicationUser>, UserStore>();
            services.AddTransient<IRoleStore<ApplicationRole>, RoleStore>();
            services.AddIdentity<ApplicationUser, ApplicationRole>().AddDefaultTokenProviders();
        }

        private static void AddSwagger(IServiceCollection services)
        {

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Jube.App.Api",
                    Version = "v1"
                });
                c.CustomSchemaIds(type => type.FullName);
                c.OperationFilter<AuthorizationHeaderParameterOperationFilter>();
            });
        }

        private static void ConfigureAuthentication(IServiceCollection services, DynamicEnvironment dynamicEnvironment)
        {
            if (dynamicEnvironment.AppSettings("NegotiateAuthentication")
                .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                    .AddNegotiate();

                services.AddAuthorization(options => { options.FallbackPolicy = options.DefaultPolicy; });
            }
            else
            {
                var jwtValidAudience = dynamicEnvironment.AppSettings("JWTValidAudience");
                var jwtValidIssuer = dynamicEnvironment.AppSettings("JWTValidIssuer");
                var jwtKey = dynamicEnvironment.AppSettings("JWTKey");

                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Hybrid";
                        options.DefaultChallengeScheme = "Hybrid";
                        options.DefaultScheme = "Hybrid";
                    })
                    .AddScheme<HybridAuthOptions, HybridAuthHandler>("Hybrid", options =>
                    {
                        options.JwtKey = jwtKey;
                        options.JwtValidIssuer = jwtValidIssuer;
                        options.JwtValidAudience = jwtValidAudience;
                    });
            }
        }

        private static void AddSingletonForEngine(IServiceCollection services
            , DynamicEnvironment dynamicEnvironment, ILog log, IConnection rabbitMqConnection,
            CacheService cacheService, JsonSerializationHelper jsonSerializationHelper, ITaskCoordinator taskCoordinator)
        {
            if (!dynamicEnvironment.AppSettings("EnableEngine").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var engine = new Engine(dynamicEnvironment, log, rabbitMqConnection, cacheService, jsonSerializationHelper, taskCoordinator, dynamicEnvironment.AppSettings("ReportConnectionString"));

            services.AddSingleton(engine);
        }

        private static IConnection AddSingletonForRabbitMqConnection(IServiceCollection services, DynamicEnvironment dynamicEnvironment, ILog log)
        {

            IConnection rabbitMqConnection = null;
            if (dynamicEnvironment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rabbitMqConnection = ConnectToRabbitMqChannel(services, log, dynamicEnvironment.AppSettings("AMQPUri"), Int32.Parse(dynamicEnvironment.AppSettings("AMQPHeartbeatSeconds") ?? "30"));
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        "Start: No connection to AMQP is being made.  AMQP will be bypassed throughout the application.");
                }
            }
            return rabbitMqConnection;
        }

        private static ConcurrentDictionary<Guid, TaskCompletionSource<Callback>> AddSingletonForCallbacks(IServiceCollection services)
        {
            var callbacks = new ConcurrentDictionary<Guid, TaskCompletionSource<Callback>>();
            services.AddSingleton(callbacks);

            return callbacks;
        }


        private static CacheService AddSingletonForCacheService(IServiceCollection services,
            ConcurrentDictionary<Guid, TaskCompletionSource<Callback>> callbacks, int callbackTimeout,
            TaskCoordinator taskCoordinator, DynamicEnvironment dynamicEnvironment, ILog log)
        {
            var cacheService =
                ConnectToRedis(dynamicEnvironment.AppSettings("RedisConnectionString"),
                    dynamicEnvironment.AppSettings("ConnectionString"),
                    callbacks,
                    callbackTimeout,
                    dynamicEnvironment.AppSettings("LocalCache").Equals("True", StringComparison.OrdinalIgnoreCase),
                    dynamicEnvironment.AppSettings("LocalCacheFill").Equals("True", StringComparison.OrdinalIgnoreCase),
                    Int64.Parse(dynamicEnvironment.AppSettings("LocalCacheBytes")),
                    dynamicEnvironment.AppSettings("RedisMessagePackCompression").Equals("True", StringComparison.OrdinalIgnoreCase),
                    dynamicEnvironment.AppSettings("RedisStorePayloadCountsAndBytes").Equals("True", StringComparison.OrdinalIgnoreCase),
                    dynamicEnvironment.AppSettings("RedisPublishSubscribeEvents").Equals("True", StringComparison.OrdinalIgnoreCase),
                    log);

            if (dynamicEnvironment.AppSettings("EnableMigration").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                RunFluentMigrator(dynamicEnvironment, cacheService, log);
            }

            cacheService.InstantiateRepositoriesTask = taskCoordinator.RunAsync("InstantiateRepositoriesAsync", _ => cacheService.StartAsync(taskCoordinator));

            services.AddSingleton(cacheService);

            return cacheService;
        }

        private static DynamicEnvironment AddSingletonForDynamicEnvironmentAndLogging(IServiceCollection services, out ILog log)
        {

            var dynamicEnvironment = new DynamicEnvironment();
            log = dynamicEnvironment.Log;

            services.AddSingleton(log);
            services.AddSingleton(dynamicEnvironment);
            return dynamicEnvironment;
        }

        private static JsonSerializationHelper AddSingletonForJsonSerializationHelper(IServiceCollection services)
        {

            var jsonSerializationHelper = new JsonSerializationHelper();
            services.AddSingleton(jsonSerializationHelper);
            return jsonSerializationHelper;
        }

        private static void ValidateConnectionToPostgres(string connectionString, ILog log)
        {
            const int retryConnectionToPostgres = 10;
            for (var i = 0; i < retryConnectionToPostgres; i++)
            {
                try
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info("Is attempting a connection validation for Postgres.");
                    }

                    var connection = new NpgsqlConnection(connectionString);
                    var command = new NpgsqlCommand("select true");
                    connection.Open();
                    command.Connection = connection;
                    command.ExecuteNonQuery();
                    connection.Close();

                    if (log.IsInfoEnabled)
                    {
                        log.Info("Postgres connection validated.");
                    }

                    return;
                }
                catch (Exception ex)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Could not connect to Postgres after {i} attempts for {ex.Message}.");
                    }

 #pragma warning disable VSTHRD002
                    Task.Delay(6000).Wait();
 #pragma warning restore VSTHRD002
                }
            }

            throw new Exception($"Could not connect to Postgres after {retryConnectionToPostgres}.");
        }

        private static IConnection ConnectToRabbitMqChannel(IServiceCollection services, ILog log,
            string amqpUrl, int heartbeat)
        {
            const int retryRabbitMqConnection = 10;
            for (var i = 0; i < retryRabbitMqConnection; i++)
            {
                try
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info("Start: Is going to make a connection to AMQP Uri " +
                                 amqpUrl + "");
                    }

                    var uri = new Uri(amqpUrl);
                    var rabbitMqConnectionFactory = new ConnectionFactory
                    {
                        Uri = uri,
                        RequestedHeartbeat = TimeSpan.FromSeconds(heartbeat)
                    };
                    var rabbitMqConnection = rabbitMqConnectionFactory.CreateConnection();
                    services.AddSingleton(rabbitMqConnection);

                    if (log.IsInfoEnabled)
                    {
                        log.Info("Start: Has made a connection to AMQP Uri " + amqpUrl + "");
                    }

                    services.AddSingleton(rabbitMqConnection);

                    return rabbitMqConnection;
                }
                catch (Exception ex)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Start: Error making a connection to AMQP Uri after {i} attempts " +
                                 amqpUrl + " with error " + ex);
                    }

 #pragma warning disable VSTHRD002
                    Task.Delay(3000).Wait();
 #pragma warning restore VSTHRD002
                }
            }

            throw new Exception($"Could not connect to RabbitMQ after {retryRabbitMqConnection} attempts.");
        }

        private static CacheService ConnectToRedis(string redisConnectionString,
            string postgresConnectionString,
            ConcurrentDictionary<Guid, TaskCompletionSource<Callback>> callbacks,
            int callbackTimeout,
            bool localCache, bool localCacheFill,
            long localCacheBytes,
            bool messagePackCompression,
            bool storePayloadCountsAndBytes,
            bool publishSubscribe, ILog log)
        {
            const int retryRedisConnectionRetry = 10;
            for (var i = 0; i < retryRedisConnectionRetry; i++)
            {
                try
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info("Start: Is going to make a connection to Redis Endpoints string showing " +
                                 "endpoints and port separated by :,  then combined separated by comma " +
                                 "for example localhost:1234,localhost4321.  Value for parsing is " +
                                 redisConnectionString + "");
                    }

                    var cacheService = new CacheService(redisConnectionString,
                        postgresConnectionString, callbacks,
                        callbackTimeout,
                        localCache, localCacheFill,
                        localCacheBytes, messagePackCompression, storePayloadCountsAndBytes, publishSubscribe, log);

                    if (log.IsInfoEnabled)
                    {
                        log.Info("Connected to Redis.  Returning connection for startup.");
                    }

                    return cacheService;
                }
                catch (Exception ex)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Can't make a connection to Redis after {i} attempt(s) for {ex.Message}.");
                    }

 #pragma warning disable VSTHRD002
                    Task.Delay(1500).Wait();
 #pragma warning restore VSTHRD002
                }
            }

            throw new Exception($"Could not connect to Redis after {retryRedisConnectionRetry} attempts.");
        }

 #pragma warning disable AsyncFixer03
 #pragma warning disable VSTHRD100
        public async void Configure(IApplicationBuilder app, IWebHostEnvironment env,
 #pragma warning restore VSTHRD100
 #pragma warning restore AsyncFixer03
            DynamicEnvironment dynamicEnvironment, ILog log)
        {
            try
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseMiddleware<TokenRefreshMiddleware>();

                var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
                app.UseWhen(context => context.Request.Path.StartsWithSegments("/api") &&
                                       lifetime.ApplicationStopping.IsCancellationRequested,
                    branch => branch.UseConditionalConnectionCloseWhenStopping());

                app.UseWhen(context => context.Request.Path.StartsWithSegments("/Account/Login"), appBuilder =>
                {
                    appBuilder.Use(async (context, next) =>
                    {
                        context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors 'none'");
                        await next.Invoke();
                    });
                });

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseHttpsRedirection()
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseExceptionHandler("/Error")
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder =>
                        appBuilder
                            .UseHsts()// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseStatusCodePages(context =>

                    {
                        var request = context.HttpContext.Request;
                        var response = context.HttpContext.Response;

                        if (response.StatusCode != (int)HttpStatusCode.Unauthorized)
                        {
                            return Task.CompletedTask;
                        }

                        if (!request.Path.StartsWithSegments("/api"))
                        {
                            response.Redirect("/Account/Login");
                        }

                        return Task.CompletedTask;
                    })
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.RequestTrackingMiddleware()
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseStaticFiles()
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseSwagger()
                );

                app.UseWhen(
                    httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                    appBuilder => appBuilder.UseSwaggerUI()
                );

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapRazorPages();
                    endpoints.MapControllers();
                    endpoints.MapHub<WatcherHub>("/watcherHub");
                });

                await app.StartRelayAsync().ConfigureAwait(false);
                await app.StartEngineAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Error in App Configure as {ex}");
            }
        }

        private static void RunFluentMigrator(DynamicEnvironment dynamicEnvironment,
            CacheService cacheService, ILog log)
        {
            string connectionString;
            if (dynamicEnvironment.AppSettings("MigrationConnectionString") != null)
            {
                connectionString = dynamicEnvironment.AppSettings("MigrationConnectionString");
            }
            else
            {
                connectionString = dynamicEnvironment.AppSettings("ConnectionString");

                if (log.IsWarnEnabled)
                {
                    log.Warn("No MigrationConnectionString Environment Variable available.");
                }
            }

            var serviceCollection = new ServiceCollection().AddFluentMigratorCore()
                .AddSingleton(dynamicEnvironment)
                .AddSingleton(cacheService)
                .AddSingleton(log)
                .ConfigureRunner(rb => rb
                    .AddPostgres11_0()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(AddActivationWatcherTableIndex).Assembly).For.Migrations())
                .BuildServiceProvider(false);

            using var scope = serviceCollection.CreateScope();
            var runner = serviceCollection.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
        }

        private void ConfigureThreadPool(DynamicEnvironment dynamicEnvironment, ILog log)
        {
            if (dynamicEnvironment.AppSettings("ThreadPoolManualControl")
                .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                ThreadPool.SetMinThreads(Int32.Parse(dynamicEnvironment.AppSettings("MinThreadPoolThreads")),
                    Int32.Parse(dynamicEnvironment.AppSettings("MinThreadPoolThreads")));

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Start: Set the min threads to {dynamicEnvironment.AppSettings("MinThreadPoolThreads")} from the configuration file.");
                }

                ThreadPool.SetMaxThreads(Int32.Parse(dynamicEnvironment.AppSettings("MaxThreadPoolThreads")),
                    Int32.Parse(dynamicEnvironment.AppSettings("MaxThreadPoolThreads")));

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Start: Set the max threads to {Int32.Parse(dynamicEnvironment.AppSettings("MaxThreadPoolThreads"))} from the configuration file.");
                }
            }
            else
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug("Start: No manual thread pool parameters have been set will configure based on CPU count and certain other estimates.");
                }

                var logicalCores = Environment.ProcessorCount;
                var workerThreads = logicalCores * 2;
                var ioThreads = logicalCores * 4;
                ThreadPool.SetMinThreads(workerThreads, ioThreads);
            }
        }
    }
}
