﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
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
        void DisposeInternalClient();
        Task SendMessageAsync(string message, DeviceModel.DeviceModelMessageSchema schema);
        Task RegisterMethodsForDeviceAsync(IDictionary<string, Script> methods, ISmartDictionary deviceState, ISmartDictionary deviceProperties);
        Task RegisterDesiredPropertiesUpdateAsync(ISmartDictionary deviceProperties);
        Task UpdatePropertiesAsync(ISmartDictionary deviceProperties);
    }

    public class DeviceClient : IDeviceClient
    {
        private const string DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";

        // See also https://github.com/Azure/toketi-iothubreact/blob/master/src/main/scala/com/microsoft/azure/iot/iothubreact/MessageFromDevice.scala
        private const string CREATION_TIME_PROPERTY = "$$CreationTimeUtc";
        private const string CLASSNAME_PROPERTY = "$$ClassName";

        private const string MESSAGE_SCHEMA_PROPERTY = "$$MessageSchema";
        private const string CONTENT_PROPERTY = "$$ContentType";

        private readonly string deviceId;
        private readonly IoTHubProtocol protocol;
        private readonly IDeviceClientWrapper client;
        private readonly IDeviceMethods deviceMethods;
        private readonly IDevicePropertiesRequest propertiesUpdateRequest;
        private readonly ILogger log;

        private bool connected;

        public string DeviceId => this.deviceId;
        public IoTHubProtocol Protocol => this.protocol;

        public DeviceClient(
            string deviceId,
            IoTHubProtocol protocol,
            IDeviceClientWrapper client,
            IDeviceMethods deviceMethods,
            ILogger logger)
        {
            this.deviceId = deviceId;
            this.protocol = protocol;
            this.client = client;
            this.deviceMethods = deviceMethods;
            this.log = logger;

            this.propertiesUpdateRequest = new DeviceProperties(client, this.log);
        }

        public async Task ConnectAsync()
        {
            if (this.client != null && !this.connected)
            {
                try
                {
                    // TODO: HTTP clients don't "connect", find out how HTTP connections are measured and throttled
                    //       https://github.com/Azure/device-simulation-dotnet/issues/85
                    await this.client.OpenAsync();
                    this.connected = true;
                }
                catch (UnauthorizedException e)
                {
                    // Note: this exception might not occur with HTTP
                    // TODO: test for HTTP

                    this.log.Error("Device connection auth failed", () => new { this.deviceId, this.protocol, e });
                    throw new DeviceAuthFailedException(e);
                }
                catch (DeviceNotFoundException e)
                {
                    this.log.Error("Device not found", () => new { this.deviceId, this.protocol, e });
                    throw new DeviceNotFoundException(this.deviceId);
                }
            }
        }

        public async Task DisconnectAsync()
        {
            this.connected = false;
            if (this.client != null)
            {
                await this.client.CloseAsync();
                this.DisposeInternalClient();
            }
        }

        public void DisposeInternalClient()
        {
            try
            {
                // Note: this is important to ensure that any pending task inside the SDK is stopped
                this.client.Dispose();
            }
            catch (Exception e)
            {
                this.log.Error("Something went wrong while disposing the device client",
                    () => new { this.deviceId, this.protocol, e });
            }
        }

        public async Task RegisterMethodsForDeviceAsync(
            IDictionary<string, Script> methods,
            ISmartDictionary deviceState,
            ISmartDictionary deviceProperties)
        {
            this.log.Debug("Attempting to register device methods",
                () => new { this.deviceId });

            await this.deviceMethods.RegisterMethodsAsync(this.deviceId, methods, deviceState, deviceProperties);
        }

        public async Task RegisterDesiredPropertiesUpdateAsync(ISmartDictionary deviceProperties)
        {
            this.log.Debug("Attempting to register desired property notifications for device",
                () => new { this.deviceId });

            await this.propertiesUpdateRequest.RegisterChangeUpdateAsync(this.deviceId, deviceProperties);
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
            try
            {
                var reportedProperties = this.SmartDictionaryToTwinCollection(properties);
                await this.client.UpdateReportedPropertiesAsync(reportedProperties);
                this.log.Debug("Update reported properties for device", () => new
                {
                    this.deviceId,
                    reportedProperties
                });
            }
            catch (KeyNotFoundException e)
            {
                // This exception sometimes occurs when calling UpdateReportedPropertiesAsync.
                // Still unknown why, apparently an issue with the internal AMQP library
                // used by IoT SDK. We need to collect extra information to report the issue.
                this.log.Error("Unexpected error, failed to update reported properties",
                    () => new
                    {
                        Protocol = this.protocol.ToString(),
                        Exception = e.GetType().FullName,
                        e.Message,
                        e.StackTrace,
                        e.Data,
                        e.Source,
                        e.TargetSite,
                        e.InnerException // This appears to always be null in this scenario
                    });
            }
            catch (Exception e)
            {
                this.log.Error("Failed to update reported properties",
                    () => new
                    {
                        Protocol = this.protocol.ToString(),
                        ExceptionMessage = e.Message,
                        Exception = e.GetType().FullName,
                        e.InnerException
                    });
            }
        }

        private async Task SendRawMessageAsync(Message message)
        {
            try
            {
                await this.client.SendEventAsync(message);

                this.log.Debug("SendRawMessageAsync for device", () => new
                {
                    this.deviceId
                });
            }
            catch (TimeoutException e)
            {
                this.log.Error("Message delivery timed out",
                    () => new
                    {
                        Protocol = this.protocol.ToString(),
                        ExceptionMessage = e.Message,
                        Exception = e.GetType().FullName,
                        e.InnerException
                    });

                throw new TelemetrySendTimeoutException("Message delivery timed out with " + e.Message, e);
            }
            catch (IOException e)
            {
                this.log.Error("Message delivery IOExcepotion",
                    () => new
                    {
                        Protocol = this.protocol.ToString(),
                        ExceptionMessage = e.Message,
                        Exception = e.GetType().FullName,
                        e.InnerException
                    });

                throw new TelemetrySendIOException("Message delivery I/O failed with " + e.Message, e);
            }
            catch (AggregateException aggEx) when (aggEx.InnerException != null)
            {
                var e = aggEx.InnerException;

                this.log.Error("Message delivery failed",
                    () => new
                    {
                        Protocol = this.protocol.ToString(),
                        ExceptionMessage = e.Message,
                        Exception = e.GetType().FullName,
                        e.InnerException
                    });

                throw new TelemetrySendException("Message delivery failed with " + e.Message, e);
            }
            catch (ObjectDisposedException e)
            {
                // This error often occurs under CPU stress, apparently a bug in the internal AMQP library
                this.log.Error("Message delivery failed, internal client failure",
                    () => new
                    {
                        Protocol = this.protocol.ToString(),
                        ExceptionMessage = e.Message,
                        Exception = e.GetType().FullName,
                        e.InnerException
                    });

                throw new BrokenDeviceClientException("Message delivery failed, internal client failure", e);
            }
            catch (Exception e)
            {
                this.log.Error("Message delivery failed",
                    () => new
                    {
                        Protocol = this.protocol.ToString(),
                        ExceptionMessage = e.Message,
                        Exception = e.GetType().FullName,
                        e.InnerException
                    });

                throw new TelemetrySendException("Message delivery failed with " + e.Message, e);
            }
        }

        private async Task SendProtobufMessageAsync(string message, DeviceModel.DeviceModelMessageSchema schema)
        {
            var eventMessage = default(Message);
            string className = schema.ClassName;
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
