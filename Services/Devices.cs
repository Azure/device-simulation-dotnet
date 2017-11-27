// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Device = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Device;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDevices
    {
        Task<Tuple<bool, string>> PingRegistryAsync();
        IDeviceClient GetClient(Device device, IoTHubProtocol protocol);
        Task<Device> GetAsync(string deviceId);
        Task<Device> CreateAsync(string deviceId);
        Task AddTagAsync(string deviceId);
    }

    public class Devices : IDevices
    {
        // The registry might be in an inconsistent state after several requests, this limit
        // is used to recreate the registry manager instance every once in a while, while starting
        // the simulation. When the simulation is running the registry is not used anymore.
        private const uint REGISTRY_LIMIT_REQUESTS = 1000;

        private readonly ILogger log;
        private readonly string ioTHubHostName;
        private readonly string ioTHubConnString;
        private RegistryManager registry;
        private int registryCount;

        public Devices(
            IServicesConfig config,
            ILogger logger)
        {
            this.log = logger;
            this.ioTHubConnString = config.IoTHubConnString;
            this.ioTHubHostName = IotHubConnectionStringBuilder.Create(config.IoTHubConnString).HostName;
            this.registryCount = -1;
        }

        public async Task<Tuple<bool, string>> PingRegistryAsync()
        {
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

        public IDeviceClient GetClient(Device device, IoTHubProtocol protocol)
        {
            var sdkClient = this.GetDeviceSdkClient(device, protocol);

            return new DeviceClient(
                device.Id,
                protocol,
                sdkClient,
                this.log);
        }

        public async Task<Device> GetAsync(string deviceId)
        {
            this.log.Debug("Fetching device from registry", () => new { deviceId });

            Device result = null;

            try
            {
                var device = await this.GetRegistry().GetDeviceAsync(deviceId);
                if (device != null)
                {
                    result = new Device(device, (Twin) null, this.ioTHubHostName);
                }
                else
                {
                    this.log.Debug("Device not found", () => new { deviceId });
                }
            }
            catch (Exception e)
            {
                if (e.InnerException != null && e.InnerException.GetType() == typeof(TaskCanceledException))
                {
                    this.log.Error("Get device task timed out", () => new { deviceId, e.Message });
                    throw;
                }

                this.log.Error("Unable to fetch the IoT device", () => new { deviceId, e });
                throw new ExternalDependencyException("Unable to fetch the IoT device.");
            }

            return result;
        }

        public async Task<Device> CreateAsync(string deviceId)
        {
            try
            {
                this.log.Debug("Creating device", () => new { deviceId });

                var device = new Azure.Devices.Device(deviceId);

                device = await this.GetRegistry().AddDeviceAsync(device);

                return new Device(device, (Twin) null, this.ioTHubHostName);
            }
            catch (Exception e)
            {
                if (e.InnerException != null && e.InnerException.GetType() == typeof(TaskCanceledException))
                {
                    // We get here when the cancellation token is triggered, which is fine
                    this.log.Debug("Device creation task canceled", () => new { deviceId, e.Message });
                    return null;
                }

                this.log.Error("Unable to create the device", () => new { deviceId, e });
                throw new ExternalDependencyException("Unable to create the device.", e);
            }
        }

        public async Task AddTagAsync(string deviceId)
        {
            this.log.Debug("Writing device twin and adding the `IsSimulated` Tag",
                () => new { deviceId, DeviceTwin.SIMULATED_TAG_KEY, DeviceTwin.SIMULATED_TAG_VALUE });

            var twin = new Twin
            {
                Tags = { [DeviceTwin.SIMULATED_TAG_KEY] = DeviceTwin.SIMULATED_TAG_VALUE }
            };
            await this.GetRegistry().UpdateTwinAsync(deviceId, twin, "*");
        }

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
                this.registry = RegistryManager.CreateFromConnectionString(this.ioTHubConnString);
                this.registry.OpenAsync();
            }

            this.registryCount++;

            return this.registry;
        }

        private Azure.Devices.Client.DeviceClient GetDeviceSdkClient(Device device, IoTHubProtocol protocol)
        {
            var connectionString = $"HostName={device.IoTHubHostName};DeviceId={device.Id};SharedAccessKey={device.AuthPrimaryKey}";

            Azure.Devices.Client.DeviceClient sdkClient;
            switch (protocol)
            {
                case IoTHubProtocol.AMQP:
                    this.log.Debug("Creating AMQP device client",
                        () => new { device.Id, device.IoTHubHostName });

                    sdkClient = Azure.Devices.Client.DeviceClient.CreateFromConnectionString(connectionString, TransportType.Amqp_Tcp_Only);
                    break;

                case IoTHubProtocol.MQTT:
                    this.log.Debug("Creating MQTT device client",
                        () => new { device.Id, device.IoTHubHostName });

                    sdkClient = Azure.Devices.Client.DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt_Tcp_Only);
                    break;

                case IoTHubProtocol.HTTP:
                    this.log.Debug("Creating HTTP device client",
                        () => new { device.Id, device.IoTHubHostName });

                    sdkClient = Azure.Devices.Client.DeviceClient.CreateFromConnectionString(connectionString, TransportType.Http1);
                    break;

                default:
                    this.log.Error("Unable to create a client for the given protocol",
                        () => new { protocol });

                    throw new InvalidConfigurationException($"Unable to create a client for the given protocol ({protocol})");
            }

            return sdkClient;
        }
    }
}
