// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Models.DeviceModelApiModel
{
    public class DeviceModelSimulationScript
    {
        [JsonProperty(PropertyName = "Type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "Path")]
        public string Path { get; set; }

        [JsonProperty(PropertyName = "Params", NullValueHandling = NullValueHandling.Ignore)]
        public object Params { get; set; }

        public DeviceModelSimulationScript()
        {
            this.Type = string.Empty;
            this.Path = string.Empty;
            this.Params = null;
        }

        // Map API model to service model
        public Script ToServiceModel()
        {
            if (this.IsEmpty()) return null;

            return new Script
            {
                Type = !string.IsNullOrEmpty(this.Type) ? this.Type : null,
                Path = !string.IsNullOrEmpty(this.Path) ? this.Path : null,
                Params = this.Params
            };
        }

        // Map service model to API model
        public static DeviceModelSimulationScript FromServiceModel(Script value)
        {
            if (value == null) return null;

            return new DeviceModelSimulationScript
            {
                Type = string.IsNullOrEmpty(value.Type) ? null : value.Type,
                Path = string.IsNullOrEmpty(value.Path) ? null : value.Path,
                Params = value.Params
            };
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(this.Type)
                   && string.IsNullOrEmpty(this.Path)
                   && this.Params == null;
        }

        public void ValidateInputRequest(ILogger log)
        {
            const string INTERNAL = "internal";
            const string NO_TYPE = "Script must contains a valid type";
            const string NO_PATH = "Script must contains a valid path";

            if (string.IsNullOrEmpty(this.Type))
            {
                log.Error(NO_TYPE, () => new { Script = this });
                throw new BadRequestException(NO_TYPE);
            }

            if (string.IsNullOrEmpty(this.Path))
            {
                log.Error(NO_PATH, () => new { Script = this });
                throw new BadRequestException(NO_PATH);
            }

            if (this.Type == INTERNAL)
            {
                this.ValidateParams(log);
            }
        }

        private void ValidateParams(ILogger log)
        {
            if (this.Params == null)
            {
                this.ThrowInvalidParams(log);
            }
            
            var rootObject = JObject.Parse(this.Params.ToString());
            var values = rootObject.First?.First;

            foreach (var token in rootObject)
            {
                var value = token.Value;
                if (value == null)
                {
                    this.ThrowInvalidParams(log);
                }

                this.CheckProperties(log, value);
            }
        }

        private void CheckProperties(ILogger log, JToken propValue)
        {
            string min = CheckProperty(log, propValue, "Min");
            string max = CheckProperty(log, propValue, "Max");
            CheckProperty(log, propValue, "Step");
            CheckProperty(log, propValue, "Unit");

            if (Int32.TryParse(min, out int minValue) && Int32.TryParse(max, out int maxValue))
            {
                if (minValue >= maxValue)
                {
                    this.ThrowInvalidParams(log);
                }
            }
            else
            {
                this.ThrowInvalidParams(log);
            }
        }

        private string CheckProperty(ILogger log, JToken propValue, string key)
        {
            string value = propValue.SelectToken(key).ToString();
            
            if (string.IsNullOrEmpty(value))
            {
                this.ThrowInvalidParams(log);
            }

            return value;
        }

        private void ThrowInvalidParams(ILogger log)
        {
            const string NO_PARAMS = "Script must contains a valid params";
            log.Error(NO_PARAMS, () => new { Script = this });
            throw new BadRequestException(NO_PARAMS);
        }
    }
}