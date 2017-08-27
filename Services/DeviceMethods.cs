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
using System.Net;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceMethods
    {
        Task<MethodResponse> MethodExecution(MethodRequest methodRequest, object userContext);
    }
    
    public class DeviceMethods : IDeviceMethods
    {
        private readonly Azure.Devices.Client.DeviceClient client;
        private readonly ILogger log;
        private readonly IScriptInterpreter scriptInterpreter;
        IDictionary<string, Script> cloudToDeviceMethods;
        private string deviceId;

        public DeviceMethods(
            Azure.Devices.Client.DeviceClient client,
            ILogger logger,
            IDictionary<string, Script> methods, string device)
        {
            this.client = client;
            this.log = logger;
            this.cloudToDeviceMethods = methods;
            deviceId = device;

            this.SetupMethodCallbacksForDevice();
        }
        
        public async Task<MethodResponse> MethodExecution(MethodRequest methodRequest, object userContext)
        {
            //TODO: exception handling needs added for this callback (it's on its own thread)

            this.log.Info("Executing method with json payload.", () => new {methodRequest.Name,
                                                                            methodRequest.DataAsJson,
                                                                            deviceId
            });

            //TODO: Use JavaScript engine to execute methods.
            //lock (actor.DeviceState)
            //{
            //    actor.DeviceState = this.scriptInterpreter.Invoke(
            //        this.deviceModel.Simulation.Script,
            //        scriptContext,
            //        actor.DeviceState);
            //}

            string result = "'I am the simulator.  Someone called " + methodRequest.Name + ".'";

            this.log.Info("Executed method.", () => new {methodRequest.Name});

            byte[] resultEncoded = Encoding.UTF8.GetBytes(result);
            return new MethodResponse(resultEncoded, (int)HttpStatusCode.OK);
        }
        
        private void SetupMethodCallbacksForDevice()
        {
            this.log.Debug("Setting up methods for device.", () => new {
                this.cloudToDeviceMethods.Count,
                deviceId
            });

            //walk this list and add a method handler for each method specified
            foreach (var item in this.cloudToDeviceMethods)
            {
                this.log.Debug("Setting up method for device.", () => new {
                    item.Key,
                    deviceId
                });
                this.client.SetMethodHandlerAsync(item.Key, MethodExecution, null).Wait();

                this.log.Debug("Method for device setup successfully.", () => new {
                    item.Key,
                    deviceId
                });
            }
        }
    }
}
