// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;
using Microsoft.Owin.Hosting;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService
{
    public class Program
    {
        static readonly IConfig config = new Config();

        static void Main(string[] args)
        {
            // TODO: remove workaround and support all versions
            var options = new StartOptions("http://*:" + config.Port + "/" + v1.Version.Name);
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
