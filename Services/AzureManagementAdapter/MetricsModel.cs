// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.AzureManagementAdapter
{
    public class MetricsResponseListModel
    {
        [JsonProperty("responses")]
        public List<MetricResponseModel> Responses { get; set; }
    }

    public class MetricResponseModel
    {
        [JsonProperty("httpStatusCode")]
        public string HttpStatusCode { get; set; }

        [JsonProperty("content")]
        public MetricContentModel Content { get; set; }

        [JsonProperty("contentLength")]
        public string ContentLength { get; set; }
    }

    public class MetricContentModel
    {
        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public List<MetricValueModel> Value { get; set; }

        [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
        public string Code { get; set; }

        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public MetricContentErrorModel Error { get; set; }
    }

    public class MetricContentErrorModel
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class MetricValueModel
    {
        [JsonProperty("data")]
        public List<MetricDataModel> Data { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public MetricValueNameModel Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }
    }

    public class MetricValueNameModel
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("localizedValue")]
        public string LocalizedValue { get; set; }
    }

    public class MetricDataModel
    {
        [JsonProperty("timeStamp")]
        public string TimeStamp { get; set; }

        [JsonProperty("total")]
        public long Total { get; set; }
    }

    public class MetricsRequestListModel
    {
        [JsonProperty(PropertyName = "requests")]
        public List<MetricsRequestModel> Requests { get; set; }

        public MetricsRequestListModel()
        {
            this.Requests = new List<MetricsRequestModel>();
        }
    }

    public class MetricsRequestModel
    {
        [JsonProperty(PropertyName = "httpMethod")]
        public string HttpMethod { get; set; }

        [JsonProperty(PropertyName = "relativeUrl")]
        public string RelativeUrl { get; set; }

        public MetricsRequestModel()
        {
            this.HttpMethod = string.Empty;
            this.RelativeUrl = string.Empty;
        }
    }
}
