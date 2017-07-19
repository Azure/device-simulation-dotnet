// Copyright (c) Microsoft. All rights reserved.

using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService
{
    public class Startup
    {
        // Initialized in `Startup`
        public IConfigurationRoot Configuration { get; }

        // Initialized in `ConfigureServices`
        public IContainer ApplicationContainer { get; private set; }

        // Invoked by `Program.cs`
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddIniFile("appsettings.ini", optional: false, reloadOnChange: true);
            this.Configuration = builder.Build();
        }

        // This is where you register dependencies, add services to the
        // container. This method is called by the runtime, before the
        // Configure method below.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Add controllers as services so they'll be resolved.
            services.AddMvc().AddControllersAsServices();

            this.ApplicationContainer = DependencyResolution.Setup(services);

            // Print some useful information at bootstrap time
            this.PrintBootstrapInfo(this.ApplicationContainer);

            // Create the IServiceProvider based on the container
            return new AutofacServiceProvider(this.ApplicationContainer);
        }

        private void PrintBootstrapInfo(IContainer container)
        {
            var logger = container.Resolve<Services.Diagnostics.ILogger>();
            var config = container.Resolve<IConfig>();
            logger.Info("Web service started", () => new { Uptime.ProcessId });
            logger.Info("Device Types folder: " + config.ServicesConfig.DeviceTypesFolder, () => { });
            logger.Info("Scripts folder:      " + config.ServicesConfig.DeviceTypesScriptsFolder, () => { });
            logger.Info("IoT Hub manager URL: " + config.ServicesConfig.IoTHubManagerApiUrl, () => { });
        }

        // This method is called by the runtime, after the ConfigureServices
        // method above. Use this method to add middleware.
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            IApplicationLifetime appLifetime)
        {
            loggerFactory.AddConsole(this.Configuration.GetSection("Logging"));

            app.UseMvc();

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(() => this.ApplicationContainer.Dispose());
        }
    }
}
