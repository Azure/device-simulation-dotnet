// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDevices
    {
        Task<Device> GetOrCreateAsync(string deviceId);
        Task<Device> GetAsync(string deviceId);
        Task<Device> CreateAsync(Device device);
        IDeviceClient GetClient(Device device, IoTHubProtocol protocol);
    }

    public class Devices : IDevices
    {
        private readonly ILogger log;
        private readonly IHttpClient httpClient;
        private readonly string iothubmanUri;
        private readonly int iothubmanTimeout;

        public Devices(
            IServicesConfig config,
            ILogger logger,
            IHttpClient httpClient)
        {
            this.log = logger;
            this.httpClient = httpClient;
            this.iothubmanTimeout = config.IoTHubManagerApiTimeout;
            this.iothubmanUri = config.IoTHubManagerApiUrl + "/devices/";

            this.log.Debug("Devices service instantiated",
                () => new { this.iothubmanUri, this.iothubmanTimeout });
        }

        public async Task<Device> GetOrCreateAsync(string deviceId)
        {
            try
            {
                return await this.GetAsync(deviceId);
            }
            catch (ResourceNotFoundException)
            {
                var device = new Device
                {
                    Id = deviceId,
                    Enabled = true
                };

                return await this.CreateAsync(device);
            }
        }

        public async Task<Device> GetAsync(string deviceId)
        {
            this.log.Info("Getting device", () => new { deviceId });

            var request = new HttpRequest();
            request.SetUriFromString(this.iothubmanUri + WebUtility.UrlDecode(deviceId));
            request.Options.Timeout = this.iothubmanTimeout * 1000;
            var response = await this.httpClient.GetAsync(request);

            this.log.Debug("IoT Hub manager response", () => new { deviceId, response.StatusCode, response.Content });

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return JsonConvert.DeserializeObject<Device>(response.Content);
                case HttpStatusCode.NotFound:
                    this.log.Warn("The device doesn't exist", () => new { deviceId });
                    throw new ResourceNotFoundException("The device doesn't exist.");
                default:
                    if (response.IsRetriableError)
                    {
                        this.log.Warn("Retriable error: unable to fetch the list of IoT devices", () => new { deviceId });
                        throw new ExternalDependencyException(
                            "Unable to fetch the list of IoT devices. Status code " + response.StatusCode + ". Please retry.");
                    }

                    this.log.Error("Unable to fetch the list of IoT devices", () => new { deviceId });
                    throw new ExternalDependencyException(
                        "Unable to fetch the list of IoT devices. Status code " + response.StatusCode + ".");
            }
        }

        public async Task<Device> CreateAsync(Device device)
        {
            this.log.Info("Creating device", () => new { device.Id });

            var request = new HttpRequest();
            request.SetUriFromString(this.iothubmanUri);
            request.Options.Timeout = this.iothubmanTimeout * 1000;
            request.SetContent(device);
            var response = await this.httpClient.PostAsync(request);

            this.log.Debug("IoT Hub manager response",
                () => new { device.Id, response.StatusCode, response.Content });

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return JsonConvert.DeserializeObject<Device>(response.Content);
                default:
                    if (response.IsRetriableError)
                    {
                        this.log.Warn("Retriable error: unable to create the IoT device",
                            () => new { device.Id });

                        throw new ExternalDependencyException(
                            "Unable to create the IoT device. Status code " + response.StatusCode + ". Please retry.");
                    }

                    this.log.Error("Unable to create the IoT device",
                        () => new { device.Id });

                    throw new ExternalDependencyException(
                        "Fatal error: unable to create the IoT device. Status code " + response.StatusCode);
            }
        }

        public IDeviceClient GetClient(Device device, IoTHubProtocol protocol)
        {
            var connectionString = $"HostName={device.IoTHubHostName};DeviceId={device.Id};SharedAccessKey={device.AuthPrimaryKey}";

            Azure.Devices.Client.DeviceClient sdkClient;
            switch (protocol)
            {
                case IoTHubProtocol.AMQP:
                    this.log.Info("Creating AMQP device client",
                        () => new { device.Id, device.IoTHubHostName });

                    sdkClient = Azure.Devices.Client.DeviceClient.CreateFromConnectionString(connectionString, TransportType.Amqp_Tcp_Only);
                    break;

                case IoTHubProtocol.MQTT:
                    this.log.Info("Creating MQTT device client",
                        () => new { device.Id, device.IoTHubHostName });

                    sdkClient = Azure.Devices.Client.DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt_Tcp_Only);
                    break;

                case IoTHubProtocol.HTTP:
                    this.log.Info("Creating HTTP device client",
                        () => new { device.Id, device.IoTHubHostName });

                    sdkClient = Azure.Devices.Client.DeviceClient.CreateFromConnectionString(connectionString, TransportType.Http1);
                    break;

                default:
                    this.log.Error("Unable to create a client for the given protocol",
                        () => new { protocol });

                    throw new Exception($"Unable to create a client for the given protocol ({protocol})");
            }

            return new DeviceClient(sdkClient, protocol, this.log);
        }
    }
}
