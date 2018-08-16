// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    public class AddDeviceApiModel
    {
        [JsonProperty(PropertyName = "DeviceId")]
        public string DeviceId { get; set; }

        [JsonProperty(PropertyName = "ModelId")]
        public string ModelId { get; set; }

        public AddDeviceApiModel()
        {
            this.DeviceId = null;
            this.ModelId = null;
        }

        public async Task ValidateInputRequest(ILogger log)
        {
            const string INVALID_DEVICE_NAME = "Device name is invalid";
            const string INVALID_DEVICE_MODELID = "Device model id is invalid";
            
            if (string.IsNullOrEmpty(this.DeviceId))
            {
                log.Error(INVALID_DEVICE_NAME, () => new { device = this });
                throw new BadRequestException(INVALID_DEVICE_NAME);
            }

            if (string.IsNullOrEmpty(this.ModelId))
            {
                log.Error(INVALID_DEVICE_MODELID, () => new { device = this });
                throw new BadRequestException(INVALID_DEVICE_MODELID);
            }
        }
    }
}
