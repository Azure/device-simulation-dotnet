// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceMethods
    {
        Task RegisterMethodsAsync(
            IDeviceClientWrapper client,
            string deviceId,
            IDictionary<string, Script> methods,
            ISmartDictionary deviceState,
            ISmartDictionary deviceProperties,
            IScriptInterpreter scriptInterpreter);
    }

    public class DeviceMethods : IDeviceMethods
    {
        private readonly ILogger log;
        private readonly IDiagnosticsLogger diagnosticsLogger;
        private readonly bool methodsEnabled;
        private IDictionary<string, Script> cloudToDeviceMethods;
        private ISmartDictionary deviceState;
        private ISmartDictionary deviceProperties;
        private string deviceId;
        private bool isRegistered;

        public DeviceMethods(
            IServicesConfig servicesConfig,
            ILogger logger,
            IDiagnosticsLogger diagnosticsLogger)
        {
            this.log = logger;
            this.diagnosticsLogger = diagnosticsLogger;
            this.methodsEnabled = servicesConfig.C2DMethodsEnabled;
            this.deviceId = string.Empty;
            this.isRegistered = false;
        }

        public async Task RegisterMethodsAsync(
            IDeviceClientWrapper client,
            string deviceId,
            IDictionary<string, Script> methods,
            ISmartDictionary deviceState,
            ISmartDictionary deviceProperties,
            IScriptInterpreter scriptInterpreter)
        {
            if (methods == null) return;

            if (this.isRegistered)
            {
                this.log.Error("Application error, each device must have a separate instance");
                throw new Exception("Application error, each device must have a separate instance of " + this.GetType().FullName);
            }

            if (!this.methodsEnabled)
            {
                this.isRegistered = true;
                this.log.Debug("Skipping methods registration, methods are disabled in the global configuration.");
                return;
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

                await client.SetMethodHandlerAsync(item.Key, this.ExecuteMethodAsync, scriptInterpreter);

                this.log.Debug("Method for device setup successfully", () => new
                {
                    this.deviceId,
                    methodName = item.Key
                });
            }

            this.isRegistered = true;
        }

        public Task<MethodResponse> ExecuteMethodAsync(MethodRequest methodRequest, object scriptInterpreter)
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
                var t = Task.Run(() => this.MethodExecution(methodRequest, (IScriptInterpreter) scriptInterpreter));

                return Task.FromResult(new MethodResponse((int) HttpStatusCode.OK));
            }
            catch (Exception e)
            {
                var msg = "Failed executing method.";
                this.log.Error(msg, () => new { methodRequest, e });
                this.diagnosticsLogger.LogServiceError(msg, new { methodRequest, e.Message });
                return Task.FromResult(new MethodResponse((int) HttpStatusCode.InternalServerError));
            }
        }

        private void MethodExecution(MethodRequest methodRequest, IScriptInterpreter scriptInterpreter)
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
                scriptInterpreter.Invoke(
                    this.cloudToDeviceMethods[methodRequest.Name],
                    scriptContext,
                    this.deviceState,
                    this.deviceProperties);

                this.log.Debug("Executed method for device", () => new { this.deviceId, methodRequest.Name, this.deviceState, this.deviceProperties });
            }
            catch (Exception e)
            {
                var msg = "Error while executing method for device";
                this.log.Error(msg,
                    () => new
                    {
                        this.deviceId,
                        methodName = methodRequest.Name,
                        methodRequest.DataAsJson,
                        e
                    });
                this.diagnosticsLogger.LogServiceError(msg,
                    new
                    {
                        this.deviceId,
                        methodName = methodRequest.Name,
                        methodRequest.DataAsJson,
                        e.Message
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
