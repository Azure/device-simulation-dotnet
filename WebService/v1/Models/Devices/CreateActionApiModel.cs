// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Devices
{
    public class CreateActionApiModel
    {
        [JsonProperty(PropertyName = "DeviceId")]
        public string DeviceId { get; set; }

        [JsonProperty(PropertyName = "ModelId")]
        public string ModelId { get; set; }

        public CreateActionApiModel()
        {
            this.DeviceId = null;
            this.ModelId = null;
        }

        public void ValidateInputRequest(ILogger log)
        {
            const string INVALID_DEVICE_NAME = "Device name is invalid";
            const string INVALID_DEVICE_MODEL_ID = "Device model id is invalid";
            
            if (string.IsNullOrEmpty(this.DeviceId))
            {
                log.Error(INVALID_DEVICE_NAME, () => new { device = this });
                throw new BadRequestException(INVALID_DEVICE_NAME);
            }

            if (string.IsNullOrEmpty(this.ModelId))
            {
                log.Error(INVALID_DEVICE_MODEL_ID, () => new { device = this });
                throw new BadRequestException(INVALID_DEVICE_MODEL_ID);
            }
        }
    }
}
