﻿using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Steeltoe.CircuitBreaker.Hystrix;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.MySql.EFCore;
using Timesheets;
using Pivotal.Discovery.Client;
using Steeltoe.Common.Discovery;

namespace TimesheetsServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            services.AddDbContext<TimeEntryContext>(options => options.UseMySql(Configuration));
            services.AddScoped<ITimeEntryDataGateway, TimeEntryDataGateway>();
            
            services.AddSingleton<IProjectClient>(sp =>
            {
             var handler = new DiscoveryHttpClientHandler(sp.GetService<IDiscoveryClient>());
             var httpClient = new HttpClient(handler, false)
                {
                    BaseAddress = new Uri(Configuration.GetValue<string>("REGISTRATION_SERVER_ENDPOINT"))
                };

                //return new ProjectClient(httpClient);
                var logger = sp.GetService<ILogger<ProjectClient>>();
                return new ProjectClient(httpClient, logger);
            });
            services.AddDiscoveryClient(Configuration);
            services.AddHystrixMetricsStream(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMvc();
            app.UseDiscoveryClient();
            app.UseHystrixMetricsStream();
            app.UseHystrixRequestContext();            
        }
    }
}