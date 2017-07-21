// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new Config(new ConfigData());

            /*
            Kestrel is a cross-platform HTTP server based on libuv, a
            cross-platform asynchronous I/O library.
            https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers
            */
            var host = new WebHostBuilder()
                .UseUrls("http://*:" + config.Port)
                .UseKestrel(options => { options.AddServerHeader = false; })
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
