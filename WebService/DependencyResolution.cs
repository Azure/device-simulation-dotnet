// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService
{
    public class DependencyResolution
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

            // Auto-wire additional assemblies
            assembly = typeof(IServicesConfig).GetTypeInfo().Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
        }

        /// <summary>Setup Custom rules overriding autowired ones.</summary>
        private static void SetupCustomRules(ContainerBuilder builder)
        {
            // Make sure the configuration is read only once.
            IConfig config = new Config(new ConfigData(new Logger(Uptime.ProcessId, LogLevel.Info)));
            builder.RegisterInstance(config).As<IConfig>().SingleInstance();

            // Service configuration is generated by the entry point, so we
            // prepare the instance here.
            builder.RegisterInstance(config.ServicesConfig).As<IServicesConfig>().SingleInstance();
            builder.RegisterInstance(config.ServicesConfig.RateLimiting).As<IRateLimitingConfiguration>().SingleInstance();

            // Instantiate only one logger
            // TODO: read log level from configuration
            //       https://github.com/Azure/device-simulation-dotnet/issues/43
            var logger = new Logger(Uptime.ProcessId, LogLevel.Debug);
            builder.RegisterInstance(logger).As<ILogger>().SingleInstance();

            // Auth and CORS setup
            Auth.Startup.SetupDependencies(builder, config);

            // By default the DI container create new objects when injecting
            // dependencies. To improve performance we reuse some instances,
            // for example to reuse IoT Hub connections, as opposed to creating
            // a new connection every time.
            builder.RegisterType<Simulations>().As<ISimulations>().SingleInstance();
            builder.RegisterType<DeviceModels>().As<IDeviceModels>().SingleInstance();
            builder.RegisterType<Services.Devices>().As<IDevices>().SingleInstance();
        }

        private static void RegisterFactory(IContainer container)
        {
            Factory.RegisterContainer(container);
        }

        /// <summary>
        /// Provide factory pattern for dependencies that are instantiated
        /// multiple times during the application lifetime.
        /// How to use:
        /// <code>
        /// class MyClass : IMyClass {
        ///     public MyClass(DependencyInjection.IFactory factory) {
        ///         this.factory = factory;
        ///     }
        ///     public SomeMethod() {
        ///         var instance1 = this.factory.Resolve<ISomething>();
        ///         var instance2 = this.factory.Resolve<ISomething>();
        ///         var instance3 = this.factory.Resolve<ISomething>();
        ///     }
        /// }
        /// </code>
        /// </summary>
        public interface IFactory
        {
            T Resolve<T>();
        }

        public class Factory : IFactory
        {
            private static IContainer container;

            public static void RegisterContainer(IContainer c)
            {
                container = c;
            }

            public T Resolve<T>()
            {
                return container.Resolve<T>();
            }
        }
    }
}
