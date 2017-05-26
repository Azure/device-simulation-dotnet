// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;
using Microsoft.Owin.Hosting;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService
{
    /// <summary>Application entry point</summary>
    public class Program
    {
        static readonly IConfig config = new Config();

        static void Main(string[] args)
        {
            var options = new StartOptions("http://*:" + config.Port);
            using (WebApp.Start<Startup>(options))
            {
                Console.WriteLine("Server listening at http://*:" + config.Port);
                Console.WriteLine("Health check: http://127.0.0.1:" + config.Port + "/v1/status");
                Console.WriteLine("Press [Enter] to quit...");
                Console.ReadLine();
            }
        }
    }
}
