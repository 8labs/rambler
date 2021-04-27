namespace Rambler.Server
{
    using Database;
    using Database.Models;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.StaticFiles;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Logging;
    using Options;
    using Rambler.Contracts;
    using Rambler.Contracts.Responses;
    using Rambler.Contracts.Server;
    using Socket;
    using State;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Utility;
    using WebService.Services;

    /// <summary>
    /// quick hack for a shared boostrapper
    /// Mainly so it can share with the end to end tests
    /// </summary>
    public class Startup
    {
        private IEnumerable<Type> reqProcessorTypes;
        private IEnumerable<Type> resProcessorTypes;

        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            if (env != null)
            {
                var builder = new ConfigurationBuilder()
                   .SetBasePath(env.ContentRootPath)
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

                builder.AddEnvironmentVariables();
                Configuration = builder.Build();
            }
        }

        public void ConfigureChatServices(IServiceCollection services)
        {
            services
                //requests
                .AddSingleton<RequestDistributor>()
                .AddSingleton<IRequestPublisher>(x => x.GetService<RequestDistributor>())
                .AddSingleton<IRequestDistributor>(x => x.GetService<RequestDistributor>())

                //responses
                .AddSingleton<ResponseDistributor>()
                .AddSingleton<IResponsePublisher>(x => x.GetService<ResponseDistributor>())
                .AddSingleton<IResponseDistributor>(x => x.GetService<ResponseDistributor>())

                //dealing with json
                .AddSingleton<JsonSocketSubscriber>()
                .AddSingleton<ISocketSubscriber, JsonSocketSubscriber>(x => x.GetService<JsonSocketSubscriber>())
                .AddSingleton<JsonPublisher>()
                .AddSingleton<IPublisher, JsonPublisher>(x => x.GetService<JsonPublisher>())
                .AddSingleton<IAuthorize, Authorizor>()

                //all the caches/server bits
                .AddSingleton<StateServer>()
                .AddSingleton<StateMutator>()
                .AddSingleton<NodeServer>()
                .AddSingleton<StateCache>()
                .AddSingleton<SocketSubscriptions>()

                // BOTS EVERYWHERE
                .AddSingleton<BotService>()
                .AddTransient<InitializeBots>()

                .AddTransient<ICaptchaService, CaptchaService>()

                .AddSingleton<SocketMiddleware>()
                .AddSingleton<DnsBlackListService>()
                ;

            //register all the various processors
            var assembly = typeof(Startup).GetTypeInfo().Assembly;
            reqProcessorTypes = assembly.GetAllTypesImplementingOpenGenericInterface(typeof(IRequestProcessor<>));
            resProcessorTypes = assembly.GetAllTypesImplementingOpenGenericInterface(typeof(IResponseProcesor<>));

            foreach (var proc in reqProcessorTypes)
            {
                services.AddTransient(proc);
            }

            foreach (var proc in resProcessorTypes)
            {
                services.AddSingleton(proc);
            }
        }

        public void ConfigureWebApiServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddLogging();

            services.AddTransient<EmailService>();

            services.AddCors(Options => Options.AddPolicy(
                "CorsPolicy",
                builder => builder
                    .AllowAnyOrigin()
                    //.WithHeaders("authorization", "accept", "content-type", "origin")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()));

            services
                .AddMvc()
                // switch back to CamelCase for the json serialization
                .AddJsonOptions(options =>
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver());


            services.AddDbContext<ApplicationDbContext>(builder =>
            {
                builder.UseNpgsql(connectionString, options =>
                    options.MigrationsAssembly(migrationsAssembly));
            }, ServiceLifetime.Scoped);

            services
                .AddIdentity<ApplicationUser, ApplicationRole>(o =>
                {
                    o.Password.RequireDigit = false;
                    o.Password.RequireLowercase = false;
                    o.Password.RequireUppercase = false;
                    o.Password.RequireNonAlphanumeric = false;
                    o.Password.RequiredLength = 3;
                    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                    o.Lockout.MaxFailedAccessAttempts = 5;
                    o.Lockout.AllowedForNewUsers = true;

                    // User settings
                    o.User.RequireUniqueEmail = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            var siteOptions = Configuration.GetSection("Site").Get<SiteOptions>();

            services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                options.Cookie = new CookieBuilder
                {
                    Domain = siteOptions.CookieDomain,
                    HttpOnly = true,
                    Name = siteOptions.CookieIdentity,
                    Path = "/",
                    SameSite = SameSiteMode.None,
                    SecurePolicy = CookieSecurePolicy.None,
                    Expiration = TimeSpan.FromDays(30),
                };
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.Events = new CookieAuthenticationEvents()
                {
                    OnRedirectToLogin = (ctx) =>
                    {
                        ctx.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = (ctx) =>
                    {
                        ctx.Response.StatusCode = 403;
                        return Task.CompletedTask;
                    }
                };
                options.SlidingExpiration = true;
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder => builder
                    .AddConsole()
                    .AddConfiguration(Configuration.GetSection("Logging"))
                    );

            services.AddOptions();
            services.Configure<Options.SiteOptions>(Configuration.GetSection("Site"));
            services.Configure<Options.TokenOptions>(Configuration.GetSection("Token"));
            services.Configure<Options.EmailOptions>(Configuration.GetSection("Email"));
            services.Configure<Options.DnsBlackListOptions>(Configuration.GetSection("DnsBlackList"));

            var facebook = Configuration.GetSection("Facebook").Get<FacebookAuthOptions>();
            var google = Configuration.GetSection("Google").Get<GoogleAuthOptions>();
            services.AddAuthentication();
            //.AddFacebook(o =>
            //{
            //    o.ClientId = facebook.ClientId;
            //    o.ClientSecret = facebook.ClientSecret;
            //})
            //.AddGoogle(o =>
            //{
            //    o.ClientId = google.ClientId;
            //    o.ClientSecret = google.ClientSecret;
            //});

            ConfigureChatServices(services);
            ConfigureWebApiServices(services);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IServiceProvider provider)
        {
            var log = loggerFactory.CreateLogger<Startup>();



            Boot(provider);
            ConfigureChat(app, provider);
            ConfigureWebApi(app, env, provider);
            ConfigureClientHosting(app, log);
        }

        public void ConfigureChat(IApplicationBuilder app, IServiceProvider provider)
        {
            var test = provider.GetService<Options.TokenOptions>();

            //spin up the servers
            provider.GetService<NodeServer>().Start();
            provider.GetService<BotService>().Start();

            //wire this mess up
            app.UseWebSockets();
            var middleware = provider.GetService<SocketMiddleware>();
            middleware.Use(app);
        }

        public void ConfigureWebApi(IApplicationBuilder appBuilder, IHostingEnvironment env, IServiceProvider provider)
        {
            appBuilder.Map("/api", app =>
            {
                // cors required yay :|
                app.UseCors("CorsPolicy");

                //required to match the correct origin for the various auth callbacks
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });
                app.UseHttpMethodOverride();

                //some general help for dev errors
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                    app.UseDatabaseErrorPage();
                }
                else
                {
                    //app.UseExceptionHandler("/Home/Error");
                }

                app.UseAuthentication();
                app.UseMvcWithDefaultRoute();
            });
        }

        public void Boot(IServiceProvider provider)
        {
            AutoAddAllMessageTypes(provider.GetService<JsonSocketSubscriber>());
            AddRequestProcessors(reqProcessorTypes, provider);
            AddResponseProcessors(resProcessorTypes, provider);
        }

        private void ConfigureClientHosting(IApplicationBuilder app, ILogger log)
        {
            var client = Environment.GetEnvironmentVariable("RAMBLER_HOSTCLIENT");
            if (!string.IsNullOrEmpty(client))
            {
                var dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "../Rambler.Client/web/build"));
                log.LogInformation("Hosting client at: " + dir.FullName);

                var provider = new FileExtensionContentTypeProvider();
                provider.Mappings[".svg"] = "image/svg+xml";

                app.UseDefaultFiles();
                app.UseStaticFiles(new StaticFileOptions()
                {
                    FileProvider = new PhysicalFileProvider(dir.FullName),
                    //RequestPath = new PathString("/web"),
                    ContentTypeProvider = provider,
                });

            }
        }

        private void AddResponseProcessors(IEnumerable<Type> types, IServiceProvider provider)
        {
            var server = provider.GetService<NodeServer>();
            //doing some evil reflection shit here for now
            MethodInfo method = typeof(NodeServer).GetMethod("AddProcessor");
            foreach (var type in types)
            {
                var rType = type.GetTypeInfo().GetInterfaces().First().GetGenericArguments().First();
                MethodInfo genericMethod = method.MakeGenericMethod(rType);
                genericMethod.Invoke(server, new object[] { provider.GetService(type) });
            }
        }

        private void AddRequestProcessors(IEnumerable<Type> types, IServiceProvider provider)
        {
            var server = provider.GetService<StateServer>();
            //doing some evil reflection shit here for now
            MethodInfo method = typeof(StateServer).GetMethod("AddProcessor");
            foreach (var type in types)
            {
                var rType = type.GetTypeInfo().GetInterfaces().First().GetGenericArguments().First();
                MethodInfo genericMethod = method.MakeGenericMethod(rType);
                genericMethod.Invoke(server, new object[] { type });
            }
        }

        private void AutoAddAllMessageTypes(JsonSocketSubscriber json)
        {
            //wire up the supported messages using a hack for now
            //wiring will likely change when switching to protobuf or whatever

            var types = typeof(IRequest)
                .GetTypeInfo()
                .Assembly
                .GetTypes()
                .Where(t => t.GetAttribute<MessageKey>() != null)
                .Where(t => t.FullName.Contains("Requests"));

            //wheee generic reflection
            MethodInfo method = typeof(JsonSocketSubscriber).GetMethod("AddMessageType");
            foreach (var type in types)
            {
                MethodInfo genericMethod = method.MakeGenericMethod(type);
                genericMethod.Invoke(json, new object[] { type.GetAttribute<MessageKey>().Key });
            }
        }
    }
}
