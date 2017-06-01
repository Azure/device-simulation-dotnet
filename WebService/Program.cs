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
                Console.WriteLine("Web service started, process ID: " + Uptime.ProcessId);
                Console.WriteLine($"[{Uptime.ProcessId}] Listening at http://*:" + config.Port);
                Console.WriteLine($"[{Uptime.ProcessId}] Health check: http://127.0.0.1:" + config.Port + "/" + v1.Version.Path + "/status");

                // Production mode: keep the service alive until killed
                if (args.Length > 0 && args[0] == "--background")
                {
                    while (true) Console.ReadLine();
                }

                // Development mode: keep the service alive until Enter is pressed
                Console.WriteLine("Press [Enter] to quit...");
                Console.ReadLine();
            }
        }
    }
}
