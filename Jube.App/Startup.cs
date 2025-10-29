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
    using System.IdentityModel.Tokens.Jwt;
    using System.Net;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;
    using Cache;
    using Code;
    using Code.Jube.WebApp.Code;
    using Code.signalr;
    using Code.WatcherDispatch;
    using DynamicEnvironment;
    using Engine.Invoke;
    using FluentMigrator.Runner;
    using log4net;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authentication.Negotiate;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.OpenApi.Models;
    using Middlewares;
    using Migrations.Baseline;
    using Newtonsoft.Json.Serialization;
    using Npgsql;
    using RabbitMQ.Client;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var contractResolver = AddSingletonForDefaultContractResolver(services);
            var dynamicEnvironment = AddSingletonForDynamicEnvironmentAndLogging(services, out var log);

            ValidateConnectionToPostgres(dynamicEnvironment.AppSettings("ConnectionString"), log);

            var seeded = AddSingletonForRandomSeed(services);
            var pendingEntityAnalysisModelInvoke = AddSingletonForPendingEntityAnalysisModelInvoke(services);

            var cacheService = AddSingletonForCacheService(services, dynamicEnvironment, log);
            var rabbitMqChannel = AddSingletonForRabbitMqChannel(services, dynamicEnvironment, log);

            AddSingletonForEngine(services, dynamicEnvironment, log, seeded, rabbitMqChannel, cacheService, pendingEntityAnalysisModelInvoke, contractResolver);
            AddSingletonForIdentity(services);
            ConfigureAuthentication(services, dynamicEnvironment);
            AddGenericServicesRequired(services);
            AddSwagger(services);
            AddSingletonRelayToBeInstantiatedInConfigureServices(services);
            WriteWelcomeMessageToConsole();
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

        private static void AddSingletonRelayToBeInstantiatedInConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<Relay>();
        }

        private static void AddGenericServicesRequired(IServiceCollection services)
        {

            services.AddAuthorization();
            services.AddRazorPages();
            services.AddHttpContextAccessor();
            services.AddControllers();
            services.AddMvc();
            services.AddSignalR();
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
            var jwtValidAudience = dynamicEnvironment.AppSettings("JWTValidAudience");
            var jwtValidIssuer = dynamicEnvironment.AppSettings("JWTValidIssuer");
            var jwtKey = dynamicEnvironment.AppSettings("JWTKey");

            if (dynamicEnvironment.AppSettings("NegotiateAuthentication")
                .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                    .AddNegotiate();

                services.AddAuthorization(options => { options.FallbackPolicy = options.DefaultPolicy; });
            }
            else
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.AutomaticRefreshInterval = TimeSpan.FromMinutes(5);
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ClockSkew = TimeSpan.Zero,
                        NameClaimType = ClaimTypes.Name,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidAudience = jwtValidAudience,
                        ValidIssuer = jwtValidIssuer,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            if (DateTime.UtcNow.AddMinutes(10) < context.SecurityToken.ValidTo)
                            {
                                return Task.CompletedTask;
                            }

                            var token = Jwt.CreateToken(context.Principal?.Identity?.Name,
                                jwtKey,
                                jwtValidIssuer,
                                jwtValidAudience
                            );

                            var cookieOptions = new CookieOptions
                            {
                                Expires = DateTime.Now.AddMinutes(15),
                                HttpOnly = true
                            };

                            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                            context.Response.Headers.Append("authentication",
                                tokenString);

                            context.Response.Cookies.Append("authentication", tokenString
                                , cookieOptions);

                            return Task.CompletedTask;
                        }
                    };
                });
            }
        }

        private static void AddSingletonForEngine(IServiceCollection services, DynamicEnvironment dynamicEnvironment, ILog log, Random seeded, IModel rabbitMqChannel, CacheService cacheService, ConcurrentQueue<EntityAnalysisModelInvoke> pendingEntityAnalysisModelInvoke, DefaultContractResolver contractResolver)
        {

            if (!dynamicEnvironment.AppSettings("EnableEngine").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            
            var engine = new Engine.Program(dynamicEnvironment, log, seeded, rabbitMqChannel, cacheService,
                pendingEntityAnalysisModelInvoke, contractResolver);
            services.AddSingleton(engine);
        }

        private static IModel AddSingletonForRabbitMqChannel(IServiceCollection services, DynamicEnvironment dynamicEnvironment, ILog log)
        {

            IModel rabbitMqChannel = null;
            if (dynamicEnvironment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rabbitMqChannel = ConnectToRabbitMqChannel(services, log, dynamicEnvironment.AppSettings("AMQPUri"));
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        "Start: No connection to AMQP is being made.  AMQP will be bypassed throughout the application.");
                }
            }
            return rabbitMqChannel;
        }

        private static CacheService AddSingletonForCacheService(IServiceCollection services, DynamicEnvironment dynamicEnvironment, ILog log)
        {

            var cacheService =
                ConnectToRedis(dynamicEnvironment.AppSettings("RedisConnectionString"),
                    dynamicEnvironment.AppSettings("ConnectionString"),
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

            cacheService.InstantiateRepositories().GetAwaiter().GetResult();
            services.AddSingleton(cacheService);
            return cacheService;
        }

        private static ConcurrentQueue<EntityAnalysisModelInvoke> AddSingletonForPendingEntityAnalysisModelInvoke(IServiceCollection services)
        {

            var pendingEntityAnalysisModelInvoke = new ConcurrentQueue<EntityAnalysisModelInvoke>();
            services.AddSingleton(pendingEntityAnalysisModelInvoke);
            return pendingEntityAnalysisModelInvoke;
        }
        private static Random AddSingletonForRandomSeed(IServiceCollection services)
        {

            var seeded = new Random(Guid.NewGuid().GetHashCode());
            services.AddSingleton(seeded);
            return seeded;
        }

        private static DynamicEnvironment AddSingletonForDynamicEnvironmentAndLogging(IServiceCollection services, out ILog log)
        {

            var dynamicEnvironment = new DynamicEnvironment();
            log = dynamicEnvironment.Log;

            services.AddSingleton(log);
            services.AddSingleton(dynamicEnvironment);
            return dynamicEnvironment;
        }

        private static DefaultContractResolver AddSingletonForDefaultContractResolver(IServiceCollection services)
        {

            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };
            services.AddSingleton(contractResolver);
            return contractResolver;
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

                    Task.Delay(6000).Wait();
                }
            }

            throw new Exception($"Could not connect to Postgres after {retryConnectionToPostgres}.");
        }

        private static IModel ConnectToRabbitMqChannel(IServiceCollection services, ILog log,
            string amqpUrl)
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
                        Uri = uri
                    };
                    var rabbitMqConnection = rabbitMqConnectionFactory.CreateConnection();
                    services.AddSingleton(rabbitMqConnection);

                    if (log.IsInfoEnabled)
                    {
                        log.Info("Start: Has made a connection to AMQP Uri " + amqpUrl + "");
                    }

                    var rabbitMqChannel = rabbitMqConnection.CreateModel();
                    rabbitMqChannel.QueueDeclare("jubeNotifications", false, false, false, null);
                    rabbitMqChannel.QueueDeclare("jubeInbound", false, false, false, null);
                    rabbitMqChannel.ExchangeDeclare("jubeActivations", ExchangeType.Fanout);
                    rabbitMqChannel.ExchangeDeclare("jubeOutbound", ExchangeType.Fanout);

                    services.AddSingleton(rabbitMqChannel);

                    return rabbitMqChannel;
                }
                catch (Exception ex)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Start: Error making a connection to AMQP Uri after {i} attempts " +
                                 amqpUrl + " with error " + ex);
                    }

                    Task.Delay(3000).Wait();
                }
            }

            throw new Exception($"Could not connect to RabbitMQ after {retryRabbitMqConnection} attempts.");
        }

        private static CacheService ConnectToRedis(string redisConnectionString, string postgresConnectionString,
            bool localCache, bool localCacheFill, long localCacheBytes, bool messagePackCompression, bool storePayloadCountsAndBytes,bool publishSubscribe, ILog log)
        {
            const int retryRedisConnectionRetry = 10;
            for (var i = 0; i < retryRedisConnectionRetry; i++)
            {
                try
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info("Start: Is going to make a connection to Redis Endpoints string showing " +
                                 "endpoints and port seperated by :,  then combined seperated by comma " +
                                 "for example localhost:1234,localhost4321.  Value for parsing is " +
                                 redisConnectionString + "");
                    }

                    var cacheService = new CacheService(redisConnectionString,
                        postgresConnectionString, localCache, localCacheFill, localCacheBytes, messagePackCompression, storePayloadCountsAndBytes, publishSubscribe,log);

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

                    Task.Delay(1500).Wait();
                }
            }

            throw new Exception($"Could not connect to Redis after {retryRedisConnectionRetry} attempts.");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            DynamicEnvironment dynamicEnvironment)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
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
            }

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
                appBuilder => appBuilder.UseTransposeJwtFromCookieToHeaderMiddleware()
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

            app.UseRouting();

            app.UseWhen(
                httpContext =>
                    !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                appBuilder => appBuilder.UseAuthentication()
            );

            app.UseWhen(
                httpContext =>
                    !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                appBuilder => appBuilder.UseAuthorization()
            );

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapHub<WatcherHub>("/watcherHub");
            });

            app.StartRelay();
            app.StartEngine();
        }

        private static void RunFluentMigrator(DynamicEnvironment dynamicEnvironment,
            CacheService cacheService, ILog log)
        {
            var serviceCollection = new ServiceCollection().AddFluentMigratorCore()
                .AddSingleton(dynamicEnvironment)
                .AddSingleton(cacheService)
                .AddSingleton(log)
                .ConfigureRunner(rb => rb
                    .AddPostgres11_0()
                    .WithGlobalConnectionString(dynamicEnvironment.AppSettings("ConnectionString"))
                    .ScanIn(typeof(AddActivationWatcherTableIndex).Assembly).For.Migrations())
                .BuildServiceProvider(false);

            using var scope = serviceCollection.CreateScope();
            var runner = serviceCollection.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
        }
    }
}
