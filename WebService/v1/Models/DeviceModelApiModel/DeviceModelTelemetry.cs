// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers;
using Newtonsoft.Json;
using System;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel
{
    public class DeviceModelTelemetry
    {
        [JsonProperty(PropertyName = "Interval")]
        public string Interval { get; set; }

        [JsonProperty(PropertyName = "MessageTemplate")]
        public string MessageTemplate { get; set; }

        [JsonProperty(PropertyName = "MessageSchema")]
        public DeviceModelTelemetryMessageSchema MessageSchema { get; set; }

        public DeviceModelTelemetry()
        {
            this.Interval = "00:00:00";
            this.MessageTemplate = string.Empty;
            this.MessageSchema = new DeviceModelTelemetryMessageSchema();
        }

        // Map service model to API model
        public static DeviceModelTelemetry FromServiceModel(DeviceModelMessage value)
        {
            if (value == null) return null;

            var result = new DeviceModelTelemetry
            {
                Interval = value.Interval.ToString("c"),
                MessageTemplate = value.MessageTemplate,
                MessageSchema = DeviceModelTelemetryMessageSchema.FromServiceModel(value.MessageSchema)
            };

            return result;
        }

        // Map API model to service model
        public static DeviceModelMessage ToServiceModel(DeviceModelTelemetry value)
        {
            if (value == null) return null;

            var result = new DeviceModelMessage
            {
                Interval = TimeSpan.Parse(value.Interval),
                MessageTemplate = value.MessageTemplate,
                MessageSchema = DeviceModelTelemetryMessageSchema.ToServiceModel(value.MessageSchema)
            };

            return result;
        }

        public void ValidateInputRequest(ILogger log)
        {
            const string NO_INTERVAL = "Device model telemetry must contains a valid interval";
            const string NO_MESSAGE_TEMPLATE = "Device model telemetry must contains a valid message template";
            const string NO_MESSAGE_SCHEMA = "Device model telemetry must contains a valid message schema";

            try
            {
                IntervalHelper.ValidateInterval(this.Interval);
            }
            catch (InvalidIntervalException exception)
            {
                log.Error(NO_INTERVAL, () => new { deviceModelTelemetry = this, exception });
                throw new BadRequestException(NO_INTERVAL);
            }

            if (string.IsNullOrEmpty(this.MessageTemplate))
            {
                log.Error(NO_MESSAGE_TEMPLATE, () => new { deviceModelTelemetry = this });
                throw new BadRequestException(NO_MESSAGE_TEMPLATE);
            }

            if (this.MessageSchema.IsEmpty())
            {
                log.Error(NO_MESSAGE_SCHEMA, () => new { deviceModelTelemetry = this });
                throw new BadRequestException(NO_MESSAGE_SCHEMA);
            }
        }
    }
}
