// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers;
using Newtonsoft.Json;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel
{
    public class DeviceModelSimulation
    {
        [JsonProperty(PropertyName = "InitialState")]
        public Dictionary<string, object> InitialState { get; set; }

        [JsonProperty(PropertyName = "Interval", NullValueHandling = NullValueHandling.Ignore)]
        public string Interval { get; set; }

        [JsonProperty(PropertyName = "Scripts")]
        public List<DeviceModelSimulationScript> Scripts { get; set; }

        private const string NO_INTERVAL = "Device model simulation state must contains a valid interval";
        private const string NO_SCRIPTS = "Device model simulation state must contains a valid script";

        public DeviceModelSimulation()
        {
            this.InitialState = new Dictionary<string, object>();
            this.Interval = null;
            this.Scripts = new List<DeviceModelSimulationScript>();
        }

        // Map API model to service model
        public static StateSimulation ToServiceModel(DeviceModelSimulation model)
        {
            if (model == null) return null;

            return new StateSimulation
            {
                InitialState = model.InitialState,
                Interval = TimeSpan.Parse(model.Interval),
                Scripts = model.Scripts.Select(script => script.ToServiceModel()).Where(x => x != null).ToList()
            };
        }

        // Map service model to API model
        public static DeviceModelSimulation FromServiceModel(StateSimulation value)
        {
            if (value == null) return null;

            return new DeviceModelSimulation
            {
                InitialState = value.InitialState,
                Interval = value.Interval.ToString("c"),
                Scripts = value.Scripts.Select(DeviceModelSimulationScript.FromServiceModel).Where(x => x != null).ToList()
            };
        }

        // Validate device model simulation state:
        // Required fields: Interval and Scripts
        public void ValidateInputRequest(ILogger log)
        {
            try
            {
                IntervalHelper.ValidateInterval(this.Interval);
            }
            catch (InvalidIntervalException exception)
            {
                log.Error(NO_INTERVAL, () => new { deviceModelSimulation = this, exception });
                throw new BadRequestException(NO_INTERVAL);
            }

            if (this.Scripts == null || this.Scripts.Count == 0)
            {
                log.Error(NO_SCRIPTS, () => new { deviceModelSimulation = this });
                throw new BadRequestException(NO_SCRIPTS);
            }
            else
            {
                foreach (var script in this.Scripts)
                {
                    script.ValidateInputRequest(log);
                }
            }
        }

        public List<string> ValidationHelper()
        {
            List<string> errors = new List<string>();

            try
            {
                IntervalHelper.ValidateInterval(this.Interval);
            }
            catch (InvalidIntervalException)
            {
                errors.Add(NO_INTERVAL);
            }

            // Allow user to have an empty list of scripts which may upload later
            if (this.Scripts == null)
            {
                errors.Add(NO_SCRIPTS);
            }

            return errors;
        }
    }
}
