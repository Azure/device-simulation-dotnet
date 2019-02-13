// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.PartitioningAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
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
        // Agent responsible for creating devices and partitions
        private IPartitioningAgent partitioningAgent;

        // Agent responsible for simulating IoT devices
        private ISimulationAgent simulationAgent;

        // Token used to stop the application threads when shutting down
        private readonly CancellationTokenSource appStopToken = new CancellationTokenSource();

        // Service responsible for managing simulation state 
        private ISimulations simulationService;

        // References used to monitor the application
        private Task partitioningAgentTask;
        private Task simulationAgentTask;
        private Task threadsMonitoringTask;

        // Initialized in `Startup`
        public IConfigurationRoot Configuration { get; }

        // Initialized in `ConfigureServices`
        public IContainer ApplicationContainer { get; private set; }

        // Invoked by `Program.cs`
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddIniFile(ConfigFile.DEFAULT, optional: false, reloadOnChange: true);

            if (ConfigFile.GetDevOnlyConfigFile() != null)
            {
                Console.WriteLine("===========================\nLOADING SETTINGS FROM " + ConfigFile.GetDevOnlyConfigFile() + "\n===========================");
                builder.AddIniFile(ConfigFile.GetDevOnlyConfigFile(), optional: true, reloadOnChange: true);
            }

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

            // Start simulation agent and partitioning agent threads
            appLifetime.ApplicationStarted.Register(() => this.StartAgents(appLifetime));
            appLifetime.ApplicationStopping.Register(this.StopAgents);

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(() => this.ApplicationContainer.Dispose());
        }

        private void StartAgents(IApplicationLifetime appLifetime)
        {
            // Temporary workaround to allow twin JSON deserialization in IoT SDK
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                CheckAdditionalContent = false
            };

            var config = this.ApplicationContainer.Resolve<IConfig>();

            // Start the partitioning agent, unless disabled
            this.partitioningAgent = this.ApplicationContainer.Resolve<IPartitioningAgent>();
            this.partitioningAgentTask = config.ServicesConfig.DisablePartitioningAgent
                ? Task.Run(() => Thread.Sleep(TimeSpan.FromHours(1)))
                : this.partitioningAgent.StartAsync(this.appStopToken.Token);

            // Start the simulation agent, unless disabled
            this.simulationAgent = this.ApplicationContainer.Resolve<ISimulationAgent>();
            this.simulationAgentTask = config.ServicesConfig.DisableSimulationAgent
                ? Task.Run(() => Thread.Sleep(TimeSpan.FromHours(1)))
                : this.simulationAgent.StartAsync(this.appStopToken.Token);

            // This creates sample simulations that will be shown on simulation dashboard by default
            this.simulationService = this.ApplicationContainer.Resolve<ISimulations>();
            if (!config.ServicesConfig.DisableSeedByTemplate)
            {
                this.simulationService.TrySeedAsync();
            }

            this.threadsMonitoringTask = this.MonitorThreadsAsync(appLifetime);
        }

        private void StopAgents()
        {
            // Send a signal to stop the application
            this.appStopToken.Cancel();

            // Stop the application threads
            // TODO: see if we can rely solely on the cancellation token
            this.partitioningAgent?.Stop();
            this.simulationAgent?.Stop();
        }

        private Task MonitorThreadsAsync(IApplicationLifetime appLifetime)
        {
            return Task.Run(() =>
                {
                    while (!this.appStopToken.IsCancellationRequested)
                    {
                        // Check threads every 2 seconds
                        Thread.Sleep(2000);

                        if (this.simulationAgentTask.Status == TaskStatus.Faulted
                            || this.simulationAgentTask.Status == TaskStatus.Canceled
                            || this.simulationAgentTask.Status == TaskStatus.RanToCompletion
                            || this.partitioningAgentTask.Status == TaskStatus.Faulted
                            || this.partitioningAgentTask.Status == TaskStatus.Canceled
                            || this.partitioningAgentTask.Status == TaskStatus.RanToCompletion)
                        {
                            var log = this.ApplicationContainer.Resolve<ILogger>();
                            log.Error("Part of the service is not running",
                                () => new
                                {
                                    SimulationAgent = this.simulationAgentTask.Status.ToString(),
                                    PartitioningAgent = this.partitioningAgentTask.Status.ToString()
                                });

                            // Allow few seconds to flush logs
                            Thread.Sleep(5000);
                            appLifetime.StopApplication();
                        }
                    }
                },
                this.appStopToken.Token);
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

            log.Write("Connections per second:    " + config.RateLimitingConfig.ConnectionsPerSecond);
            log.Write("Registry ops per minute:   " + config.RateLimitingConfig.RegistryOperationsPerMinute);
            log.Write("Messages per second:       " + config.RateLimitingConfig.DeviceMessagesPerSecond);

            if (config.ServicesConfig.DeviceTwinEnabled)
            {
                log.Write("Twin reads per second:     " + config.RateLimitingConfig.TwinReadsPerSecond);
                log.Write("Twin writes per second:    " + config.RateLimitingConfig.TwinWritesPerSecond);
            }
            else
            {
                log.Write("Twin reads per second:     0 - Twin disabled");
                log.Write("Twin writes per second:    0 - Twin disabled");
            }

            log.Write("C2D Methods:               " + (config.ServicesConfig.C2DMethodsEnabled ? "Enabled" : "Disabled"));

            log.Write("Number of telemetry threads:      " + config.AppConcurrencyConfig.TelemetryThreads);
            log.Write("Max pending connections:          " + config.AppConcurrencyConfig.MaxPendingConnections);
            log.Write("Max pending telemetry messages:   " + config.AppConcurrencyConfig.MaxPendingTelemetry);
            log.Write("Max pending twin writes:          " + config.AppConcurrencyConfig.MaxPendingTwinWrites);
            log.Write("Min duration of state loop:       " + config.AppConcurrencyConfig.MinDeviceStateLoopDuration);
            log.Write("Min duration of connection loop:  " + config.AppConcurrencyConfig.MinDeviceConnectionLoopDuration);
            log.Write("Min duration of telemetry loop:   " + config.AppConcurrencyConfig.MinDeviceTelemetryLoopDuration);
            log.Write("Min duration of twin write loop:  " + config.AppConcurrencyConfig.MinDevicePropertiesLoopDuration);
            log.Write("Max devices per partition:        " + config.ClusteringConfig.MaxPartitionSize);

            log.Write("Main storage:        " + config.ServicesConfig.MainStorage.StorageType);
            log.Write("Simulations storage: " + config.ServicesConfig.SimulationsStorage.StorageType);
            log.Write("Statistics storage:  " + config.ServicesConfig.StatisticsStorage.StorageType);
            log.Write("Replay files storage:" + config.ServicesConfig.ReplayFilesStorage.StorageType);
            log.Write("Devices storage:     " + config.ServicesConfig.DevicesStorage.StorageType);
            log.Write("Partitions storage:  " + config.ServicesConfig.PartitionsStorage.StorageType);
            log.Write("Nodes storage:       " + config.ServicesConfig.NodesStorage.StorageType);

            log.Write("SDK device client timeout:                  " + config.ServicesConfig.IoTHubSdkDeviceClientTimeout);
            log.Write("SDK Microsoft.Azure.Devices.Client version: "
                      + typeof(Devices.Client.Message).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
            log.Write("SDK Microsoft.Azure.Devices.Common version: "
                      + typeof(Devices.Common.ExceptionExtensions).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

            if (config.ServicesConfig.DisableSimulationAgent)
            {
                log.Error("Simulation Agent is disabled!");
            }

            if (config.ServicesConfig.DisablePartitioningAgent)
            {
                log.Error("Partitioning Agent is disabled!");
            }

            if (config.ServicesConfig.DisableSeedByTemplate)
            {
                log.Warn("Seed by Template is disabled!");
            }
        }
    }
}
