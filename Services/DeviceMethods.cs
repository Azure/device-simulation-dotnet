// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceMethods
    {
        Task RegisterMethodsAsync(
            string deviceId,
            IDictionary<string, Script> methods,
            ISmartDictionary deviceState,
            ISmartDictionary deviceProperties);
    }

    public class DeviceMethods : IDeviceMethods
    {
        private readonly IDeviceClientWrapper client;
        private readonly ILogger log;
        private readonly IScriptInterpreter scriptInterpreter;
        private IDictionary<string, Script> cloudToDeviceMethods;
        private ISmartDictionary deviceState;
        private ISmartDictionary deviceProperties;
        private string deviceId;
        private bool isRegistered;

        public DeviceMethods(
            IDeviceClientWrapper client,
            ILogger logger,
            IScriptInterpreter scriptInterpreter)
        {
            this.client = client;
            this.log = logger;
            this.scriptInterpreter = scriptInterpreter;
            this.deviceId = string.Empty;
            this.isRegistered = false;
        }

        public async Task RegisterMethodsAsync(
            string deviceId,
            IDictionary<string, Script> methods,
            ISmartDictionary deviceState,
            ISmartDictionary deviceProperties)
        {
            if (methods == null) return;

            if (this.isRegistered)
            {
                this.log.Error("Application error, each device must have a separate instance", () => { });
                throw new Exception("Application error, each device must have a separate instance of " + this.GetType().FullName);
            }

            this.deviceId = deviceId;
            this.cloudToDeviceMethods = methods;
            this.deviceState = deviceState;
            this.deviceProperties = deviceProperties;

            this.log.Debug("Setting up methods for device.", () => new
            {
                this.deviceId,
                methodsCount = this.cloudToDeviceMethods.Count
            });

            // walk this list and add a method handler for each method specified
            foreach (var item in this.cloudToDeviceMethods)
            {
                this.log.Debug("Setting up method for device.", () => new { item.Key, this.deviceId });

                await this.client.SetMethodHandlerAsync(item.Key, this.ExecuteMethodAsync, null);

                this.log.Debug("Method for device setup successfully", () => new
                {
                    this.deviceId,
                    methodName = item.Key
                });
            }

            this.isRegistered = true;
        }

        public Task<MethodResponse> ExecuteMethodAsync(MethodRequest methodRequest, object userContext)
        {
            try
            {
                this.log.Debug("Creating task to execute method with json payload.", () => new
                {
                    this.deviceId,
                    methodName = methodRequest.Name,
                    methodRequest.DataAsJson
                });

                // Kick the method off on a separate thread & immediately return
                // Not immediately returning would block the client connection to the hub
                var t = Task.Run(() => this.MethodExecution(methodRequest));

                return Task.FromResult(new MethodResponse((int) HttpStatusCode.OK));
            }
            catch (Exception e)
            {
                this.log.Error("Failed executing method.", () => new { methodRequest, e });
                return Task.FromResult(new MethodResponse((int) HttpStatusCode.InternalServerError));
            }
        }

        private void MethodExecution(MethodRequest methodRequest)
        {
            try
            {
                this.log.Debug("Executing method with json payload.", () => new
                {
                    this.deviceId,
                    methodName = methodRequest.Name,
                    methodRequest.DataAsJson
                });

                var scriptContext = new Dictionary<string, object>
                {
                    ["currentTime"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
                    ["deviceId"] = this.deviceId,
                    // TODO: add "deviceModel" so that the method scripts can use it like the "state" scripts
                    //       https://github.com/Azure/device-simulation-dotnet/issues/91
                    //["deviceModel"] = this.device.
                };
                if (methodRequest.DataAsJson != "null")
                    this.AddPayloadToContext(methodRequest.DataAsJson, scriptContext);

                this.log.Debug("Executing method for device", () => new
                {
                    this.deviceId,
                    methodName = methodRequest.Name,
                    this.deviceState
                });

                // ignore the return state - state updates are handled by callbacks from the script
                this.scriptInterpreter.Invoke(
                    this.cloudToDeviceMethods[methodRequest.Name],
                    scriptContext,
                    this.deviceState,
                    this.deviceProperties);

                this.log.Debug("Executed method for device", () => new { this.deviceId, methodRequest.Name, this.deviceState, this.deviceProperties });
            }
            catch (Exception e)
            {
                this.log.Error("Error while executing method for device",
                    () => new
                    {
                        this.deviceId,
                        methodName = methodRequest.Name,
                        methodRequest.DataAsJson,
                        e
                    });
            }
        }

        private void AddPayloadToContext(string dataAsJson, Dictionary<string, object> scriptContext)
        {
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataAsJson);
            foreach (var item in values)
                scriptContext.Add(item.Key, item.Value);
        }
    }
}
