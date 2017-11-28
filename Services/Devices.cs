// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Device = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Device;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDevices
    {
        Task<Tuple<bool, string>> PingRegistryAsync();
        IDeviceClient GetClient(Device device, IoTHubProtocol protocol, IScriptInterpreter scriptInterpreter);
        Task<Device> GetOrCreateAsync(string deviceId, bool loadTwin, CancellationToken cancellationToken);
        Task<Device> GetAsync(string deviceId, bool loadTwin, CancellationToken cancellationToken);
        void UpdateIotHub();
    }

    public class Devices : IDevices
    {
        // Whether to discard the twin created by the service when a device is created
        // When discarding the twin, we save one Twin Read operation (i.e. don't need to fetch the ETag)
        // TODO: when not discarding the twin, use the right ETag and manage conflicts
        //       https://github.com/Azure/device-simulation-dotnet/issues/83
        private const bool DISCARD_TWIN_ON_CREATION = true;

        private readonly ILogger log;
        private readonly IRateLimiting rateLimiting;
        private readonly IIotHubConnectionStringManager connectionStringManager;
        private readonly bool twinReadsWritesEnabled;

        private RegistryManager registry;
        private string ioTHubHostName;
        
        public Devices(
            IRateLimiting rateLimiting,
            IServicesConfig config,
            IIotHubConnectionStringManager connStringManager,
            ILogger logger)
        {
            this.rateLimiting = rateLimiting;
            this.log = logger;

            this.twinReadsWritesEnabled = config.TwinReadWriteEnabled;
            this.log.Debug("Devices service instantiated", () => new { this.ioTHubHostName });

            this.connectionStringManager = connStringManager;
            this.UpdateIotHub();
        }

        public async Task<Tuple<bool, string>> PingRegistryAsync()
        {
            try
            {
                await this.rateLimiting.LimitRegistryOperationsAsync(
                    () => this.registry.GetDeviceAsync("healthcheck"));
                return new Tuple<bool, string>(true, "OK");
            }
            catch (Exception e)
            {
                this.log.Error("Device registry test failed", () => new { e });
                return new Tuple<bool, string>(false, e.Message);
            }
        }

        /// <summary>
        /// Get IoTHub connection string from either from user provided
        /// value or from the environment variable.
        /// </summary>
        public void UpdateIotHub()
        {
            string connString = this.connectionStringManager.GetIotHubConnectionString();
            this.registry = RegistryManager.CreateFromConnectionString(connString);
            this.ioTHubHostName = IotHubConnectionStringBuilder.Create(connString).HostName;
            this.log.Info("Updated IoTHub for devices", () => new { this.ioTHubHostName });
        }

        public IDeviceClient GetClient(
            Device device,
            IoTHubProtocol protocol,
            IScriptInterpreter scriptInterpreter)
        {
            var sdkClient = this.GetDeviceSdkClient(device, protocol);
            var methods = new DeviceMethods(sdkClient, this.log, scriptInterpreter);

            return new DeviceClient(
                device.Id,
                protocol,
                sdkClient,
                methods,
                this.rateLimiting,
                this.log);
        }

        public async Task<Device> GetOrCreateAsync(string deviceId, bool loadTwin, CancellationToken cancellationToken)
        {

            if (!this.twinReadsWritesEnabled) { loadTwin = false; }

            try
            {
                return await this.GetAsync(deviceId, loadTwin, cancellationToken);
            }
            catch (ResourceNotFoundException)
            {
                this.log.Debug("Device not found, will create", () => new { deviceId });
                return await this.CreateAsync(deviceId, cancellationToken);
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while retrieving the device", () => new { deviceId, e });
                throw;
            }
        }

        public async Task<Device> GetAsync(string deviceId, bool loadTwin, CancellationToken cancellationToken)
        {
            Device result = null;

            try
            {
                Azure.Devices.Device device = null;
                Twin twin = null;

                if (loadTwin)
                {
                    var deviceTask = this.rateLimiting.LimitRegistryOperationsAsync(
                        () =>
                        {
                            this.log.Debug("Fetching device from registry", () => new { deviceId });
                            return this.registry.GetDeviceAsync(deviceId, cancellationToken);
                        });

                    var twinTask = this.rateLimiting.LimitTwinReadsAsync(
                        () =>
                        {
                            this.log.Debug("Fetching twin from registry", () => new { deviceId });
                            return this.registry.GetTwinAsync(deviceId, cancellationToken);
                        });

                    await Task.WhenAll(deviceTask, twinTask);

                    device = deviceTask.Result;
                    twin = twinTask.Result;
                }
                else
                {
                    device = await this.rateLimiting.LimitRegistryOperationsAsync(
                        () =>
                        {
                            this.log.Debug("Fetching device from registry", () => new { deviceId });
                            return this.registry.GetDeviceAsync(deviceId, cancellationToken);
                        });
                }

                if (device != null)
                {
                    result = new Device(device, twin, this.ioTHubHostName);
                }
            }
            catch (Exception e)
            {
                if (e.InnerException != null && e.InnerException.GetType() == typeof(TaskCanceledException))
                {
                    // We get here when the cancellation token is triggered, which is fine
                    this.log.Debug("Get device task canceled", () => new { deviceId, e.Message });
                    return null;
                }

                this.log.Error("Unable to fetch the IoT device", () => new { deviceId, e });
                throw new ExternalDependencyException("Unable to fetch the IoT device.");
            }

            if (result == null)
            {
                throw new ResourceNotFoundException("The device doesn't exist.");
            }

            return result;
        }

        private async Task<Device> CreateAsync(string deviceId, CancellationToken cancellationToken)
        {
            try
            {
                this.log.Debug("Creating device", () => new { deviceId });
                var device = new Azure.Devices.Device(deviceId);
                device = await this.rateLimiting.LimitRegistryOperationsAsync(
                    () => this.registry.AddDeviceAsync(device, cancellationToken));

                var twin = new Twin();
                if (!DISCARD_TWIN_ON_CREATION)
                {
                    this.log.Debug("Fetching device twin", () => new { device.Id });
                    twin = await this.rateLimiting.LimitTwinReadsAsync(() => this.registry.GetTwinAsync(device.Id, cancellationToken));
                }

                this.log.Debug("Writing device twin an adding the `IsSimulated` Tag",
                    () => new { device.Id, DeviceTwin.SIMULATED_TAG_KEY, DeviceTwin.SIMULATED_TAG_VALUE });
                twin.Tags[DeviceTwin.SIMULATED_TAG_KEY] = DeviceTwin.SIMULATED_TAG_VALUE;


                // TODO: when not discarding the twin, use the right ETag and manage conflicts
                //       https://github.com/Azure/device-simulation-dotnet/issues/83
                twin = await this.rateLimiting.LimitTwinWritesAsync(
                    () => this.registry.UpdateTwinAsync(device.Id, twin, "*", cancellationToken));

                return new Device(device, twin, this.ioTHubHostName);
            }
            catch (Exception e)
            {
                if (e.InnerException != null && e.InnerException.GetType() == typeof(TaskCanceledException))
                {
                    // We get here when the cancellation token is triggered, which is fine
                    this.log.Debug("Get device task canceled", () => new { deviceId, e.Message });
                    return null;
                }

                this.log.Error("Unable to fetch the IoT device", () => new { deviceId, e });
                throw new ExternalDependencyException("Unable to fetch the IoT device.");
            }
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
