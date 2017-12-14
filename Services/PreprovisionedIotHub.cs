// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Device = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Device;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IPreprovisionedIotHub
    {
        /// <summary>
        /// Ping the registry to see if the connection is healthy
        /// </summary>
        Task<Tuple<bool, string>> PingRegistryAsync();
    }

    public class PreprovisionedIotHub : IPreprovisionedIotHub
    {
        // The registry might be in an inconsistent state after several requests, this limit
        // is used to recreate the registry manager instance every once in a while, while starting
        // the simulation. When the simulation is running the registry is not used anymore.
        private const uint REGISTRY_LIMIT_REQUESTS = 1000;

        private readonly ILogger log;
        private readonly string connectionString;

        private string ioTHubHostName;
        private RegistryManager registry;
        private int registryCount;
        private bool setupDone;

        public PreprovisionedIotHub(
            IServicesConfig config,
            ILogger logger)
        {
            this.log = logger;
            this.connectionString = config.IoTHubConnString;
            this.setupDone = false;
        }

        /// <summary>
        /// Ping the registry to see if the connection is healthy
        /// </summary>
        public async Task<Tuple<bool, string>> PingRegistryAsync()
        {
            this.SetupHub();

            try
            {
                await this.GetRegistry().GetDeviceAsync("healthcheck");
                return new Tuple<bool, string>(true, "OK");
            }
            catch (Exception e)
            {
                this.log.Error("Device registry test failed", () => new { e });
                return new Tuple<bool, string>(false, e.Message);
            }
        }

        // Temporary workaround, see https://github.com/Azure/device-simulation-dotnet/issues/136
        private RegistryManager GetRegistry()
        {
            if (this.registryCount > REGISTRY_LIMIT_REQUESTS)
            {
                this.registry.CloseAsync();

                try
                {
                    this.registry.Dispose();
                }
                catch (Exception e)
                {
                    // Errors might occur here due to pending requests, they can be ignored
                    this.log.Debug("Ignoring registry manager Dispose() error", () => new { e });
                }

                this.registryCount = -1;
            }

            if (this.registryCount == -1)
            {
                this.registry = RegistryManager.CreateFromConnectionString(this.connectionString);
                this.registry.OpenAsync();
            }

            this.registryCount++;

            return this.registry;
        }

        // This call can throw an exception, which is fine when the exception happens during a method
        // call. We cannot allow the exception to occur in the constructor though, because it
        // would break DI.
        private void SetupHub()
        {
            if (this.setupDone) return;

            this.registry = RegistryManager.CreateFromConnectionString(this.connectionString);
            this.ioTHubHostName = IotHubConnectionStringBuilder.Create(this.connectionString).HostName;
            this.log.Info("Selected active IoT Hub for preprovisioned hub status check", () => new { this.ioTHubHostName });

            this.setupDone = true;
        }
    }
}
