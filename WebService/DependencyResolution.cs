// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.PartitioningAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Agent = Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Agent;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService
{
    public static class DependencyResolution
    {
        /// <summary>
        /// Autofac configuration. Find more information here:
        /// @see http://docs.autofac.org/en/latest/integration/aspnetcore.html
        /// </summary>
        public static IContainer Setup(IServiceCollection services)
        {
            var builder = new ContainerBuilder();

            builder.Populate(services);

            AutowireAssemblies(builder);

            SetupCustomRules(builder);

            var container = builder.Build();
            RegisterFactory(container);

            return container;
        }

        public static IConfig GetConfig()
        {
            return GetConfig(out var tmp);
        }

        /// <summary>
        /// Autowire interfaces to classes from all the assemblies, to avoid
        /// manual configuration. Note that autowiring works only for interfaces
        /// with just one implementation.
        /// @see http://autofac.readthedocs.io/en/latest/register/scanning.html
        /// </summary>
        private static void AutowireAssemblies(ContainerBuilder builder)
        {
            var assembly = Assembly.GetEntryAssembly();
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();

            // Auto-wire Services.DLL
            assembly = typeof(IServicesConfig).GetTypeInfo().Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();

            // Auto-wire SimulationAgent.DLL
            assembly = typeof(ISimulationAgent).GetTypeInfo().Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();

            // Auto-wire PartitioningAgent.DLL
            assembly = typeof(IPartitioningAgent).GetTypeInfo().Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
        }

        /// <summary>
        /// Parse appsettings.ini and map data into the global Config class
        /// </summary>
        /// <param name="configuration">Configuration object provided by the SDK</param>
        private static IConfig GetConfig(out IConfigurationRoot configuration)
        {
            // More info about configuration at
            // https://docs.microsoft.com/aspnet/core/fundamentals/configuration
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddIniFile("appsettings.ini", optional: true, reloadOnChange: false);

            // Parse file and ensure the file is parsed only once
            configuration = configurationBuilder.Build();

            return new Config(new ConfigData(configuration, new Logger(Uptime.ProcessId)));
        }

        /// <summary>
        /// SetupAsync custom rules overriding autowired ones, for example in cases
        /// where an interface has multiple implementations, and cases where
        /// a singleton is preferred to new instances.
        /// </summary>
        private static void SetupCustomRules(ContainerBuilder builder)
        {
            var config = GetConfig(out var configurationRoot);

            // Register individual configuration parts so they can be referenced directly
            builder.RegisterInstance(configurationRoot).As<IConfigurationRoot>().SingleInstance();
            builder.RegisterInstance(config).As<IConfig>().SingleInstance();
            builder.RegisterInstance(config.LoggingConfig).As<ILoggingConfig>().SingleInstance();
            builder.RegisterInstance(config.ServicesConfig).As<IServicesConfig>().SingleInstance();
            builder.RegisterInstance(config.RateLimitingConfig).As<IRateLimitingConfig>().SingleInstance();
            builder.RegisterInstance(config.DeploymentConfig).As<IDeploymentConfig>().SingleInstance();
            builder.RegisterInstance(config.SimulationConcurrencyConfig).As<ISimulationConcurrencyConfig>().SingleInstance();
            builder.RegisterInstance(config.ClusteringConfig).As<IClusteringConfig>().SingleInstance();

            // Instantiate only one logger
            var logger = new Logger(Uptime.ProcessId, config.LoggingConfig);
            builder.RegisterInstance(logger).As<ILogger>().SingleInstance();

            // Auth and CORS setup
            Auth.Startup.SetupDependencies(builder, config);

            // By default the DI container create new objects when injecting
            // dependencies. To improve performance we reuse some instances,
            // for example to reuse IoT Hub connections, as opposed to creating
            // a new connection every time.
            builder.RegisterType<Agent>().As<ISimulationAgent>().SingleInstance();
            builder.RegisterType<Simulations>().As<ISimulations>().SingleInstance();
            builder.RegisterType<DeviceModels>().As<IDeviceModels>().SingleInstance();
            builder.RegisterType<DiagnosticsLogger>().As<IDiagnosticsLogger>().SingleInstance();

            // Registrations required by Autofac, these classes implement the same interface
            builder.RegisterType<Connect>().As<Connect>();
            builder.RegisterType<SetDeviceTag>().As<SetDeviceTag>();
            builder.RegisterType<CredentialsSetup>().As<CredentialsSetup>();
            builder.RegisterType<FetchFromRegistry>().As<FetchFromRegistry>();
            builder.RegisterType<Register>().As<Register>();
            builder.RegisterType<UpdateDeviceState>().As<UpdateDeviceState>();
            builder.RegisterType<SendTelemetry>().As<SendTelemetry>();
            builder.RegisterType<UpdateReportedProperties>().As<UpdateReportedProperties>();
            builder.RegisterType<Deregister>().As<Deregister>();
            builder.RegisterType<Disconnect>().As<Disconnect>();
        }

        private static void RegisterFactory(IContainer container)
        {
            Factory.RegisterResolver(container.Resolve);
        }
    }
}
