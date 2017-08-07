// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceClient
    {
        Task SendMessageAsync(string message, DeviceType.DeviceTypeMessageSchema schema);

        Task SendRawMessageAsync(Message message);

        Task DisconnectAsync();

        Task UpdateTwinAsync(DeviceServiceModel device);
    }

    public class DeviceClient : IDeviceClient
    {
        private const string DateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";

        // See also https://github.com/Azure/toketi-iothubreact/blob/master/src/main/scala/com/microsoft/azure/iot/iothubreact/MessageFromDevice.scala
        private const string CreationTimeProperty = "$$CreationTimeUtc";

        private const string MessageSchemaProperty = "$$MessageSchema";
        private const string ContentProperty = "$$ContentType";

        private readonly Azure.Devices.Client.DeviceClient client;
        private readonly ILogger log;
        private readonly IoTHubProtocol protocol;

        public DeviceClient(
            Azure.Devices.Client.DeviceClient client,
            IoTHubProtocol protocol,
            ILogger logger)
        {
            this.client = client;
            this.log = logger;
            this.protocol = protocol;
        }

        public async Task SendMessageAsync(string message, DeviceType.DeviceTypeMessageSchema schema)
        {
            var eventMessage = new Message(Encoding.UTF8.GetBytes(message));
            eventMessage.Properties.Add(CreationTimeProperty, DateTimeOffset.UtcNow.ToString(DateFormat));
            eventMessage.Properties.Add(MessageSchemaProperty, schema.Name);
            eventMessage.Properties.Add(ContentProperty, "JSON");

            await this.SendRawMessageAsync(eventMessage);
        }

        public async Task SendRawMessageAsync(Message message)
        {
            try
            {
                await this.client.SendEventAsync(message);
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

        public async Task UpdateTwinAsync(DeviceServiceModel device)
        {
            var azureTwin = await this.GetTwinAsync();

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

            await this.client.UpdateReportedPropertiesAsync(reportedProperties);
        }

        public async Task DisconnectAsync()
        {
            if (this.client != null)
            {
                await this.client.CloseAsync();
                this.client.Dispose();
            }
        }

        private async Task<Twin> GetTwinAsync()
        {
            return await this.client.GetTwinAsync();
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
