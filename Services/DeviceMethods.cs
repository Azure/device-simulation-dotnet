// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using System.Net;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceMethods
    {
        Task<MethodResponse> MethodExecution(MethodRequest methodRequest, object userContext);
    }
    
    public class DeviceMethods : IDeviceMethods
    {
        private static readonly TimeSpan retryMethodCallbackRegistration = TimeSpan.FromSeconds(10);

        private readonly Azure.Devices.Client.DeviceClient client;
        private readonly ILogger log;

        private IScriptInterpreter scriptInterpreter;
        private IDictionary<string, Script> cloudToDeviceMethods;
        private Dictionary<string, object> deviceState;
        private string deviceId;

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
        
        public async Task<MethodResponse> MethodExecution(MethodRequest methodRequest, object userContext)
        {
            try
            {
                this.log.Info("Executing method with json payload.", () => new {methodRequest.Name,
                    methodRequest.DataAsJson, this.deviceId});
                var scriptContext = new Dictionary<string, object>
                {
                    ["currentTime"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
                    ["deviceId"] = this.deviceId
                };

                this.log.Debug("Executing method for device", () => new { this.deviceId,
                    deviceState = this.deviceState, methodRequest.Name });

                // ignore the return state - state updates are handled by callbacks from the script
                this.scriptInterpreter.Invoke(
                    this.cloudToDeviceMethods[methodRequest.Name],
                    scriptContext,
                    this.deviceState);

                //TODO: Implement all other Javascript methods across all devices
                //for Firmware update (FirmwareUpdateStatus) device needs to update status to 
                //command sent, image downloaded, applying firmware, complete, rebooting, ... then set to blank

                this.log.Debug("Invoked method for device", () => new { this.deviceId, methodRequest.Name });

                string result = "'Method " + methodRequest.Name + " successfully executed.'";

                this.log.Info("Executed method.", () => new {methodRequest.Name});
                byte[] resultEncoded = Encoding.UTF8.GetBytes(result);
                return new MethodResponse(resultEncoded, (int)HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                this.log.Error("Error while executing method for device",
                    () => new {
                        methodRequest.Name,
                        methodRequest.DataAsJson,
                        this.deviceId,
                        e
                    });
                return new MethodResponse(Encoding.UTF8.GetBytes("Error while executing method for device"), 
                    (int)HttpStatusCode.InternalServerError);
            }
        }

        private void SetupMethodCallbacksForDevice()
        {
            this.log.Debug("Setting up methods for device.", () => new {
                this.cloudToDeviceMethods.Count,
                this.deviceId
            });

            // walk this list and add a method handler for each method specified
            foreach (var item in this.cloudToDeviceMethods)
            {
                this.log.Debug("Setting up method for device.", () => new {item.Key,this.deviceId});

                this.client.SetMethodHandlerAsync(item.Key, MethodExecution, null). 
                    Wait(retryMethodCallbackRegistration);

                this.log.Debug("Method for device setup successfully", () => new {
                    item.Key,
                    this.deviceId
                });
            }
        }
    }
}
