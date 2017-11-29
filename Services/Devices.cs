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
    public interface IDevices
    {
        /// <summary>
        /// Ping the registry to see if the connection is healthy
        /// </summary>
        Task<Tuple<bool, string>> PingRegistryAsync();

        /// <summary>
        /// Get a client for the device
        /// </summary>
        IDeviceClient GetClient(Device device, IoTHubProtocol protocol);

        /// <summary>
        /// Get the device from the registry
        /// </summary>
        Task<Device> GetAsync(string deviceId);

        /// <summary>
        /// Register the new device
        /// </summary>
        Task<Device> CreateAsync(string deviceId);

        /// <summary>
        /// Add a tag to the device, to say it is a simulated device 
        /// </summary>
        Task AddTagAsync(string deviceId);

        /// <summary> 
        /// Set the current IoT Hub using either the user provided one or the configuration settings 
        /// </summary>
        void SetCurrentIotHub();
    }

    public class Devices : IDevices
    {
        // The registry might be in an inconsistent state after several requests, this limit
        // is used to recreate the registry manager instance every once in a while, while starting
        // the simulation. When the simulation is running the registry is not used anymore.
        private const uint REGISTRY_LIMIT_REQUESTS = 1000;

        private readonly ILogger log;
        private readonly IIotHubConnectionStringManager connectionStringManager;
        private readonly bool twinReadsWritesEnabled;

        private string ioTHubHostName;
        private RegistryManager registry;
        private int registryCount;
        private bool setupDone;

        public Devices(
            IServicesConfig config,
            IIotHubConnectionStringManager connStringManager,
            ILogger logger)
        {
            this.log = logger;
            this.connectionStringManager = connStringManager;
            this.twinReadsWritesEnabled = config.TwinReadWriteEnabled;
            this.registryCount = -1;
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

        /// <summary>
        /// Get a client for the device
        /// </summary>
        public IDeviceClient GetClient(Device device, IoTHubProtocol protocol)
        {
            this.SetupHub();

            var sdkClient = this.GetDeviceSdkClient(device, protocol);

            return new DeviceClient(
                device.Id,
                protocol,
                sdkClient,
                this.log);
        }

        /// <summary>
        /// Get the device from the registry
        /// </summary>
        public async Task<Device> GetAsync(string deviceId)
        {
            this.SetupHub();

            this.log.Debug("Fetching device from registry", () => new { deviceId });

            Device result = null;
            var now = DateTimeOffset.UtcNow;

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
                    var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now.ToUnixTimeMilliseconds();
                    this.log.Error("Get device task timed out", () => new { timeSpent, deviceId, e.Message });
                    throw;
                }

                this.log.Error("Unable to fetch the IoT device", () => new { deviceId, e });
                throw new ExternalDependencyException("Unable to fetch the IoT device.");
            }

            return result;
        }

        /// <summary>
        /// Register the new device
        /// </summary>
        public async Task<Device> CreateAsync(string deviceId)
        {
            this.SetupHub();

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

        /// <summary>
        /// Add a tag to the device, to say it is a simulated device 
        /// </summary>
        public async Task AddTagAsync(string deviceId)
        {
            this.SetupHub();

            this.log.Debug("Writing device twin and adding the `IsSimulated` Tag",
                () => new { deviceId, DeviceTwin.SIMULATED_TAG_KEY, DeviceTwin.SIMULATED_TAG_VALUE });

            var twin = new Twin
            {
                Tags = { [DeviceTwin.SIMULATED_TAG_KEY] = DeviceTwin.SIMULATED_TAG_VALUE }
            };
            await this.GetRegistry().UpdateTwinAsync(deviceId, twin, "*");
        }

        /// <summary> 
        /// Get IoTHub connection string from either the user provided value or the configuration 
        /// </summary>
        public void SetCurrentIotHub()
        {
            string connString = this.connectionStringManager.GetIotHubConnectionString();
            this.registry = RegistryManager.CreateFromConnectionString(connString);
            this.ioTHubHostName = IotHubConnectionStringBuilder.Create(connString).HostName;
            this.log.Info("Selected active IoT Hub for devices", () => new { this.ioTHubHostName });
        }

        // This call can throw an exception, which is fine when the exception happens during a method
        // call. We cannot allow the exception to occur in the constructor though, because it
        // would break DI.
        private void SetupHub()
        {
            if (this.setupDone) return;
            this.SetCurrentIotHub();

            this.setupDone = true;
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
                string connString = this.connectionStringManager.GetIotHubConnectionString();
                this.registry = RegistryManager.CreateFromConnectionString(connString);
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
