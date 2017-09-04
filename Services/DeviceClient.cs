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
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceClient
    {
        IoTHubProtocol Protocol { get; }

        Task SendMessageAsync(string message, DeviceModel.DeviceModelMessageSchema schema);

        Task SendRawMessageAsync(Message message);

        Task DisconnectAsync();

        Task UpdateTwinAsync(Device device);

        void RegisterMethodsForDevice(Dictionary<string, object> deviceState, IDictionary<string, Script> methods);

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
        private readonly string deviceId;
        private readonly IScriptInterpreter scriptInterpreter;

        //used to create method pointers for the device for the IoTHub to callback to
        private DeviceMethods deviceMethods;

        public DeviceClient(
            Azure.Devices.Client.DeviceClient client,
            IoTHubProtocol protocol,
            ILogger logger, 
            string deviceId,
            IScriptInterpreter scriptInterpreter = null)
        {
            this.client = client;
            this.protocol = protocol;
            this.log = logger;
            this.deviceId = deviceId;
            this.scriptInterpreter = scriptInterpreter;
        }

        public IoTHubProtocol Protocol { get { return this.protocol; } }

        public void RegisterMethodsForDevice(Dictionary<string, object> deviceState, IDictionary<string, 
            Script> methods)

        {
            log.Debug("Attempting to setup methods for device", () => new 
            {
                this.deviceId                
            });

            if (this.scriptInterpreter == null)
            {
                log.Error("No script interpreter was found for registering methods.", () => 
                    new{this.deviceId});
                //todo throw
            }

            //TODO: Inject through the constructor instead
            this.deviceMethods = new DeviceMethods(this.client, log, methods, deviceState, this.deviceId, 
                this.scriptInterpreter);

        }

        public async Task SendMessageAsync(string message, DeviceModel.DeviceModelMessageSchema schema)
        {
            var eventMessage = new Message(Encoding.UTF8.GetBytes(message));
            eventMessage.Properties.Add(CreationTimeProperty, DateTimeOffset.UtcNow.ToString(DateFormat));
            eventMessage.Properties.Add(MessageSchemaProperty, schema.Name);
            eventMessage.Properties.Add(ContentProperty, "JSON");

            await this.SendRawMessageAsync(eventMessage);

            log.Debug("SendMessageAsync for device", () => new
            {
                this.deviceId
            });
        }

        public async Task SendRawMessageAsync(Message message)
        {
            try
            {
                await this.client.SendEventAsync(message);

                log.Debug("SendRawMessageAsync for device", () => new
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

        public async Task UpdateTwinAsync(Device device)
        {
            var azureTwin = await this.GetTwinAsync();

            // Remove properties
            var props = azureTwin.Properties.Reported.GetEnumerator();
            while (props.MoveNext())
            {
                var current = (KeyValuePair<string, object>)props.Current;

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
