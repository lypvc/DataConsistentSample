﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IntegrationEvents;
using MassTransit;
using Microservice_Ship.EventHandlers;
using Microservice_Ship.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microservice_Ship
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
            services.AddSingleton("server=.;database=OrderDB;uid=sa;pwd=1");
            services.AddTransient<IShipRepository, ShipRepository>();
            StartESB(services);
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }

        void StartESB(IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();

            var shipRepository = provider.GetService<IShipRepository>();
       
            var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var host = cfg.Host(new Uri("rabbitmq://localhost"), hst =>
                {
                    hst.Username("guest");
                    hst.Password("guest");
                });
                cfg.ReceiveEndpoint(host, "ship_order", e =>
                {                 
                    e.Consumer(()=>new ShiperOrderEventHandler(shipRepository));                  
                });
            });
            bus.Start();

            services.AddSingleton(bus);

        }
    }
}
