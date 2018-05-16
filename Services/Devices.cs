// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
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
        /// <summary>
        /// Set the current IoT Hub using either the user provided one or the configuration settings
        /// </summary>
        void SetCurrentIotHub();

        /// <summary>
        /// Get a client for the device
        /// </summary>
        IDeviceClient GetClient(Device device, IoTHubProtocol protocol, IScriptInterpreter scriptInterpreter);

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
        /// Create a list of devices
        /// </summary>
        Task CreateListAsync(IEnumerable<string> deviceIds);

        /// <summary>
        /// Delete a list of devices
        /// </summary>
        Task DeleteListAsync(IEnumerable<string> deviceIds);

        /// <summary>
        /// Generate a device Id
        /// </summary>
        string GenerateId(string deviceModelId, int position);
    }

    public class Devices : IDevices
    {
        // Simulated devices are marked with a tag "IsSimulated = Y"
        public const string SIMULATED_TAG_KEY = "IsSimulated";
        public const string SIMULATED_TAG_VALUE = "Y";

        // The registry might be in an inconsistent state after several requests, this limit
        // is used to recreate the registry manager instance every once in a while, while starting
        // the simulation. When the simulation is running the registry is not used anymore.
        private const uint REGISTRY_LIMIT_REQUESTS = 1000;

        // When working with batches, this is the max size that the batch insert and delete APIs allow
        private const int REGISTRY_MAX_BATCH_SIZE = 100;

        private readonly IIotHubConnectionStringManager connectionStringManager;
        private readonly ILogger log;

        private readonly bool twinReadsWritesEnabled;
        private string ioTHubHostName;
        private IRegistryManager registry;
        private int registryCount;
        private bool setupDone;
        private IServicesConfig servicesConfig;

        public Devices(
            IServicesConfig config,
            IIotHubConnectionStringManager connStringManager,
            IRegistryManager registryManager,
            ILogger logger)
        {
            this.connectionStringManager = connStringManager;
            this.registry = registryManager;
            this.log = logger;
            this.twinReadsWritesEnabled = config.TwinReadWriteEnabled;
            this.registryCount = -1;
            this.setupDone = false;
            this.servicesConfig = config;
        }

        /// <summary>
        /// Get IoTHub connection string from either the user provided value or the configuration
        /// </summary>
        public void SetCurrentIotHub()
        {
            string connString = this.connectionStringManager.GetIotHubConnectionString();
            this.registry = this.registry.CreateFromConnectionString(connString);
            this.ioTHubHostName = IotHubConnectionStringBuilder.Create(connString).HostName;
            this.log.Info("Selected active IoT Hub for devices", () => new { this.ioTHubHostName });
        }

        /// <summary>
        /// Get a client for the device
        /// </summary>
        public IDeviceClient GetClient(Device device, IoTHubProtocol protocol, IScriptInterpreter scriptInterpreter)
        {
            this.SetupHub();

            var sdkClient = this.GetDeviceSdkClient(device, protocol);
            var methods = new DeviceMethods(sdkClient, this.log, scriptInterpreter);

            return new DeviceClient(
                device.Id,
                protocol,
                sdkClient,
                methods,
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
                    result = new Device(device, this.ioTHubHostName);
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
                throw new ExternalDependencyException("Unable to fetch the IoT device");
            }

            return result;
        }

        /// <summary>
        /// Register the new device
        /// </summary>
        public async Task<Device> CreateAsync(string deviceId)
        {
            this.SetupHub();
            var now = DateTimeOffset.UtcNow;

            try
            {
                this.log.Debug("Creating device", () => new { deviceId });

                var device = new Azure.Devices.Device(deviceId);

                device = await this.GetRegistry().AddDeviceAsync(device);

                return new Device(device, this.ioTHubHostName);
            }
            catch (Exception e)
            {
                var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now.ToUnixTimeMilliseconds();
                this.log.Error("Unable to create the device", () => new { timeSpent, deviceId, e });
                throw new ExternalDependencyException("Unable to create the device", e);
            }
        }

        /// <summary>
        /// Add a tag to the device, to say it is a simulated device 
        /// </summary>
        public async Task AddTagAsync(string deviceId)
        {
            this.SetupHub();

            this.log.Debug("Writing device twin and adding the `IsSimulated` Tag",
                () => new { deviceId, SIMULATED_TAG_KEY, SIMULATED_TAG_VALUE });

            var twin = new Twin
            {
                Tags = { [SIMULATED_TAG_KEY] = SIMULATED_TAG_VALUE }
            };
            await this.GetRegistry().UpdateTwinAsync(deviceId, twin, "*");
        }

        /// <summary>
        /// Create a list of devices
        /// </summary>
        public async Task CreateListAsync(IEnumerable<string> deviceIds)
        {
            this.SetupHub();

            var batches = this.SplitArray(deviceIds.ToList(), REGISTRY_MAX_BATCH_SIZE).ToArray();

            this.log.Info("Creating devices",
                () => new { Count = deviceIds.Count(), Batches = batches.Length, REGISTRY_MAX_BATCH_SIZE });

            for (var batchNumber = 0; batchNumber < batches.Length; batchNumber++)
            {
                var batch = batches[batchNumber];

                this.log.Info("Creating devices batch",
                    () => new { batchNumber, batchSize = batch.Count() });

                BulkRegistryOperationResult result = await this.registry.AddDevices2Async(
                    batch.Select(id => new Azure.Devices.Device(id)));

                this.log.Info("Devices batch created",
                    () => new { batchNumber, result.IsSuccessful, result.Errors });
            }
        }

        /// <summary>
        /// Delete a list of devices
        /// </summary>
        public async Task DeleteListAsync(IEnumerable<string> deviceIds)
        {
            this.SetupHub();

            var batches = this.SplitArray(deviceIds.ToList(), REGISTRY_MAX_BATCH_SIZE).ToArray();

            this.log.Info("Deleting devices",
                () => new { Count = deviceIds.Count(), Batches = batches.Length, REGISTRY_MAX_BATCH_SIZE });

            try
            {
                for (var batchNumber = 0; batchNumber < batches.Length; batchNumber++)
                {
                    var batch = batches[batchNumber];

                    this.log.Info("Deleting devices batch",
                        () => new { batchNumber, batchSize = batch.Count() });

                    BulkRegistryOperationResult result = await this.registry.RemoveDevices2Async(
                        batch.Select(id => new Azure.Devices.Device(id)),
                        forceRemove: true);

                    this.log.Info("Devices batch deleted",
                        () => new { batchNumber, result.IsSuccessful, result.Errors });
                }
            }
            catch (TooManyDevicesException error)
            {
                this.log.Error("Failed to delete devices, the batch is too big", () => new { error });
                throw;
            }
            catch (IotHubCommunicationException error)
            {
                this.log.Error("Failed to delete devices (IotHubCommunicationException)", () => new { error.InnerException, error });
                throw;
            }
            catch (Exception error)
            {
                this.log.Error("Failed to delete devices", () => new { error });
                throw;
            }
        }

        /// <summary>
        /// Generate a device Id
        /// </summary>
        public string GenerateId(string deviceModelId, int position)
        {
            // Since we're running across a cluster, devise a strategy to get unique names
            // According to https://forums.docker.com/t/net-core-linux-get-docker-container-id-in-code/32725
            // Environment.MachineName returns the GUID of the container
            return Environment.MachineName + "_" + deviceModelId + "." + position;
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
        private IRegistryManager GetRegistry()
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
                this.registry = this.registry.CreateFromConnectionString(connString);
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

            sdkClient.SetRetryPolicy(new Azure.Devices.Client.NoRetry());

            // When sending telemetry or other operations, wait only for preconfigured number of milliseconds. 
            // This setting sets how throttling affects the application. The default SDK value is 4 minutes, 
            // that causes high CPU usage. However extreme lower values such as 10000 milliseconds causes 
            // memory leaks leading to simulator crashing and termination of telemetry.
            sdkClient.OperationTimeoutInMilliseconds = (uint)this.servicesConfig.IoTSdkConnectTimeout;

            return sdkClient;
        }

        private IEnumerable<IEnumerable<T>> SplitArray<T>(IReadOnlyCollection<T> array, int size)
        {
            var count = (int) Math.Ceiling((float) array.Count / size);
            for (int i = 0; i < count; i++)
            {
                yield return array.Skip(i * size).Take(size);
            }
        }
    }
}
