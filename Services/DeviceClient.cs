﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceClient
    {
        IoTHubProtocol Protocol { get; }

        Task ConnectAsync();

        Task DisconnectAsync();

        Task SendMessageAsync(string message, DeviceModel.DeviceModelMessageSchema schema);

        Task UpdateTwinAsync(Device device);

        void RegisterMethodsForDevice(IDictionary<string, Script> methods, Dictionary<string, object> deviceState);
    }

    public class DeviceClient : IDeviceClient
    {
        private const string DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:sszzz";

        // See also https://github.com/Azure/toketi-iothubreact/blob/master/src/main/scala/com/microsoft/azure/iot/iothubreact/MessageFromDevice.scala
        private const string CREATION_TIME_PROPERTY = "$$CreationTimeUtc";

        private const string MESSAGE_SCHEMA_PROPERTY = "$$MessageSchema";
        private const string CONTENT_PROPERTY = "$$ContentType";

        private readonly Azure.Devices.Client.DeviceClient client;
        private readonly ILogger log;
        private readonly IoTHubProtocol protocol;
        private readonly IRateLimiting rateLimiting;
        private readonly string deviceId;
        private readonly IScriptInterpreter scriptInterpreter;

        //used to create method pointers for the device for the IoTHub to callback to
        // ReSharper disable once NotAccessedField.Local
        private DeviceMethods deviceMethods;

        private bool connected;

        public IoTHubProtocol Protocol => this.protocol;

        public DeviceClient(
            Azure.Devices.Client.DeviceClient client,
            IoTHubProtocol protocol,
            IRateLimiting rateLimiting,
            ILogger logger,
            string deviceId,
            IScriptInterpreter scriptInterpreter)
        {
            this.client = client;
            this.protocol = protocol;
            this.rateLimiting = rateLimiting;
            this.log = logger;
            this.deviceId = deviceId;
            this.scriptInterpreter = scriptInterpreter;
            this.connected = false;
        }

        public async Task ConnectAsync()
        {
            if (this.client != null && !this.connected)
            {
                // TODO: HTTP clients don't "connect", find out how HTTP connections are measured and throttled
                //       https://github.com/Azure/device-simulation-dotnet/issues/85
                await this.rateLimiting.LimitConnectionsAsync(() => this.client.OpenAsync());
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

        public void RegisterMethodsForDevice(IDictionary<string, Script> methods,
            Dictionary<string, object> deviceState)
        {
            this.log.Debug("Attempting to setup methods for device", () => new
            {
                this.deviceId
            });

            // TODO: Inject through the constructor instead
            this.deviceMethods = new DeviceMethods(this.client, this.log, methods, deviceState, this.deviceId,
                this.scriptInterpreter);
        }

        public async Task SendMessageAsync(string message, DeviceModel.DeviceModelMessageSchema schema)
        {
            var eventMessage = new Message(Encoding.UTF8.GetBytes(message));
            eventMessage.Properties.Add(CREATION_TIME_PROPERTY, DateTimeOffset.UtcNow.ToString(DATE_FORMAT));
            eventMessage.Properties.Add(MESSAGE_SCHEMA_PROPERTY, schema.Name);
            eventMessage.Properties.Add(CONTENT_PROPERTY, "JSON");

            await this.SendRawMessageAsync(eventMessage);

            this.log.Debug("SendMessageAsync for device", () => new
            {
                this.deviceId
            });
        }

        public async Task UpdateTwinAsync(Device device)
        {
            if (!this.connected) await this.ConnectAsync();

            var azureTwin = await this.rateLimiting.LimitTwinReadsAsync(
                () => this.client.GetTwinAsync());

            // Remove properties
            var props = azureTwin.Properties.Reported.GetEnumerator();
            while (props.MoveNext())
            {
                var current = (KeyValuePair<string, object>) props.Current;

                if (!device.Twin.ReportedProperties.ContainsKey(current.Key))
                {
                    this.log.Debug("Removing key", () => new { current.Key });
                    azureTwin.Properties.Reported[current.Key] = null;
                }
            }

            // Write properties
            var reportedProperties = DictionaryToTwinCollection(device.Twin.ReportedProperties);
            await this.rateLimiting.LimitTwinWritesAsync(
                () => this.client.UpdateReportedPropertiesAsync(reportedProperties));
        }

        private async Task SendRawMessageAsync(Message message)
        {
            try
            {
                if (!this.connected) await this.ConnectAsync();

                await this.rateLimiting.LimitMessagesAsync(
                    () => this.client.SendEventAsync(message));

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

        private static TwinCollection DictionaryToTwinCollection(Dictionary<string, JToken> x)
        {
            var result = new TwinCollection();

            if (x != null)
            {
                foreach (KeyValuePair<string, JToken> item in x)
                {
                    try
                    {
                        result[item.Key] = item.Value;
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
