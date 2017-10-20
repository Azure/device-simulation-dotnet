﻿// Copyright (c) Microsoft. All rights reserved.

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
        Task RegisterMethodsAsync(
            string deviceId,
            IDictionary<string, Script> methods,
            Dictionary<string, object> deviceState);
    }

    public class DeviceMethods : IDeviceMethods
    {
        private readonly Azure.Devices.Client.DeviceClient client;
        private readonly ILogger log;
        private readonly IScriptInterpreter scriptInterpreter;
        private IDictionary<string, Script> cloudToDeviceMethods;
        private Dictionary<string, object> deviceState;
        private string deviceId;

        public DeviceMethods(
            Azure.Devices.Client.DeviceClient client,
            ILogger logger,
            IScriptInterpreter scriptInterpreter)
        {
            this.client = client;
            this.log = logger;
            this.scriptInterpreter = scriptInterpreter;
            this.deviceId = string.Empty;
        }

        public async Task RegisterMethodsAsync(
            string deviceId,
            IDictionary<string, Script> methods,
            Dictionary<string, object> deviceState)
        {
            if (this.deviceId != string.Empty)
            {
                this.log.Error("Application error, each device must have a separate instance", () => { });
                throw new Exception("Application error, each device must have a separate instance of " + this.GetType().FullName);
            }

            this.deviceId = deviceId;
            this.cloudToDeviceMethods = methods;
            this.deviceState = deviceState;

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
        }

        public Task<MethodResponse> ExecuteMethodAsync(MethodRequest methodRequest, object userContext)
        {
            try
            {
                this.log.Info("Creating task to execute method with json payload.", () => new
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
            catch(Exception e)
            {
                log.Error("Failed executing method.", () => new { methodRequest, e });
                return Task.FromResult(new MethodResponse((int)HttpStatusCode.InternalServerError));
            }
        }

        private void MethodExecution(MethodRequest methodRequest)
        {
            try
            {
                this.log.Info("Executing method with json payload.", () => new
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
                    this.deviceState);

                this.log.Debug("Executed method for device", () => new { this.deviceId, methodRequest.Name });
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
