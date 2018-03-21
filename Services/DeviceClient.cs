// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceClient
    {
        IoTHubProtocol Protocol { get; }
        Task ConnectAsync();
        Task DisconnectAsync();
        Task SendMessageAsync(string message, DeviceModel.DeviceModelMessageSchema schema);
        Task UpdatePropertiesAsync(ISmartDictionary properties);
        Task RegisterMethodsForDeviceAsync(IDictionary<string, Script> methods, Dictionary<string, object> deviceState);
    }

    public class DeviceClient : IDeviceClient
    {
        private const string DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";

        // See also https://github.com/Azure/toketi-iothubreact/blob/master/src/main/scala/com/microsoft/azure/iot/iothubreact/MessageFromDevice.scala
        private const string CREATION_TIME_PROPERTY = "$$CreationTimeUtc";

        private const string MESSAGE_SCHEMA_PROPERTY = "$$MessageSchema";
        private const string CONTENT_PROPERTY = "$$ContentType";

        private readonly string deviceId;
        private readonly IoTHubProtocol protocol;
        private readonly Azure.Devices.Client.DeviceClient client;
        private readonly ILogger log;

        private bool connected;

        public IoTHubProtocol Protocol => this.protocol;

        public DeviceClient(
            string deviceId,
            IoTHubProtocol protocol,
            Azure.Devices.Client.DeviceClient client,
            ILogger logger)
        {
            this.deviceId = deviceId;
            this.protocol = protocol;
            this.client = client;
            this.log = logger;
        }

        public async Task ConnectAsync()
        {
            if (this.client != null && !this.connected)
            {
                // TODO: HTTP clients don't "connect", find out how HTTP connections are measured and throttled
                //       https://github.com/Azure/device-simulation-dotnet/issues/85
                await this.client.OpenAsync();
                this.connected = true;
            }
        }

        public async Task DisconnectAsync()
        {
            this.connected = false;
            if (this.client != null)
            {
                await this.client.CloseAsync();
                this.client.Dispose();
            }
        }

        public Task RegisterMethodsForDeviceAsync(
            IDictionary<string, Script> methods,
            Dictionary<string, object> deviceState)
        {
            /* TEMPORARY DISABLED
            this.log.Debug("Attempting to register device methods",
                () => new { this.deviceId });

            await this.deviceMethods.RegisterMethodsAsync(this.deviceId, methods, deviceState);
            */
            return Task.CompletedTask;
        }

        public async Task SendMessageAsync(string message, DeviceModel.DeviceModelMessageSchema schema)
        {
            var eventMessage = new Message(Encoding.UTF8.GetBytes(message));
            eventMessage.Properties.Add(CREATION_TIME_PROPERTY, DateTimeOffset.UtcNow.ToString(DATE_FORMAT));
            eventMessage.Properties.Add(MESSAGE_SCHEMA_PROPERTY, schema.Name);
            eventMessage.Properties.Add(CONTENT_PROPERTY, "JSON");

            eventMessage.ContentType = "application/json";
            eventMessage.ContentEncoding = "utf-8";
            eventMessage.MessageSchema = schema.Name;
            eventMessage.CreationTimeUtc = DateTime.UtcNow;

            this.log.Debug("Sending message from device",
                () => new { this.deviceId, Schema = schema.Name });

            await this.SendRawMessageAsync(eventMessage);
        }

        /// <summary>
        /// Updates the reported properties in the device twin on the IoT Hub
        /// </summary>
        public async Task UpdatePropertiesAsync(ISmartDictionary properties)
        {
            if (!this.connected) await this.ConnectAsync();

            var reportedProperties = SmartDictionaryToTwinCollection(properties);

            await this.client.UpdateReportedPropertiesAsync(reportedProperties);

            this.log.Debug("Update reported properties for device", () => new
            {
                this.deviceId,
                ReportedProperties = reportedProperties
            });

            return;
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
            }
        }

        private static TwinCollection SmartDictionaryToTwinCollection(ISmartDictionary dictionary)
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
                        Console.WriteLine(e);
                        throw;
                    }
                }
            }

            return result;
        }
    }
}
