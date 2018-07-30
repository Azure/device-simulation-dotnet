// Copyright (c) Microsoft. All rights reserved.

using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.ClusteringAgent;
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
        private IClusteringAgent clusteringAgent;

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
            this.ApplicationContainer = DependencyResolution.Init(services);

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
            appLifetime.ApplicationStarted.Register(this.StartAgents);
            appLifetime.ApplicationStopping.Register(this.StopAgents);

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(() => this.ApplicationContainer.Dispose());
        }

        private void StartAgents()
        {
            // Temporary workaround to allow twin JSON deserialization in IoT SDK
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                CheckAdditionalContent = false
            };

            this.clusteringAgent = this.ApplicationContainer.Resolve<IClusteringAgent>();
            this.clusteringAgent.StartAsync();

            this.simulationAgent = this.ApplicationContainer.Resolve<ISimulationAgent>();
            this.simulationAgent.StartAsync();
        }

        private void StopAgents()
        {
            this.clusteringAgent?.Stop();
            this.simulationAgent?.Stop();
        }

        private void PrintBootstrapInfo(IContainer container)
        {
            var log = container.Resolve<ILogger>();
            var config = container.Resolve<IConfig>();
            log.Write("Service started", () => new { Uptime.ProcessId, LogLevel = config.LoggingConfig.LogLevel.ToString() });

            log.Write("Logging level:             " + config.LoggingConfig.LogLevel);
            log.Write("Web service auth required: " + config.ClientAuthConfig.AuthRequired);

            log.Write("Device Models folder:      " + config.ServicesConfig.DeviceModelsFolder);
            log.Write("Scripts folder:            " + config.ServicesConfig.DeviceModelsScriptsFolder);

            log.Write("(default) Connections per second:    " + config.DefaultRateLimitingConfig.ConnectionsPerSecond);
            log.Write("(default) Registry ops per minute:   " + config.DefaultRateLimitingConfig.RegistryOperationsPerMinute);
            log.Write("(default) Twin reads per second:     " + config.DefaultRateLimitingConfig.TwinReadsPerSecond);
            log.Write("(default) Twin writes per second:    " + config.DefaultRateLimitingConfig.TwinWritesPerSecond);
            log.Write("(default) Messages per second:       " + config.DefaultRateLimitingConfig.DeviceMessagesPerSecond);
            log.Write("(default) Messages per day:          " + config.DefaultRateLimitingConfig.DeviceMessagesPerDay);

            log.Write("Number of telemetry threads:      " + config.AppConcurrencyConfig.TelemetryThreads);
            log.Write("Max pending connections:          " + config.AppConcurrencyConfig.MaxPendingConnections);
            log.Write("Max pending telemetry messages:   " + config.AppConcurrencyConfig.MaxPendingTelemetry);
            log.Write("Max pending twin writes:          " + config.AppConcurrencyConfig.MaxPendingTwinWrites);
            log.Write("Min duration of state loop:       " + config.AppConcurrencyConfig.MinDeviceStateLoopDuration);
            log.Write("Min duration of connection loop:  " + config.AppConcurrencyConfig.MinDeviceConnectionLoopDuration);
            log.Write("Min duration of telemetry loop:   " + config.AppConcurrencyConfig.MinDeviceTelemetryLoopDuration);
            log.Write("Min duration of twin write loop:  " + config.AppConcurrencyConfig.MinDevicePropertiesLoopDuration);

            log.Write("Max devices per partition:        " + config.ClusteringConfig.MaxPartitionSize);
            log.Write("Max devices per node:             " + config.ClusteringConfig.MaxDevicesPerNode);
        }
    }
}
