// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceMethods
    {
        Task<MethodResponse> MethodHandlerAsync(MethodRequest methodRequest, object userContext);
    }

    public class DeviceMethods : IDeviceMethods
    {
        private static readonly TimeSpan retryMethodCallbackRegistration = TimeSpan.FromSeconds(10);

        private readonly Azure.Devices.Client.DeviceClient client;
        private readonly ILogger log;
        private readonly IScriptInterpreter scriptInterpreter;
        private readonly IDictionary<string, Script> cloudToDeviceMethods;
        private readonly Dictionary<string, object> deviceState;
        private readonly string deviceId;

        public DeviceMethods(
            Azure.Devices.Client.DeviceClient client,
            ILogger logger,
            IDictionary<string, Script> methods,
            Dictionary<string, object> deviceState,
            string device,
            IScriptInterpreter scriptInterpreter)
        {
            this.client = client;
            this.log = logger;
            this.cloudToDeviceMethods = methods;
            this.deviceId = device;
            this.deviceState = deviceState;
            this.scriptInterpreter = scriptInterpreter;
            this.SetupMethodCallbacksForDevice();
        }

        public async Task<MethodResponse> MethodHandlerAsync(MethodRequest methodRequest, object userContext)
        {
            this.log.Info("Creating task to execute method with json payload.", () => new
            {
                methodRequest.Name,
                methodRequest.DataAsJson,
                this.deviceId
            });

            // kick the method off on a separate thread & immediately return
            // not immediately returning blocks the client connection to the hub
            var t = Task.Run(() => MethodExecution(methodRequest));
            return new MethodResponse(Encoding.UTF8.GetBytes("Executed Method:" + methodRequest.Name), (int) HttpStatusCode.OK);
        }

        private void MethodExecution(MethodRequest methodRequest)
        {
            try
            {
                this.log.Info("Executing method with json payload.", () => new
                {
                    methodRequest.Name,
                    methodRequest.DataAsJson,
                    this.deviceId
                });
                var scriptContext = new Dictionary<string, object>
                {
                    ["currentTime"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
                    ["deviceId"] = this.deviceId,
                };
                if (methodRequest.DataAsJson != "null")
                    AddPayloadToContext(methodRequest.DataAsJson, scriptContext);

                this.log.Debug("Executing method for device", () => new
                {
                    this.deviceId,
                    deviceState = this.deviceState,
                    methodRequest.Name
                });

                // ignore the return state - state updates are handled by callbacks from the script
                this.scriptInterpreter.Invoke(
                    this.cloudToDeviceMethods[methodRequest.Name],
                    scriptContext,
                    this.deviceState);

                //TODO: Implement all other Javascript methods across all devices
                //for Firmware update (FirmwareUpdateStatus) device needs to update status to 
                //command sent, image downloaded, applying firmware, complete, rebooting, ... then set to blank

                this.log.Debug("Executed method for device", () => new { this.deviceId, methodRequest.Name });
                return;
            }
            catch (Exception e)
            {
                this.log.Error("Error while executing method for device",
                    () => new
                    {
                        methodRequest.Name,
                        methodRequest.DataAsJson,
                        this.deviceId,
                        e
                    });
                return;
            }
        }

        private void AddPayloadToContext(string dataAsJson, Dictionary<string, object> scriptContext)
        {
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataAsJson);
            foreach (var item in values)
                scriptContext.Add(item.Key, item.Value);
        }

        private void SetupMethodCallbacksForDevice()
        {
            this.log.Debug("Setting up methods for device.", () => new
            {
                this.cloudToDeviceMethods.Count,
                this.deviceId
            });

            // walk this list and add a method handler for each method specified
            foreach (var item in this.cloudToDeviceMethods)
            {
                this.log.Debug("Setting up method for device.", () => new { item.Key, this.deviceId });

                this.client.SetMethodHandlerAsync(item.Key, MethodHandlerAsync, null).Wait(retryMethodCallbackRegistration);

                this.log.Debug("Method for device setup successfully", () => new
                {
                    item.Key,
                    this.deviceId
                });
            }
        }
    }
}
