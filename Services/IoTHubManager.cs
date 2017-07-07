// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IIoTHubManager
    {
        Task<Tuple<bool, string>> PingAsync();
    }

    public class IoTHubManager : IIoTHubManager
    {
        private readonly ILogger log;
        private readonly IHttpClient httpClient;
        private readonly string iothubmanUri;
        private readonly int iothubmanTimeout;

        private class StatusApiModel
        {
            public string Status { get; set; }
        }

        public IoTHubManager(
            IServicesConfig config,
            ILogger logger,
            IHttpClient httpClient)
        {
            this.log = logger;
            this.httpClient = httpClient;
            this.iothubmanTimeout = config.IoTHubManagerApiTimeout;
            this.iothubmanUri = config.IoTHubManagerApiUrl + "/status/";

            this.log.Debug("Devices service instantiated",
                () => new { this.iothubmanUri, this.iothubmanTimeout });
        }

        public async Task<Tuple<bool, string>> PingAsync()
        {
            var request = new HttpRequest();
            request.SetUriFromString(this.iothubmanUri);
            request.Options.Timeout = this.iothubmanTimeout * 1000;
            var response = await this.httpClient.GetAsync(request);

            this.log.Debug("IoT Hub manager response", () => new { response.StatusCode, response.Content });

            switch (response.StatusCode)
            {
                case 0:
                    return new Tuple<bool, string>(false, "Service unreachable");
                case HttpStatusCode.OK:
                    StatusApiModel data = JsonConvert.DeserializeObject<StatusApiModel>(response.Content);
                    bool healthy = data.Status.Substring(0, 2).ToUpperInvariant() == "OK";
                    return new Tuple<bool, string>(healthy, data.Status);
                default:
                    this.log.Error("Unable to fetch IoTHubManager status", () => { });
                    return new Tuple<bool, string>(false, response.StatusCode.ToString());
            }
        }
    }
}
