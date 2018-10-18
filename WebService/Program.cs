// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService
{
    public static class Program
    {
        // Application entry point
        public static void Main(string[] args)
        {
            var webServicePort = DependencyResolution.GetConfig().Port;

            try
            {
                /*
                Kestrel is a cross-platform HTTP server based on libuv,
                a cross-platform asynchronous I/O library.
                https://docs.microsoft.com/aspnet/core/fundamentals/servers
                */
                var host = new WebHostBuilder()
                    .UseUrls("http://*:" + webServicePort)
                    .UseKestrel(options => { options.AddServerHeader = false; })
                    .UseIISIntegration()
                    .UseStartup<Startup>()
                    .Build();

                host.Run();
            }
            catch (IOException e)
                when (e.InnerException is AddressInUseException)
            {
                PrintTcpErrorMessage(webServicePort);
                // Required to kill the process
                throw;
            }
        }

        private static void PrintTcpErrorMessage(int port)
        {
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Fatal error, the port " + port + " required by the web service is not available.");
            Console.ResetColor();
        }
    }
}
