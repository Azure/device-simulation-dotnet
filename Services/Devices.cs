// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Device = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Device;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDevices
    {
        // Set IoTHub connection strings, using either the user provided value or the configuration, 
        // initialize the IoT Hub registry, and perform other initializations. 
        Task InitAsync();

        // Explicitly set IoTHub connection string
        // TODO: remove this method once InitAsync() uses the connection string of a given
        //       simulation instead of using the single conn string present in the storage.
        void TmpInit(string connectionString);

        // Get a client for the device
        IDeviceClient GetClient(Device device, IoTHubProtocol protocol);

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

        /// <summary>
        /// Delete a device
        /// </summary>
        Task DeleteAsync(string deviceId);

        /// <summary>
        /// Delete a list of devices
        /// </summary>
        Task DeleteListAsync(IEnumerable<string> deviceIds);

        /// <summary>
        /// Create a list of devices using bulk import via storage account
        /// </summary>
        Task<string> CreateListUsingJobsAsync(IEnumerable<string> deviceIds);

        /// <summary>
        /// Check if an IoT Hub job is complete, executing an action if the job failed
        /// </summary>
        Task<bool> IsJobCompleteAsync(string jobId, Action recreateJobSignal);

        /// <summary>
        /// Delete a list of devices using bulk import via storage account
        /// </summary>
        Task<string> DeleteListUsingJobsAsync(IEnumerable<string> deviceIds);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing,
        /// or resetting unmanaged resources.
        /// </summary>
        void Dispose();
    }

    public class Devices : IDevices, IDisposable
    {
        // Simulated devices are marked with a tag "IsSimulated = Y"
        public const string SIMULATED_TAG_KEY = "IsSimulated";
        public const string SIMULATED_TAG_VALUE = "Y";

        // When creating import/export jobs the app creates a SAS token to grant access to a blob
        // in the storage account. When the token expires the job is unable to access this blob
        // so it's important to ensure that the token remains valid until the job completes.
        // Note (2018): assume that S3 hub processes ~1M devices per hour
        private const int JOB_SAS_TOKEN_DURATION_HOURS = 24 * 2; // 2 days

        // When using bulk operations, this is the max number of devices that the registry APIs allow
        private const int REGISTRY_MAX_BATCH_SIZE = 100;

        private readonly IConnectionStrings connectionStrings;
        private readonly IDeviceClientWrapper deviceClientFactory;
        private readonly IRegistryManager registry;

        private readonly ILogger log;
        private readonly IDiagnosticsLogger diagnosticsLogger;
        private readonly IServicesConfig config;
        private readonly IInstance instance;

        private string ioTHubHostName;
        private string connString;
        private string fixedDeviceKey;

        public Devices(
            IServicesConfig config,
            IConnectionStrings connStrings,
            IRegistryManager registryManager,
            IDeviceClientWrapper deviceClientFactory,
            ILogger logger,
            IDiagnosticsLogger diagnosticsLogger,
            IInstance instance)
        {
            this.config = config;
            this.connectionStrings = connStrings;
            this.registry = registryManager;
            this.deviceClientFactory = deviceClientFactory;
            this.log = logger;
            this.diagnosticsLogger = diagnosticsLogger;
            this.instance = instance;

            this.connString = null;
        }

        // Set IoTHub connection strings, using either the user provided value or the configuration, 
        // initialize the IoT Hub registry, and perform other initializations. 
        // TODO: use the simulation object to decide which conn string to use
        public async Task InitAsync()
        {
            this.instance.InitOnce();

            try
            {
                // TODO: use the simulation object to decide which conn string to use
                // Retrieve connection string from file/storage
                this.connString = await this.connectionStrings.GetAsync();

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
            catch (Exception e) when (e is ArgumentException || e is FormatException)
            {
                const string MSG = "Invalid IoT Hub connection string";
                this.log.Error(MSG, e);
                this.diagnosticsLogger.LogServiceError(MSG, e.Message);
                throw new InvalidIotHubConnectionStringFormatException(MSG, e);
            }
            catch (Exception e)
            {
                const string MSG = "IoT Hub connection setup failed";
                this.log.Error(MSG, e);
                this.diagnosticsLogger.LogServiceError(MSG, e.Message);
                throw;
            }
        }

        // TODO: method to be removed when InitAsync allows to use the connection string
        //       of a given simulation (i.e. when context is supported)
        public void TmpInit(string connectionString)
        {
            this.instance.InitOnce();

            try
            {
                this.connString = connectionString;

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
            catch (Exception e) when (e is ArgumentException || e is FormatException)
            {
                const string MSG = "Invalid IoT Hub connection string";
                this.log.Error(MSG, e);
                this.diagnosticsLogger.LogServiceError(MSG, e.Message);
                throw new InvalidIotHubConnectionStringFormatException(MSG, e);
            }
            catch (Exception e)
            {
                const string MSG = "IoT Hub connection setup failed";
                this.log.Error(MSG, e);
                this.diagnosticsLogger.LogServiceError(MSG, e.Message);
                throw;
            }
        }

        // Get a client for the device
        public IDeviceClient GetClient(Device device, IoTHubProtocol protocol)
        {
            this.instance.InitRequired();

            IDeviceClientWrapper sdkClient = this.GetDeviceSdkClient(device, protocol);
            var methods = new DeviceMethods(this.config, this.log, this.diagnosticsLogger);

            return new DeviceClient(
                device.Id,
                protocol,
                sdkClient,
                methods,
                this.config,
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

            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            Device result = null;
            try
            {
                Azure.Devices.Device device = await this.registry.GetDeviceAsync(deviceId);
                if (device != null)
                {
                    result = new Device(device, this.ioTHubHostName);
                }
                else
                {
                    var timeSpentMsecs = GetTimeSpentMsecs();
                    this.log.Debug("Device not found", () => new { timeSpentMsecs, deviceId });
                }
            }
            catch (Exception e) when (e is TaskCanceledException || e.InnerException is TaskCanceledException)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Get device task timed out", () => new { timeSpentMsecs, deviceId, e.Message });

                throw new ExternalDependencyException("Get device task timed out", e);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                const string MSG = "Unable to fetch the IoT device";
                this.log.Error(MSG, () => new { timeSpentMsecs, deviceId, e });
                this.diagnosticsLogger.LogServiceError(MSG, new { timeSpentMsecs, deviceId, e.Message });
                throw new ExternalDependencyException(MSG);
            }

            return result;
        }

        // Register a new device
        public async Task<Device> CreateAsync(string deviceId)
        {
            this.instance.InitRequired();

            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            try
            {
                this.log.Debug("Creating device", () => new { deviceId });

                var device = this.PrepareDeviceObject(deviceId, this.fixedDeviceKey);
                device = await this.registry.AddDeviceAsync(device);

                return new Device(device, this.ioTHubHostName);
            }
            catch (QuotaExceededException e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                const string MSG = "Too many devices, quota exceeded, unable to create the device";
                this.log.Error(MSG, () => new { timeSpentMsecs, deviceId, e });
                this.diagnosticsLogger.LogServiceError(MSG, new { timeSpentMsecs, deviceId, e.Message });
                throw new TotalDeviceCountQuotaExceededException(MSG, e);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                const string MSG = "Unable to create the device";
                this.log.Error(MSG, () => new { timeSpentMsecs, deviceId, e });
                this.diagnosticsLogger.LogServiceError(MSG, new { timeSpentMsecs, deviceId, e.Message });
                throw new ExternalDependencyException(MSG, e);
            }
        }

        // Add a tag to the device, to say it is a simulated device
        public async Task AddTagAsync(string deviceId)
        {
            this.instance.InitRequired();

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
            this.instance.InitRequired();

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
            this.instance.InitRequired();

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
                this.log.Error("Failed to delete device, unexpected error", () => new { error });
                throw;
            }
        }

        /// <summary>
        /// Delete a list of devices
        /// </summary>
        public async Task DeleteListAsync(IEnumerable<string> deviceIds)
        {
            this.instance.InitRequired();

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
                const string MSG = "Failed to delete devices, the batch is too big";
                this.log.Error(MSG, error);
                this.diagnosticsLogger.LogServiceError(MSG, error.Message);
                throw;
            }
            catch (IotHubCommunicationException error)
            {
                const string MSG = "Failed to delete devices (IotHubCommunicationException)";
                this.log.Error(MSG, () => new { error.InnerException, error });
                this.diagnosticsLogger.LogServiceError(MSG, new { error.Message });
                throw;
            }
            catch (Exception error)
            {
                const string MSG = "Failed to delete devices, unexpected error";
                this.log.Error(MSG, error);
                this.diagnosticsLogger.LogServiceError(MSG, error.Message);
                throw;
            }
        }

        // Create a list of devices using bulk import via storage account
        public async Task<string> CreateListUsingJobsAsync(IEnumerable<string> deviceIds)
        {
            this.instance.InitRequired();

            this.log.Info("Starting bulk device creation");

            // List of devices
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
                    Tags = new TwinCollection { [SIMULATED_TAG_KEY] = SIMULATED_TAG_VALUE }
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
                this.log.Error("Failed to create blob file required for the device bulk creation job", e);
                throw new ExternalDependencyException("Failed to create blob file", e);
            }

            // Create import job
            JobProperties job;
            try
            {
                var sasToken = this.GetSasTokenForImportExport();
                this.log.Info("Creating job to import devices for bulk creation");
                job = await this.registry.ImportDevicesAsync(blob.Container.StorageUri.PrimaryUri.AbsoluteUri + sasToken, blob.Name);
                this.log.Info("Job to import devices created for bulk creation");
            }
            catch (JobQuotaExceededException e)
            {
                this.log.Error("Job quota exceeded, retry later", e);
                throw new ExternalDependencyException("Job quota exceeded, retry later", e);
            }
            catch (Exception e)
            {
                this.log.Error("Failed to create device import job for bulk creation", e);
                throw new ExternalDependencyException("Failed to create device import job for bulk creation", e);
            }

            return job.JobId;
        }

        // Delete a list of devices using bulk import via storage account
        public async Task<string> DeleteListUsingJobsAsync(IEnumerable<string> deviceIds)
        {
            this.instance.InitRequired();

            this.log.Info("Starting bulk device deletion");

            // List of devices
            var serializedDevices = new List<string>();
            foreach (var deviceId in deviceIds)
            {
                var device = new ExportImportDevice
                {
                    Id = deviceId,
                    ImportMode = ImportMode.Delete
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
                this.log.Error("Failed to create blob file required for the device bulk deletion job", e);
                throw new ExternalDependencyException("Failed to create blob file", e);
            }

            // Create import job
            JobProperties job;
            try
            {
                var sasToken = this.GetSasTokenForImportExport();
                this.log.Info("Creating job to import devices for bulk deletion");
                job = await this.registry.ImportDevicesAsync(blob.Container.StorageUri.PrimaryUri.AbsoluteUri + sasToken, blob.Name);
                this.log.Info("Job to import devices created for bulk deletion");
            }
            catch (JobQuotaExceededException e)
            {
                this.log.Error("Job quota exceeded, retry later", e);
                throw new ExternalDependencyException("Job quota exceeded, retry later", e);
            }
            catch (Exception e)
            {
                this.log.Error("Failed to create device import job for bulk deletion", e);
                throw new ExternalDependencyException("Failed to create device import job for bulk deletion", e);
            }

            return job.JobId;
        }

        // Check if an IoT Hub job is complete
        public async Task<bool> IsJobCompleteAsync(string jobId, Action recreateJobSignal)
        {
            this.instance.InitRequired();

            JobProperties job;

            try
            {
                job = await this.registry.GetJobAsync(jobId);

                switch (job.Status)
                {
                    case JobStatus.Unknown:
                    case JobStatus.Scheduled:
                    case JobStatus.Queued:
                    case JobStatus.Enqueued:
                    case JobStatus.Running:
                        this.log.Debug("The Job is not complete yet", () => new { jobId, importJob = job });
                        return false;

                    case JobStatus.Completed:
                        this.log.Debug("The Job is complete", () => new { jobId, importJob = job });
                        return true;

                    case JobStatus.Failed:
                    case JobStatus.Cancelled:
                        this.log.Error("The Job failed or has been cancelled", () => new { jobId, importJob = job });
                        recreateJobSignal.Invoke();
                        return false;
                }
            }
            catch (Exception e)
            {
                this.log.Error("Error while checking job status", () => new { jobId, e });
                throw new ExternalDependencyException("Error while checking job status", e);
            }

            this.log.Error("Unknown registry job status", () => new { jobId, importJob = job });
            throw new ExternalDependencyException("Unknown job status: " + job.Status);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing,
        /// or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.registry?.Dispose();
            this.deviceClientFactory.Dispose();
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

        private async Task<CloudBlockBlob> WriteDevicesToBlobAsync(List<string> serializedDevices)
        {
            var sb = new StringBuilder();
            serializedDevices.ForEach(serializedDevice => sb.AppendLine(serializedDevice));

            // Write to blob
            var blob = await this.CreateImportExportBlobAsync();
            using (var stream = await blob.OpenWriteAsync())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
                for (var i = 0; i < bytes.Length; i += 500)
                {
                    int length = Math.Min(bytes.Length - i, 500);
                    await stream.WriteAsync(bytes, i, length);
                }
            }

            return blob;
        }

        private async Task<CloudBlockBlob> CreateImportExportBlobAsync()
        {
            // Container for the files managed by Azure IoT SDK.
            // Note: use a new container to speed up the operation and avoid old files left over
            string containerName = ("iothub-" + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-") + Guid.NewGuid().ToString("N")).ToLowerInvariant();
            string blobName = "devices.txt";

            this.log.Info("Creating import blob", () => new { containerName, blobName });

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(this.config.IoTHubImportStorageAccount);
            CloudBlobClient client = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(containerName);
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            await container.CreateIfNotExistsAsync();
            await blob.DeleteIfExistsAsync();

            return blob;
        }

        private string GetSasTokenForImportExport()
        {
            // Recommended to address possible clock skew, see
            // https://docs.microsoft.com/azure/storage/common/storage-dotnet-shared-access-signature-part-1#best-practices-when-using-sas
            const int CLOCK_SKEW_MINUTES = 15;

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(this.config.IoTHubImportStorageAccount);

            // Note:
            // 1. don't set start time, so the token is valid immediately and not affected by clock skew
            // 2. for clients using a REST version prior to 2012-02-12, the maximum duration for a SAS that does
            //    NOT reference a stored access policy is 1 hour
            var policy = new SharedAccessAccountPolicy
            {
                Permissions = SharedAccessAccountPermissions.Read
                              | SharedAccessAccountPermissions.Write
                              | SharedAccessAccountPermissions.Delete
                              | SharedAccessAccountPermissions.Add
                              | SharedAccessAccountPermissions.Create
                              | SharedAccessAccountPermissions.Update,
                Services = SharedAccessAccountServices.Blob,
                ResourceTypes = SharedAccessAccountResourceTypes.Container | SharedAccessAccountResourceTypes.Object,
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(JOB_SAS_TOKEN_DURATION_HOURS * 60 + CLOCK_SKEW_MINUTES),
                Protocols = SharedAccessProtocol.HttpsOnly
            };

            return storageAccount.GetSharedAccessSignature(policy);
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

        private IDeviceClientWrapper GetDeviceSdkClient(Device device, IoTHubProtocol protocol)
        {
            var connectionString = $"HostName={device.IoTHubHostName};DeviceId={device.Id};SharedAccessKey={device.AuthPrimaryKey}";
            var userAgent = this.config.UserAgent;

            IDeviceClientWrapper sdkClient;
            switch (protocol)
            {
                case IoTHubProtocol.AMQP:
                    this.log.Debug("Creating AMQP device client",
                        () => new { device.Id, device.IoTHubHostName });

                    sdkClient = this.deviceClientFactory.CreateFromConnectionString(connectionString, TransportType.Amqp_Tcp_Only, userAgent);
                    break;

                case IoTHubProtocol.MQTT:
                    this.log.Debug("Creating MQTT device client",
                        () => new { device.Id, device.IoTHubHostName });

                    sdkClient = this.deviceClientFactory.CreateFromConnectionString(connectionString, TransportType.Mqtt_Tcp_Only, userAgent);
                    break;

                case IoTHubProtocol.HTTP:
                    this.log.Debug("Creating HTTP device client",
                        () => new { device.Id, device.IoTHubHostName });

                    sdkClient = this.deviceClientFactory.CreateFromConnectionString(connectionString, TransportType.Http1, userAgent);
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
