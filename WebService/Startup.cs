// Copyright (c) Microsoft. All rights reserved.

using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Auth;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ILogger = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics.ILogger;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService
{
    public class Startup
    {
        private ISimulation simulationAgent;

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
            // Setup (not enabling yet) CORS
            services.AddCors();

            // Add controllers as services so they'll be resolved.
            services.AddMvc().AddControllersAsServices();

            // Prepare DI container
            this.ApplicationContainer = DependencyResolution.Setup(services);

            // Print some useful information at bootstrap time
            this.PrintBootstrapInfo(this.ApplicationContainer);

            // Create the IServiceProvider based on the container
            return new AutofacServiceProvider(this.ApplicationContainer);
        }

        // This method is called by the runtime, after the ConfigureServices
        // method above. Use this method to add middleware.
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            ICorsSetup corsSetup,
            IApplicationLifetime appLifetime)
        {
            loggerFactory.AddConsole(this.Configuration.GetSection("Logging"));

            // Check for Authorization header before dispatching requests
            app.UseMiddleware<AuthMiddleware>();

            // Enable CORS - Must be before UseMvc
            // see: https://docs.microsoft.com/en-us/aspnet/core/security/cors
            corsSetup.UseMiddleware(app);

            app.UseMvc();

            // Start simulation agent thread
            appLifetime.ApplicationStarted.Register(this.StartAgent);
            appLifetime.ApplicationStopping.Register(this.StopAgent);

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(() => this.ApplicationContainer.Dispose());
        }

        private void StartAgent()
        {
            // Temporary workaround to allow twin JSON deserialization in IoT SDK
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                CheckAdditionalContent = false
            };

            this.simulationAgent = this.ApplicationContainer.Resolve<ISimulation>();
            this.simulationAgent.RunAsync();
        }

        private void StopAgent()
        {
            this.simulationAgent.Stop();
        }

        private void PrintBootstrapInfo(IContainer container)
        {
            var log = container.Resolve<ILogger>();
            var config = container.Resolve<IConfig>();
            log.Warn("Service started", () => new { Uptime.ProcessId, LogLevel = config.LoggingConfig.LogLevel.ToString() });

            log.Info("Web service auth required: " + config.ClientAuthConfig.AuthRequired, () => { });

            log.Info("Device Models folder: " + config.ServicesConfig.DeviceModelsFolder, () => { });
            log.Info("Scripts folder:      " + config.ServicesConfig.DeviceModelsScriptsFolder, () => { });

            log.Info("Connections per sec:  " + config.RateLimitingConfig.ConnectionsPerSecond, () => { });
            log.Info("Registry ops per sec: " + config.RateLimitingConfig.RegistryOperationsPerMinute, () => { });
            log.Info("Twin reads per sec:   " + config.RateLimitingConfig.TwinReadsPerSecond, () => { });
            log.Info("Twin writes per sec:  " + config.RateLimitingConfig.TwinWritesPerSecond, () => { });
            log.Info("Messages per second:  " + config.RateLimitingConfig.DeviceMessagesPerSecond, () => { });
            log.Info("Messages per day:     " + config.RateLimitingConfig.DeviceMessagesPerDay, () => { });
        }
    }
}
