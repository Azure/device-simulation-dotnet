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
    public interface IDeviceMethods
    {
        Task<MethodResponse> MethodExecution(MethodRequest methodRequest, object userContext);
    }
    
    public class DeviceMethods : IDeviceMethods
    {
        private const string DateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";

        private readonly Azure.Devices.Client.DeviceClient client;
        private readonly ILogger log;
        private readonly IoTHubProtocol protocol;

        public DeviceMethods(
            Azure.Devices.Client.DeviceClient client,
            IoTHubProtocol protocol,
            ILogger logger)
        {
            this.client = client;
            this.protocol = protocol;
            this.log = logger;
        }

        public IoTHubProtocol Protocol { get { return this.protocol; } }

        public async Task<MethodResponse> MethodExecution(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine();
            Console.WriteLine("\t{0}", methodRequest.DataAsJson);
            Console.WriteLine("\nI'm a simulated device and am executing a method: {0}", methodRequest.Name);

            string result = "'Someone called my method " + methodRequest.Name + " and I did something.  I am a simulated device.'";

            return new MethodResponse(Encoding.UTF8.GetBytes(result), 200);

        }



    }
}
