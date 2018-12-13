// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceClient
    {
        IoTHubProtocol Protocol { get; }
        string DeviceId { get; }
        Task ConnectAsync();
        Task DisconnectAsync();
        Task SendMessageAsync(string message, DeviceModel.DeviceModelMessageSchema schema);

        Task RegisterMethodsForDeviceAsync(
            IDictionary<string, Script> methods,
            ISmartDictionary deviceState,
            ISmartDictionary deviceProperties,
            IScriptInterpreter scriptInterpreter);

        Task RegisterDesiredPropertiesUpdateAsync(ISmartDictionary deviceProperties);
        Task UpdatePropertiesAsync(ISmartDictionary deviceProperties);
        void Dispose();
    }

    public class DeviceClient : IDeviceClient, IDisposable
    {
        private const string DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";

        // See also https://github.com/Azure/toketi-iothubreact/blob/master/src/main/scala/com/microsoft/azure/iot/iothubreact/MessageFromDevice.scala
        private const string CREATION_TIME_PROPERTY = "$$CreationTimeUtc";
        private const string CLASSNAME_PROPERTY = "$$ClassName";
        private const string MESSAGE_SCHEMA_PROPERTY = "$$MessageSchema";
        private const string CONTENT_PROPERTY = "$$ContentType";

        private readonly string deviceId;
        private readonly IoTHubProtocol protocol;
        private readonly IDeviceMethods deviceMethods;
        private readonly IDevicePropertiesRequest propertiesUpdateRequest;
        private readonly ILogger log;
        private readonly IDeviceClientWrapper client;
        private readonly bool deviceTwinEnabled;

        private bool connected;

        public string DeviceId => this.deviceId;
        public IoTHubProtocol Protocol => this.protocol;

        public DeviceClient(
            string deviceId,
            IoTHubProtocol protocol,
            IDeviceClientWrapper client,
            IDeviceMethods deviceMethods,
            IServicesConfig servicesConfig,
            ILogger logger)
        {
            this.deviceId = deviceId;
            this.protocol = protocol;
            this.client = client;
            this.deviceMethods = deviceMethods;
            this.log = logger;
            this.deviceTwinEnabled = servicesConfig.DeviceTwinEnabled;

            this.propertiesUpdateRequest = new DeviceProperties(servicesConfig, logger);
        }

        public async Task ConnectAsync()
        {
            if (this.client != null && !this.connected)
            {
                var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

                try
                {
                    // TODO: HTTP clients don't "connect", find out how HTTP connections are measured and throttled
                    //       https://github.com/Azure/device-simulation-dotnet/issues/85
                    await this.client.OpenAsync();
                    this.connected = true;
                }
                catch (NullReferenceException)
                {
                    // In case of multi-threaded access to the client...
                    if (this.client == null) return;

                    throw;
                }
                catch (UnauthorizedException e)
                {
                    // Note: this exception might not occur with HTTP
                    // TODO: test for HTTP

                    var timeSpentMsecs = GetTimeSpentMsecs();
                    this.log.Error("Device connection auth failed",
                        () => new { timeSpentMsecs, this.deviceId, this.protocol, e });
                    throw new DeviceAuthFailedException(e);
                }
                catch (DeviceNotFoundException e)
                {
                    var timeSpentMsecs = GetTimeSpentMsecs();
                    this.log.Error("Device not found",
                        () => new { timeSpentMsecs, this.deviceId, this.protocol, e });
                    throw new DeviceNotFoundException(this.deviceId);
                }
            }
        }

        public async Task DisconnectAsync()
        {
            this.connected = false;
            if (this.client != null)
            {
                try
                {
                    await this.client.CloseAsync();
                }
                catch (NullReferenceException)
                {
                    // In case of multi-threaded access to the client, ignore
                    if (this.client == null) return;

                    throw;
                }
                catch (Exception e)
                {
                    this.log.Error("Device disconnect failed",
                        () => new
                        {
                            this.deviceId,
                            this.protocol,
                            e
                        });
                }

                this.Dispose();
            }
        }

        public async Task RegisterMethodsForDeviceAsync(
            IDictionary<string, Script> methods,
            ISmartDictionary deviceState,
            ISmartDictionary deviceProperties,
            IScriptInterpreter scriptInterpreter)
        {
            this.log.Debug("Attempting to register device methods",
                () => new { this.deviceId });

            await this.deviceMethods.RegisterMethodsAsync(
                this.client,
                this.deviceId,
                methods,
                deviceState,
                deviceProperties,
                scriptInterpreter);
        }

        public async Task RegisterDesiredPropertiesUpdateAsync(ISmartDictionary deviceProperties)
        {
            this.log.Debug("Attempting to register desired property notifications for device",
                () => new { this.deviceId });

            await this.propertiesUpdateRequest.RegisterChangeUpdateAsync(this.client, this.deviceId, deviceProperties);
        }

        public async Task SendMessageAsync(string message, DeviceModel.DeviceModelMessageSchema schema)
        {
            switch (schema.Format)
            {
                case DeviceModel.DeviceModelMessageSchemaFormat.JSON:
                    await this.SendJsonMessageAsync(message, schema);
                    break;
                case DeviceModel.DeviceModelMessageSchemaFormat.Protobuf:
                    await this.SendProtobufMessageAsync(message, schema);
                    break;
                default:
                    throw new UnknownMessageFormatException($"Message format {schema.Format.ToString()} is invalid. Check the Telemetry format against the permitted values Binary, Text, Json, Protobuf");
            }
        }

        /// <summary>
        /// Updates the reported properties in the device twin on the IoT Hub
        /// </summary>
        public async Task UpdatePropertiesAsync(ISmartDictionary properties)
        {
            if (!this.deviceTwinEnabled)
            {
                this.log.Debug("Skipping twin update, twin operations are disabled in the global configuration.");
                return;
            }

            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            try
            {
                var reportedProperties = this.SmartDictionaryToTwinCollection(properties);
                await this.client.UpdateReportedPropertiesAsync(reportedProperties);

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Debug("Updated reported properties for device",
                    () => new { this.deviceId, timeSpentMsecs, reportedProperties });
            }
            catch (NullReferenceException)
            {
                // In case of multi-threaded access to the client, nothing to do
                if (this.client == null) return;

                throw;
            }
            catch (KeyNotFoundException e)
            {
                // This exception sometimes occurs when calling UpdateReportedPropertiesAsync.
                // Still unknown why, apparently an issue with the internal AMQP library
                // used by IoT SDK. We need to collect extra information to report the issue.
                // The exception is logged differently than usual to capture info that might help fixing the SDK.
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Unexpected error, failed to update reported properties",
                    () => new
                    {
                        timeSpentMsecs,
                        this.deviceId,
                        Protocol = this.protocol.ToString(),
                        Exception = e.GetType().FullName,
                        e.Message,
                        e.StackTrace,
                        e.Data,
                        e.Source,
                        e.TargetSite,
                        e.InnerException // This appears to always be null in this scenario
                    });
                throw new BrokenDeviceClientException("Unexpected error, failed to update reported properties", e);
            }
            catch (TimeoutException e)
            {
                // Note: this exception can occur in case of throttling, and
                // the caller should not recreate the client

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Reported properties update timed out",
                    () => new { timeSpentMsecs, this.deviceId, Protocol = this.protocol.ToString(), e });

                throw new PropertySendException("Reported properties update timed out", e);
            }
            catch (IotHubCommunicationException e)
            {
                // Note: this exception can occur in case of throttling, and
                // the caller should not recreate the client

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Failed to update reported properties",
                    () => new { timeSpentMsecs, this.deviceId, Protocol = this.protocol.ToString(), e });

                throw new PropertySendException("Failed to update reported properties", e);
            }
            catch (Exception e)
            {
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error("Failed to update reported properties",
                    () => new { timeSpentMsecs, this.deviceId, Protocol = this.protocol.ToString(), e });

                throw new BrokenDeviceClientException("Failed to update reported properties", e);
            }
        }

        public void Dispose()
        {
            this.connected = false;

            try
            {
                // IDeviceClientWrapper disposal
                this.client?.Dispose();
            }
            catch (Exception e)
            {
                this.log.Error("Something went wrong while disposing the device client",
                    () => new { this.deviceId, this.protocol, e });
            }
        }

        private async Task SendRawMessageAsync(Message message)
        {
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long GetTimeSpentMsecs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

            try
            {
                await this.client.SendEventAsync(message);

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Debug("SendRawMessageAsync for device",
                    () => new { timeSpentMsecs, this.deviceId });
            }
            catch (NullReferenceException)
            {
                // In case of multi-threaded access to the client, ignore
                if (this.client == null) return;

                throw;
            }
            catch (TimeoutException e)
            {
                var msg = "Message delivery timed out: " + e.Message;

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error(msg,
                    () => new { timeSpentMsecs, this.deviceId, Protocol = this.protocol.ToString(), e });

                throw new TelemetrySendTimeoutException(msg, e);
            }
            catch (DeviceMaximumQueueDepthExceededException e)
            {
                // Throttling in AMQP leads here
                var msg = "Daily telemetry quota exceeded: " + e.Message;

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error(msg,
                    () => new { timeSpentMsecs, this.deviceId, Protocol = this.protocol.ToString(), e });

                throw new DailyTelemetryQuotaExceededException(msg, e);
            }
            catch (QuotaExceededException e)
            {
                // Throttling in HTTP leads here
                var msg = "Daily telemetry quota exceeded: " + e.Message;

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error(msg,
                    () => new { timeSpentMsecs, this.deviceId, Protocol = this.protocol.ToString(), e });

                throw new DailyTelemetryQuotaExceededException(msg, e);
            }
            catch (SocketException e)
            {
                // TODO: throttling in MQTT leads here, but the exception
                // is too generic to know if the app is being throttled
                var msg = "Message delivery failed due to a socket error: " + e.Message + ". " +
                          "If the client is using MQTT this could be caused by throttling.";

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error(msg,
                    () => new { timeSpentMsecs, this.deviceId, Protocol = this.protocol.ToString(), e });

                throw new BrokenDeviceClientException(msg, e);
            }
            catch (DeviceNotFoundException e)
            {
                var msg = "Message delivery failed, device not found: " + e.Message;

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error(msg,
                    () => new { timeSpentMsecs, this.deviceId, Protocol = this.protocol.ToString(), e });

                throw new ResourceNotFoundException(msg, e);
            }
            catch (IOException e)
            {
                var msg = "Message delivery I/O error: " + e.Message;

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error(msg,
                    () => new { timeSpentMsecs, this.deviceId, Protocol = this.protocol.ToString(), e });

                throw new TelemetrySendIOException(msg, e);
            }
            catch (AggregateException aggEx) when (aggEx.InnerException != null)
            {
                var e = aggEx.InnerException;

                var msg = "Message delivery failed: " + e.Message;

                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error(msg,
                    () => new { timeSpentMsecs, this.deviceId, Protocol = this.protocol.ToString(), e });

                throw new TelemetrySendException(msg, e);
            }
            catch (ObjectDisposedException e)
            {
                var msg = "Message delivery failed due to internal client error: " + e.Message;

                // This error often occurs under CPU stress, apparently a bug in the internal AMQP library
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error(msg,
                    () => new { timeSpentMsecs, this.deviceId, Protocol = this.protocol.ToString(), e });

                throw new BrokenDeviceClientException(msg, e);
            }
            catch (Exception e)
            {
                var msg = "Message delivery failed due to unexpected error: " + e.Message;
                var timeSpentMsecs = GetTimeSpentMsecs();
                this.log.Error(msg,
                    () => new { timeSpentMsecs, this.deviceId, Protocol = this.protocol.ToString(), e });

                throw new TelemetrySendException(msg, e);
            }
        }

        private async Task SendProtobufMessageAsync(string message, DeviceModel.DeviceModelMessageSchema schema)
        {
            var eventMessage = default(Message);
            Type type = Assembly.GetExecutingAssembly().GetType(schema.ClassName, false);

            if (type != null)
            {
                object jsonObj = JsonConvert.DeserializeObject(message, type);

                MethodInfo methodInfo = Utilities.GetExtensionMethod("imessage", "Google.Protobuf", "ToByteArray");
                if (methodInfo != null)
                {
                    object result = methodInfo.Invoke(jsonObj, new object[] { jsonObj });
                    if (result != null)
                    {
                        byte[] byteArray = result as byte[];
                        eventMessage = new Message(byteArray);
                        eventMessage.Properties.Add(CLASSNAME_PROPERTY, schema.ClassName);
                    }
                    else
                    {
                        throw new InvalidDataException("Json message transformation to byte array yielded null");
                    }
                }
                else
                {
                    throw new ResourceNotFoundException($"Method: ToByteArray not found in {schema.ClassName}");
                }
            }
            else
            {
                throw new ResourceNotFoundException($"Type: {schema.ClassName} not found");
            }

            eventMessage.Properties.Add(CREATION_TIME_PROPERTY, DateTimeOffset.UtcNow.ToString(DATE_FORMAT));
            eventMessage.Properties.Add(MESSAGE_SCHEMA_PROPERTY, schema.Name);
            eventMessage.Properties.Add(CONTENT_PROPERTY, schema.Format.ToString());

            eventMessage.MessageSchema = schema.Name;
            eventMessage.CreationTimeUtc = DateTime.UtcNow;

            this.log.Debug("Sending message from device",
                () => new { this.deviceId, Schema = schema.Name });

            await this.SendRawMessageAsync(eventMessage);
        }

        private async Task SendJsonMessageAsync(string message, DeviceModel.DeviceModelMessageSchema schema)
        {
            var eventMessage = default(Message);

            eventMessage = new Message(Encoding.UTF8.GetBytes(message));
            eventMessage.ContentType = "application/json";
            eventMessage.ContentEncoding = "utf-8";
            eventMessage.Properties.Add(CREATION_TIME_PROPERTY, DateTimeOffset.UtcNow.ToString(DATE_FORMAT));
            eventMessage.Properties.Add(MESSAGE_SCHEMA_PROPERTY, schema.Name);
            eventMessage.Properties.Add(CONTENT_PROPERTY, schema.Format.ToString());
            eventMessage.MessageSchema = schema.Name;
            eventMessage.CreationTimeUtc = DateTime.UtcNow;

            this.log.Debug("Sending message from device",
                () => new { this.deviceId, Schema = schema.Name });

            await this.SendRawMessageAsync(eventMessage);
        }

        private TwinCollection SmartDictionaryToTwinCollection(ISmartDictionary dictionary)
        {
            var result = new TwinCollection();

            if (dictionary != null)
            {
                var items = dictionary.GetAll();

                foreach (KeyValuePair<string, object> item in items)
                {
                    try
                    {
                        // Use JToken for serialization
                        result[item.Key] = JToken.FromObject(item.Value);
                    }
                    catch (Exception e)
                    {
                        this.log.Error("Error while converting the dictionary to a twin collection", () => new { item.Key, item.Value, e });
                        throw;
                    }
                }
            }

            return result;
        }
    }
}
