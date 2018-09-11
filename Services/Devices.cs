// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Device = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Device;
using IotHubConnectionStringBuilder = Microsoft.Azure.Devices.IotHubConnectionStringBuilder;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDevices
    {
        // Set the current IoT Hub using either the user provided one or the configuration settings
        // TODO: use the simulation object to decide which conn string to use
        Task InitAsync(Models.Simulation simulation);

        // Get a client for the device
        IDeviceClient GetDeviceClient(Device device, IoTHubProtocol protocol, IScriptInterpreter scriptInterpreter);

        // Get the device without connecting to the registry, using a known connection string
        Device GetWithKnownCredentials(string deviceId);

        // Get the device from the registry
        Task<Device> GetAsync(string deviceId);

        // Register a new device
        Task<Device> CreateAsync(string deviceId);

        // Create a list of devices using bulk import via storage account
        Task<string> CreateListUsingJobsAsync(IEnumerable<string> deviceIds);

        // Delete a list of devices using bulk import via storage account
        Task DeleteListUsingJobsAsync(IList<string> deviceIds);

        // TODO: add comment about what this does
        Task<bool> IsJobCompleteAsync(string jobId, Action recreateJobSignal);

        // Get all the devices present in the hub
        Task DeleteAllDevicesAsync();
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

        private readonly IIotHubConnectionStringManager connectionStringManager;
        private readonly ILogger log;
        private readonly IServicesConfig config;
        private readonly IDeviceClientWrapper deviceClient;
        private readonly IRegistryManager registry;
        private readonly IInstance instance;

        private string ioTHubHostName;
        private string connString;
        private string fixedDeviceKey;

        public Devices(
            IServicesConfig config,
            IIotHubConnectionStringManager connStringManager,
            IRegistryManager registryManager,
            IDeviceClientWrapper deviceClient,
            ILogger logger,
            IInstance instance)
        {
            this.config = config;
            this.connectionStringManager = connStringManager;
            this.connString = null;
            this.registry = registryManager;
            this.deviceClient = deviceClient;
            this.log = logger;
            this.instance = instance;
        }

        // Set IoTHub connection strings, using either the user provided value or the configuration
        // TODO: use the simulation object to decide which connection string to use
        public async Task InitAsync(Models.Simulation simulation)
        {
            this.instance.InitOnce();

            try
            {
                // Retrieve connection string from file/storage
                this.connString = await this.connectionStringManager.GetIotHubConnectionStringAsync();

                // Parse connection string, this triggers an exception if the string is invalid
                IotHubConnectionStringBuilder connStringBuilder = IotHubConnectionStringBuilder.Create(this.connString);

                // Prepare registry class used to create/retrieve devices
                this.registry.Init(this.connString);
                this.log.Debug("Device registry object ready", () => new { this.ioTHubHostName });

                // Prepare hostname used to build device connection strings
                this.ioTHubHostName = connStringBuilder.HostName;
                this.log.Info("Selected active IoT Hub for devices", () => new { this.ioTHubHostName });

                // Prepare the auth key used for all the devices
                this.fixedDeviceKey = connStringBuilder.SharedAccessKey;
                this.log.Debug("Device authentication key defined", () => new { this.ioTHubHostName });

                this.instance.InitComplete();
            }
            catch (Exception e)
            {
                this.log.Error("Initialization failed.", e);
                throw;
            }
        }

        // Get a client for the device
        public IDeviceClient GetDeviceClient(Device device, IoTHubProtocol protocol, IScriptInterpreter scriptInterpreter)
        {
            this.instance.InitRequired();

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
            this.instance.InitRequired();

            return new Device(
                this.PrepareDeviceObject(deviceId, this.fixedDeviceKey),
                this.ioTHubHostName);
        }

        // Get the device from the registry
        public async Task<Device> GetAsync(string deviceId)
        {
            this.instance.InitRequired();

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
            this.instance.InitRequired();

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

        public async Task<string> CreateListUsingJobsAsync(IEnumerable<string> deviceIds)
        {
            this.instance.InitRequired();

            this.log.Info("Starting bulk device creation");

            // A list of devices
            var serializedDevices = new List<string>();
            foreach (var deviceId in deviceIds)
            {
                var device = new ExportImportDevice
                {
                    Id = deviceId,
                    ImportMode = ImportMode.CreateOrUpdate,
                    Authentication = new AuthenticationMechanism
                    {
                        Type = AuthenticationType.Sas,
                        SymmetricKey = new SymmetricKey
                        {
                            PrimaryKey = this.fixedDeviceKey,
                            SecondaryKey = this.fixedDeviceKey
                        }
                    },
                    Status = DeviceStatus.Enabled,
                    Tags = new TwinCollection
                    {
                        [SIMULATED_TAG_KEY] = SIMULATED_TAG_VALUE
                    }
                };

                serializedDevices.Add(JsonConvert.SerializeObject(device));
            }

            CloudBlockBlob blob;
            try
            {
                blob = await this.WriteDevicesToBlobAsync(serializedDevices);
            }
            catch (Exception e)
            {
                this.log.Error("Failed to create blob file required for the device import job", e);
                throw new ExternalDependencyException("Failed to create blob file", e);
            }

            // Create import job
            JobProperties job;
            try
            {
                var sasToken = this.GetSasTokenForImportExport();
                this.log.Info("Creating job to import devices");
                job = await this.registry.ImportDevicesAsync(blob.Container.StorageUri.PrimaryUri.AbsoluteUri + sasToken, blob.Name);
                this.log.Info("Job to import devices created");
            }
            catch (JobQuotaExceededException e)
            {
                this.log.Error("Job quota exceeded, retry later", e);
                throw new ExternalDependencyException("Job quota exceeded, retry later", e);
            }
            catch (Exception e)
            {
                this.log.Error("Failed to create device import job", e);
                throw new ExternalDependencyException("Failed to create device import job", e);
            }

            return job.JobId;
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

        /// <summary>
        /// Delete a device from IoTHub
        /// </summary>
        public async Task DeleteAsync(string deviceId)
        {
            this.CheckSetup();
            this.log.Debug("Deleting device", () => new { deviceId });

            try
            {
                await this.registry.RemoveDeviceAsync(deviceId);
            }
            catch (IotHubCommunicationException error)
            {
                this.log.Error("Failed to delete device (IotHubCommunicationException)", () => new { error.InnerException, error });
                throw;
            }
            catch (Exception error)
            {
                this.log.Error("Failed to delete device", () => new { error });
                throw;
            }
        }

        /// <summary>
        /// Delete a list of devices
        /// </summary>
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
                this.log.Error("Failed to delete devices, the batch is too big", error);
                throw;
            }
            catch (IotHubCommunicationException error)
            {
                this.log.Error("Failed to delete devices (IotHubCommunicationException)", () => new { error.InnerException, error });
                throw;
            }
            catch (Exception error)
            {
                this.log.Error("Failed to delete devices", error);
                throw;
            }
        }

        /// <summary>
        /// Generate a device Id
        /// </summary>
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
