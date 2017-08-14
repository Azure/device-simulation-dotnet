// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
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
        Task<Device> GetOrCreateAsync(string deviceId);
        Task<Device> GetAsync(string deviceId);
        Task<Device> CreateAsync(string deviceId);
    }

    public class Devices : IDevices
    {
        private readonly ILogger log;
        private readonly RegistryManager registry;
        private readonly string ioTHubHostName;

        public Devices(
            IServicesConfig config,
            ILogger logger)
        {
            this.log = logger;
            this.registry = RegistryManager.CreateFromConnectionString(config.IoTHubConnString);
            this.ioTHubHostName = IotHubConnectionStringBuilder.Create(config.IoTHubConnString).HostName;
            this.log.Debug("Devices service instantiated", () => new { this.ioTHubHostName });
        }

        public async Task<Tuple<bool, string>> PingRegistryAsync()
        {
            try
            {
                await this.registry.GetDeviceAsync("healthcheck");
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

                    throw new Exception($"Unable to create a client for the given protocol ({protocol})");
            }

            return new DeviceClient(sdkClient, protocol, this.log);
        }

        public async Task<Device> GetOrCreateAsync(string deviceId)
        {
            try
            {
                return await this.GetAsync(deviceId);
            }
            catch (ResourceNotFoundException)
            {
                this.log.Debug("Device not found, will create", () => new { deviceId });
                return await this.CreateAsync(deviceId);
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while retrieving the device", () => new { deviceId, e });
                throw;
            }
        }

        public async Task<Device> GetAsync(string deviceId)
        {
            Device result = null;

            try
            {
                var device = this.registry.GetDeviceAsync(deviceId);
                var twin = this.registry.GetTwinAsync(deviceId);
                await Task.WhenAll(device, twin);

                if (device.Result != null)
                {
                    result = new Device(device.Result, twin.Result, this.ioTHubHostName);
                }
            }
            catch (Exception e)
            {
                this.log.Error("Unable to fetch the IoT device", () => new { deviceId, e });
                throw new ExternalDependencyException("Unable to fetch the IoT device.");
            }

            if (result == null)
            {
                throw new ResourceNotFoundException("The device doesn't exist.");
            }

            return result;
        }

        public async Task<Device> CreateAsync(string deviceId)
        {
            this.log.Debug("Creating device", () => new { deviceId });
            var device = new Azure.Devices.Device(deviceId);
            var azureDevice = await this.registry.AddDeviceAsync(device);

            this.log.Debug("Fetching device twin", () => new { azureDevice.Id });
            var azureTwin = await this.registry.GetTwinAsync(azureDevice.Id);

            this.log.Debug("Writing device twin", () => new { azureDevice.Id });
            azureTwin.Tags[DeviceTwin.SimulatedTagKey] = DeviceTwin.SimulatedTagValue;
            azureTwin = await this.registry.UpdateTwinAsync(azureDevice.Id, azureTwin, "*");

            return new Device(azureDevice, azureTwin, this.ioTHubHostName);
        }
    }
}
