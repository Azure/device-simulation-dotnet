// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models
{
    public class DeviceModelScriptListModel
    {
        [JsonProperty(PropertyName = "Items")]
        public List<DeviceModelScriptApiModel.DeviceModelScriptApiModel> Items { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DeviceModelScriptList;" + Version.NUMBER },
            { "$uri", "/" + Version.PATH + "/devicemodelscripts" }
        };

        public DeviceModelScriptListModel()
        {
            this.Items = new List<DeviceModelScriptApiModel.DeviceModelScriptApiModel>();
        }

        // Map service model to API model
        public static DeviceModelScriptListModel FromServiceModel(IEnumerable<DataFile> value)
        {
            if (value == null) return null;

            return new DeviceModelScriptListModel
            {
                Items = value.Select(DeviceModelScriptApiModel.DeviceModelScriptApiModel.FromServiceModel)
                    .Where(x => x != null)
                    .ToList()
            };
        }
    }
}
