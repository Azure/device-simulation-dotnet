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
        Task SendMessageAsync(string message, DeviceType.DeviceTypeMessageSchema schema);

        Task SendRawMessageAsync(Message message);

        Task DisconnectAsync();
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

        public async Task DisconnectAsync()
        {
            if (this.client != null)
            {
                await this.client.CloseAsync();
                this.client.Dispose();
            }
        }
    }
}
