// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceClient
    {
        Task ConnectAsync();
        Task DisconnectAsync();
        Task SendMessageAsync(string message, DeviceModel.DeviceModelMessageSchema schema);
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
            // TODO: HTTP clients don't "connect", find out how HTTP connections are measured and throttled
            //       https://github.com/Azure/device-simulation-dotnet/issues/85
            if (this.client != null && !this.connected)
            {
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
    }
}
