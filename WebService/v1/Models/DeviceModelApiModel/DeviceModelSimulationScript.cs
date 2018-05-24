// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel
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
            const string NO_TYPE = "Simulation script type cannot be empty";
            const string NO_PATH = "Simulation script path cannot be empty";

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

            if (this.Type == ScriptInterpreter.INTERNAL_SCRIPT)
            {
                this.ValidateInternalScriptParams(log);
            }
        }

        /* Current Scripts structure:
        {
            "Type": "internal",
            "Path": "math.random.withinrange",
            "Params": {
                "temperature": {
                    "Min"  : "1",
                    "Max"  : "12",
                    "Step" : 1,
                    "Unit" : "C"
                },
                "pressure": {
                    "Min"  : "10",
                    "Max"  : "90",
                    "Step" : 1,
                    "Unit" : "psi"
                },
            }
        }
        Min/Max might not be needed by future scripts.
        TODO: the actural function (E.g: 'math.random.withinrange') should provide a validation method.
        */
        private void ValidateInternalScriptParams(ILogger log)
        {
            if (this.Params == null)
            {
                this.ThrowInvalidParamsError(log);
            }

            var rootObject = JObject.Parse(this.Params.ToString());

            foreach (var token in rootObject)
            {
                var value = token.Value;
                if (value == null)
                {
                    this.ThrowInvalidParamsError(log);
                }
            }
        }

        private void ThrowInvalidParamsError(ILogger log)
        {
            const string NO_PARAMS = "Script must contains valid parameters";
            log.Error(NO_PARAMS, () => new { Script = this });
            throw new BadRequestException(NO_PARAMS);
        }
    }
}
