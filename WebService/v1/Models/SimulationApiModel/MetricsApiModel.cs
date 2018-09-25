// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.AzureManagementAdapter;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.SimulationApiModel
{
    public class MetricsRequestApiModel
    {
        [JsonProperty(PropertyName = "httpMethod")]
        public string HttpMethod { get; set; }

        [JsonProperty(PropertyName = "relativeUrl")]
        public string RelativeUrl { get; set; }

        public MetricsRequestApiModel()
        {
            this.HttpMethod = string.Empty;
            this.RelativeUrl = string.Empty;
        }

        public MetricsRequestModel ToServiceModel()
        {
            return new MetricsRequestModel()
            {
                HttpMethod = this.HttpMethod,
                RelativeUrl = this.RelativeUrl
            };
        }
    }

    public class MetricsRequestsApiModel
    {
        [JsonProperty(PropertyName = "requests")]
        public List<MetricsRequestApiModel> Requests { get; set; }

        public MetricsRequestsApiModel()
        {
            this.Requests = new List<MetricsRequestApiModel>();
        }

        public MetricsRequestListModel ToServiceModel()
        {
            var result = new MetricsRequestListModel();

            foreach(var request in this.Requests)
            {
                result.Requests.Add(request.ToServiceModel());
            }

            return result;
        }
    }
}
