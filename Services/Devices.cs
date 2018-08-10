// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Device = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Device;
using IotHubConnectionStringBuilder = Microsoft.Azure.Devices.IotHubConnectionStringBuilder;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDevices
    {
        // Set the current IoT Hub using either the user provided one or the configuration settings
        void SetCurrentIotHub();

        // Get a client for the device
        IDeviceClient GetClient(Device device, IoTHubProtocol protocol, IScriptInterpreter scriptInterpreter);

        // Get the device without connecting to the registry, using a known connection string
        Device GetWithKnownCredentials(string deviceId);

        // Get the device from the registry
        Task<Device> GetAsync(string deviceId);

        // Register a new device
        Task<Device> CreateAsync(string deviceId);

        // Add a tag to the device, to say it is a simulated device
        Task AddTagAsync(string deviceId);

        // Create a list of devices
        Task CreateListAsync(IEnumerable<string> deviceIds);

        // Delete a list of devices
        Task DeleteListAsync(IEnumerable<string> deviceIds);

        // Generate a device Id
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

        // When using bulk operations, this is the max number of devices that the registry APIs allow
        private const int REGISTRY_MAX_BATCH_SIZE = 100;

        private readonly IIotHubConnectionStringManager connectionStringManager;
        private readonly ILogger log;
        private readonly IServicesConfig config;
        private readonly IDeviceClientWrapper deviceClient;

        private readonly bool twinReadsWritesEnabled;
        private string ioTHubHostName;
        private IRegistryManager registry;
        private bool setupDone;
        private string connString;
        private string fixedDeviceKey;

        public Devices(
            IServicesConfig config,
            IIotHubConnectionStringManager connStringManager,
            IRegistryManager registryManager,
            IDeviceClientWrapper deviceClient,
            ILogger logger)
        {
            this.config = config;
            this.connectionStringManager = connStringManager;
            this.connString = null;
            this.registry = registryManager;
            this.deviceClient = deviceClient;
            this.log = logger;
            this.twinReadsWritesEnabled = config.TwinReadWriteEnabled;
            this.setupDone = false;
        }

        // Set IoTHub connection strings, using either the user provided value or the configuration
        public void SetCurrentIotHub()
        {
            try
            {
                // Retrieve connection string from file/storage
                this.connString = this.connectionStringManager.GetIotHubConnectionString();

                // Parse connection string, this triggers an exception if the string is invalid
                IotHubConnectionStringBuilder connStringBuilder = IotHubConnectionStringBuilder.Create(this.connString);

                // Prepare registry class used to create/retrieve devices
                this.registry = this.registry.CreateFromConnectionString(this.connString);
                this.log.Debug("Device registry object ready", () => new { this.ioTHubHostName });

                // Prepare hostname used to build device connection strings
                this.ioTHubHostName = connStringBuilder.HostName;
                this.log.Info("Selected active IoT Hub for devices", () => new { this.ioTHubHostName });

                // Prepare the auth key used for all the devices
                this.fixedDeviceKey = connStringBuilder.SharedAccessKey;
                this.log.Debug("Device authentication key defined", () => new { this.ioTHubHostName });

                this.setupDone = true;
            }
            catch (Exception e)
            {
                this.log.Error("IoT Hub connection setup failed", () => new { e });
                throw;
            }
        }

        // Get a client for the device
        public IDeviceClient GetClient(Device device, IoTHubProtocol protocol, IScriptInterpreter scriptInterpreter)
        {
            this.CheckSetup();

            var sdkClient = this.GetDeviceSdkClient(device, protocol);
            var methods = new DeviceMethods(sdkClient, this.log, scriptInterpreter);

            return new DeviceClient(
                device.Id,
                protocol,
                sdkClient,
                methods,
                this.log);
        }

        // Get the device without connecting to the registry, using a known connection string
        public Device GetWithKnownCredentials(string deviceId)
        {
            this.CheckSetup();

            return new Device(
                this.PrepareDeviceObject(deviceId, this.fixedDeviceKey),
                this.ioTHubHostName);
        }

        // Get the device from the registry
        public async Task<Device> GetAsync(string deviceId)
        {
            this.CheckSetup();

            this.log.Debug("Fetching device from registry", () => new { deviceId });

            Device result = null;
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                Azure.Devices.Device device = await this.registry.GetDeviceAsync(deviceId);
                if (device != null)
                {
                    result = new Device(device, this.ioTHubHostName);
                }
                else
                {
                    var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                    this.log.Debug("Device not found", () => new { timeSpentMsecs, deviceId });
                }
            }
            catch (Exception e) when (e is TaskCanceledException || e.InnerException is TaskCanceledException)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Get device task timed out", () => new { timeSpentMsecs, deviceId, e.Message });
                throw new ExternalDependencyException("Get device task timed out", e);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Unable to fetch the IoT device", () => new { timeSpentMsecs, deviceId, e });
                throw new ExternalDependencyException("Unable to fetch the IoT device");
            }

            return result;
        }

        // Register a new device
        public async Task<Device> CreateAsync(string deviceId)
        {
            this.CheckSetup();
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                this.log.Debug("Creating device", () => new { deviceId });

                var device = new Azure.Devices.Device(deviceId);
                device = await this.registry.AddDeviceAsync(device);

                return new Device(device, this.ioTHubHostName);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
                this.log.Error("Unable to create the device", () => new { timeSpentMsecs, deviceId, e });
                throw new ExternalDependencyException("Unable to create the device", e);
            }
        }

        // Add a tag to the device, to say it is a simulated device
        public async Task AddTagAsync(string deviceId)
        {
            this.CheckSetup();

            this.log.Debug("Writing device twin and adding the `IsSimulated` Tag",
                () => new { deviceId, SIMULATED_TAG_KEY, SIMULATED_TAG_VALUE });

            var twin = new Twin
            {
                Tags = { [SIMULATED_TAG_KEY] = SIMULATED_TAG_VALUE }
            };

            await this.registry.UpdateTwinAsync(deviceId, twin, "*");
        }

        // Create a list of devices
        public async Task CreateListAsync(IEnumerable<string> deviceIds)
        {
            this.CheckSetup();

            var batches = this.SplitArray(deviceIds.ToList(), REGISTRY_MAX_BATCH_SIZE).ToArray();

            this.log.Info("Creating devices",
                () => new { Count = deviceIds.Count(), Batches = batches.Length, REGISTRY_MAX_BATCH_SIZE });

            for (var batchNumber = 0; batchNumber < batches.Length; batchNumber++)
            {
                var batch = batches[batchNumber];

                this.log.Debug("Creating devices batch",
                    () => new { batchNumber, batchSize = batch.Count() });

                BulkRegistryOperationResult result = await this.registry.AddDevices2Async(
                    batch.Select(id => this.PrepareDeviceObject(id, this.fixedDeviceKey)));

                this.log.Debug("Devices batch created",
                    () => new { batchNumber, result.IsSuccessful, ErrorsCount = result.Errors.Length });

                var errors = this.AnalyzeBatchErrors(result);
                if (errors > 0)
                {
                    throw new ExternalDependencyException($"Batch operation failed with {errors} errors");
                }
            }

            this.log.Info("Device creation completed",
                () => new { Count = deviceIds.Count(), Batches = batches.Length, REGISTRY_MAX_BATCH_SIZE });
        }

        // Delete a list of devices
        public async Task DeleteListAsync(IEnumerable<string> deviceIds)
        {
            this.CheckSetup();

            var batches = this.SplitArray(deviceIds.ToList(), REGISTRY_MAX_BATCH_SIZE).ToArray();

            this.log.Info("Deleting devices",
                () => new { Count = deviceIds.Count(), Batches = batches.Length, REGISTRY_MAX_BATCH_SIZE });

            try
            {
                for (var batchNumber = 0; batchNumber < batches.Length; batchNumber++)
                {
                    var batch = batches[batchNumber];

                    this.log.Debug("Deleting devices batch",
                        () => new { batchNumber, batchSize = batch.Count() });

                    BulkRegistryOperationResult result = await this.registry.RemoveDevices2Async(
                        batch.Select(id => new Azure.Devices.Device(id)),
                        forceRemove: true);

                    this.log.Debug("Devices batch deleted",
                        () => new { batchNumber, result.IsSuccessful, result.Errors });

                    var errors = this.AnalyzeBatchErrors(result);
                    if (errors > 0)
                    {
                        throw new ExternalDependencyException($"Batch operation failed with {errors} errors");
                    }
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

        // Generate a device Id
        public string GenerateId(string deviceModelId, int position)
        {
            return deviceModelId + "." + position;
        }

        // Log the errors occurred during a batch operation
        private int AnalyzeBatchErrors(BulkRegistryOperationResult result)
        {
            if (result.Errors.Length == 0) return 0;

            var errorsByType = new Dictionary<string, int>();

            // Ignore errors reporting that devices already exist
            var errorToIgnore = ErrorCode.DeviceAlreadyExists.ToString();

            foreach (var error in result.Errors)
            {
                var k = error.ErrorCode.ToString();
                if (k == errorToIgnore) continue;

                if (errorsByType.ContainsKey(k))
                {
                    errorsByType[k]++;
                }
                else
                {
                    errorsByType[k] = 1;
                }
            }

            if (errorsByType.Count == 0) return 0;

            this.log.Error("Some errors occurred in the batch operation",
                () => new { errorsByType, result.Errors });

            return errorsByType.Count;
        }

        // Create a Device object using a predefined authentication secret key
        private Azure.Devices.Device PrepareDeviceObject(string id, string key)
        {
            var result = new Azure.Devices.Device(id)
            {
                Authentication = new AuthenticationMechanism
                {
                    Type = AuthenticationType.Sas,
                    SymmetricKey = new SymmetricKey
                    {
                        PrimaryKey = key,
                        SecondaryKey = key
                    }
                }
            };

            return result;
        }

        private void CheckSetup()
        {
            if (this.setupDone) return;
            throw new ApplicationException(this.GetType().FullName + " Setup incomplete. " +
                                           "Call SetCurrentIotHub() before using the instance.");
        }

        private IDeviceClientWrapper GetDeviceSdkClient(Device device, IoTHubProtocol protocol)
        {
            var connectionString = $"HostName={device.IoTHubHostName};DeviceId={device.Id};SharedAccessKey={device.AuthPrimaryKey}";

            IDeviceClientWrapper sdkClient;
            switch (protocol)
            {
                case IoTHubProtocol.AMQP:
                    this.log.Debug("Creating AMQP device client",
                        () => new { device.Id, device.IoTHubHostName });

                    sdkClient = this.deviceClient.CreateFromConnectionString(connectionString, TransportType.Amqp_Tcp_Only);
                    break;

                case IoTHubProtocol.MQTT:
                    this.log.Debug("Creating MQTT device client",
                        () => new { device.Id, device.IoTHubHostName });

                    sdkClient = this.deviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt_Tcp_Only);
                    break;

                case IoTHubProtocol.HTTP:
                    this.log.Debug("Creating HTTP device client",
                        () => new { device.Id, device.IoTHubHostName });

                    sdkClient = this.deviceClient.CreateFromConnectionString(connectionString, TransportType.Http1);
                    break;

                default:
                    this.log.Error("Unable to create a client for the given protocol",
                        () => new { protocol });

                    throw new InvalidConfigurationException($"Unable to create a client for the given protocol ({protocol})");
            }

            sdkClient.DisableRetryPolicy();
            if (this.config.IoTHubSdkDeviceClientTimeout.HasValue)
            {
                sdkClient.OperationTimeoutInMilliseconds = this.config.IoTHubSdkDeviceClientTimeout.Value;
            }

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
