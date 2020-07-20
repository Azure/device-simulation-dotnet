// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService
{
    public static class Program
    {
        // Application entry point. This is where we set up the web host (Kestrel)
        // that will handle HTTP requests
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
                var host = new WebHostBuilder() // TODO: Use HostBuilder instead of WebHostBuilder? https://docs.microsoft.com/en-us/aspnet/core/migration/22-to-30?view=aspnetcore-3.1&tabs=visual-studio#kestrel
                    .UseUrls("http://*:" + webServicePort)
                    .UseKestrel(options => { options.AddServerHeader = false; })
                    .UseIISIntegration()
                    .UseStartup<Startup>() // <- ASP.Net Core will call in to Startup to set up the middleware
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
