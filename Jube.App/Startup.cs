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

using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using Jube.App.Code;
using Jube.App.Code.Jube.WebApp.Code;
using Jube.App.Code.signalr;
using Jube.App.Code.WatcherDispatch;
using Jube.App.Middlewares;
using Jube.Engine.Invoke;
using Jube.Extensions;
using Jube.Migrations.Baseline;
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
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Jube.App
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };
            services.AddSingleton(contractResolver);

            var log = LogManager.GetLogger(typeof(ILog));
            services.AddSingleton(log);

            var dynamicEnvironment = new DynamicEnvironment.DynamicEnvironment(log);
            services.AddSingleton(dynamicEnvironment);

            Random seeded = new(Guid.NewGuid().GetHashCode());
            services.AddSingleton(seeded);

            var pendingEntityInvoke = new ConcurrentQueue<EntityAnalysisModelInvoke>();
            services.AddSingleton(pendingEntityInvoke);

            IDatabase redisDatabase = null;
            if (dynamicEnvironment.AppSettings("Redis").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                log.Info("Start: Is going to make a connection to Redis Endpoints string showing " +
                         "endpoints and port seperated by :,  then combined seperated by comma " +
                         "for example localhost:1234,localhost4321.  Value for parsing is " +
                         dynamicEnvironment.AppSettings("RedisEndpoints") + "");
                
                var redisConnection =
                    ConnectionMultiplexer.Connect(dynamicEnvironment.AppSettings("RedisConnectionString"));
                redisDatabase = redisConnection.GetDatabase();
                redisDatabase.HashSet("Connections", Dns.GetHostName(),
                    DateTime.Now.ToUnixTimeMilliSeconds());

                services.AddSingleton(redisDatabase);
            }

            IModel rabbitMqChannel = null;
            if (dynamicEnvironment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    log.Info("Start: Is going to make a connection to AMQP Uri " +
                             dynamicEnvironment.AppSettings("AMQPUri") + "");

                    var uri = new Uri(dynamicEnvironment.AppSettings("AMQPUri"));
                    var rabbitMqConnectionFactory = new ConnectionFactory {Uri = uri};
                    var rabbitMqConnection = rabbitMqConnectionFactory.CreateConnection();
                    services.AddSingleton(rabbitMqConnection);

                    log.Info("Start: Has made a connection to AMQP Uri " + dynamicEnvironment.AppSettings("AMQP") +
                             "");

                    rabbitMqChannel = rabbitMqConnection.CreateModel();
                    rabbitMqChannel.QueueDeclare("jubeNotifications", false, false, false, null);
                    rabbitMqChannel.QueueDeclare("jubeInbound", false, false, false, null);
                    rabbitMqChannel.ExchangeDeclare("jubeActivations", ExchangeType.Fanout);
                    rabbitMqChannel.ExchangeDeclare("jubeOutbound", ExchangeType.Fanout);

                    services.AddSingleton(rabbitMqChannel);
                }
                catch (Exception ex)
                {
                    log.Info("Start: Error making a connection to AMQP Uri " +
                             dynamicEnvironment.AppSettings("AMQP") + " with error " + ex);
                }
            }
            else
            {
                log.Info(
                    "Start: No connection to AMQP is being made.  AMQP will be bypassed throughout the application.");
            }

            if (dynamicEnvironment.AppSettings("EnableEngine").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                var engine = new Jube.Engine.Program(dynamicEnvironment, log, seeded, rabbitMqChannel, redisDatabase,
                    pendingEntityInvoke, contractResolver);
                services.AddSingleton(engine);
            }

            var jwtValidAudience = dynamicEnvironment.AppSettings("JWTValidAudience");
            var jwtValidIssuer = dynamicEnvironment.AppSettings("JWTValidIssuer");
            var jwtKey = dynamicEnvironment.AppSettings("JWTKey");

            if (dynamicEnvironment.AppSettings("EnableMigration").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                RunFluentMigrator(dynamicEnvironment);

                var cacheConnectionString = dynamicEnvironment.AppSettings("CacheConnectionString");
                if (cacheConnectionString != null)
                {
                    RunFluentMigrator(dynamicEnvironment);
                }
            }

            services.AddTransient<IUserStore<ApplicationUser>, UserStore>();
            services.AddTransient<IRoleStore<ApplicationRole>, RoleStore>();
            services.AddIdentity<ApplicationUser, ApplicationRole>().AddDefaultTokenProviders();

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
                                return Task.CompletedTask;

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

            services.AddAuthorization();
            services.AddRazorPages();
            services.AddHttpContextAccessor();
            services.AddControllers();
            services.AddMvc();
            services.AddSignalR();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "Jube.App.Api", Version = "v1"});
                c.CustomSchemaIds(type => type.FullName);
                c.OperationFilter<AuthorizationHeaderParameterOperationFilter>();
            });
            services.AddSingleton<Relay>();

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
            Console.WriteLine(@"Comprehensive documentation is available via https://github.com/jube-home/aml-transaction-monitoring.");
            Console.WriteLine(@"");
            Console.WriteLine(
                @"Use a web browser (e.g. Chrome) to navigate to the user interface via default endpoint https://<ASPNETCORE_URLS Environment Variable>/ (for example https://127.0.0.1:5001/ given ASPNETCORE_URLS=https://127.0.0.1:5001/). The default user name \ password is 'Administrator' \ 'Administrator' but will need to be changed on first use.  Availability of the user interface may be a few moments after this messages as the Kestrel web server starts and endpoint routing is established.");
            Console.WriteLine(@"");
            Console.WriteLine(
                @"The default endpoint for posting example transaction payload is https://<ASPNETCORE_URLS Environment Variable>/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc/.Example JSON payload is available in the documentation via at https://jube-home.github.io/jube/Configuration/Models/Models/.");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
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
                            .UseHsts() // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                );
            }

            app.UseWhen(
                httpContext =>
                    !httpContext.Request.Path.StartsWithSegments("/api/invoke", StringComparison.OrdinalIgnoreCase),
                appBuilder => appBuilder.UseStatusCodePages(context =>

                {
                    var request = context.HttpContext.Request;
                    var response = context.HttpContext.Response;

                    if (response.StatusCode == (int) HttpStatusCode.Unauthorized)
                    {
                        if (!request.Path.StartsWithSegments("/api"))
                        {
                            response.Redirect("/Account/Login");
                        }
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

        private static void RunFluentMigrator(DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            var serviceCollection = new ServiceCollection().AddFluentMigratorCore()
                .AddSingleton(dynamicEnvironment)
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