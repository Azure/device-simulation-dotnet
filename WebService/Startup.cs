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
        private ISimulationAgent simulationAgent;

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
            // see: https://docs.microsoft.com/aspnet/core/security/cors
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

            this.simulationAgent = this.ApplicationContainer.Resolve<ISimulationAgent>();
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
            log.Write("Service started", () => new { Uptime.ProcessId, LogLevel = config.LoggingConfig.LogLevel.ToString() });
            
            log.Write("Logging level:             " + config.LoggingConfig.LogLevel, () => { });
            log.Write("Web service auth required: " + config.ClientAuthConfig.AuthRequired, () => { });

            log.Write("Device Models folder:      " + config.ServicesConfig.DeviceModelsFolder, () => { });
            log.Write("Scripts folder:            " + config.ServicesConfig.DeviceModelsScriptsFolder, () => { });

            log.Write("Connections per second:    " + config.RateLimitingConfig.ConnectionsPerSecond, () => { });
            log.Write("Registry ops per minute:   " + config.RateLimitingConfig.RegistryOperationsPerMinute, () => { });
            log.Write("Twin reads per second:     " + config.RateLimitingConfig.TwinReadsPerSecond, () => { });
            log.Write("Twin writes per second:    " + config.RateLimitingConfig.TwinWritesPerSecond, () => { });
            log.Write("Messages per second:       " + config.RateLimitingConfig.DeviceMessagesPerSecond, () => { });
            log.Write("Messages per day:          " + config.RateLimitingConfig.DeviceMessagesPerDay, () => { });

            log.Write("Number of telemetry threads:      " + config.ConcurrencyConfig.TelemetryThreads, () => { });
            log.Write("Max pending connections:          " + config.ConcurrencyConfig.MaxPendingConnections, () => { });
            log.Write("Max pending telemetry messages:   " + config.ConcurrencyConfig.MaxPendingTelemetry, () => { });
            log.Write("Max pending twin writes:          " + config.ConcurrencyConfig.MaxPendingTwinWrites, () => { });
            log.Write("Min duration of state loop:       " + config.ConcurrencyConfig.MinDeviceStateLoopDuration, () => { });
            log.Write("Min duration of connection loop:  " + config.ConcurrencyConfig.MinDeviceConnectionLoopDuration, () => { });
            log.Write("Min duration of telemetry loop:   " + config.ConcurrencyConfig.MinDeviceTelemetryLoopDuration, () => { });
            log.Write("Min duration of twin write loop:  " + config.ConcurrencyConfig.MinDevicePropertiesLoopDuration, () => { });
        }
    }
}
