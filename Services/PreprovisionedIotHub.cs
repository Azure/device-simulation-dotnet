// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IPreprovisionedIotHub
    {
        // Ping the registry to see if the connection is healthy
        Task<Tuple<bool, string>> PingRegistryAsync();
    }

    // TODO: revisit the use case, considering that simulations can use different hubs
    public class PreprovisionedIotHub : IPreprovisionedIotHub
    {
        private readonly ILogger log;
        private readonly IInstance instance;
        private readonly string connectionString;

        private string ioTHubHostName;
        private RegistryManager registry;

        public PreprovisionedIotHub(
            IServicesConfig config,
            ILogger logger,
            IInstance instance)
        {
            this.log = logger;
            this.instance = instance;
            this.connectionString = config.IoTHubConnString;
        }

        // Ping the registry to see if the connection is healthy
        public async Task<Tuple<bool, string>> PingRegistryAsync()
        {
            await this.InitAsync();

            try
            {
                await this.registry.GetDeviceAsync("healthcheck");
                return new Tuple<bool, string>(true, "OK");
            }
            catch (Exception e)
            {
                this.log.Error("Device registry test failed", e);
                return new Tuple<bool, string>(false, e.Message);
            }
        }

        // This call can throw an exception, which is fine when the exception happens during a method
        // call. We cannot allow the exception to occur in the constructor though, because it
        // would break DI and bring the application to a broken state.
        private async Task InitAsync()
        {
            if (this.instance.IsInitialized) return;

            this.registry = RegistryManager.CreateFromConnectionString(this.connectionString);
            await this.registry.OpenAsync();

            this.ioTHubHostName = IotHubConnectionStringBuilder.Create(this.connectionString).HostName;
            this.log.Info("Selected active IoT Hub for preprovisioned hub status check", () => new { this.ioTHubHostName });

            this.instance.InitComplete();
        }
    }
}
