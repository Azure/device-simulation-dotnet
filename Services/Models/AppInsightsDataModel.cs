// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.Azure.IoTSolutions.Diagnostics.Services.Models
{
    public class AppInsightsDataModel
    {
        public string EventType { get; set; }

        public string SessionId { get; set; }

        public Dictionary<string, string> EventProperties { get; set; }

        public string DeploymentId { get; set; }

        //public static AppInsightsDataModel FromServiceModel(DiagnosticsEventsServiceModel model)
        //{
        //    AppInsightsDataModel result = new AppInsightsDataModel
        //    {
        //        EventProperties = new Dictionary<string, string>(),
        //        EventType = model.EventType,
        //        SessionId = model.SessionId,
        //        DeploymentId = model.DeploymentId
        //    };

        //    if (model.EventProperties != null)
        //    {
        //        foreach (string key in model.EventProperties.Keys)
        //        {
        //            object value = model.EventProperties[key];
        //            if (value != null && value.ToString().Length > 0)
        //            {
        //                result.EventProperties[key] = value.ToString();
        //            }
        //        }
        //    }

        //    if (model.UserProperties != null)
        //    {
        //        foreach (string key in model.UserProperties.Keys)
        //        {
        //            object value = model.UserProperties[key];
        //            if (value != null && value.ToString().Length > 0)
        //            {
        //                result.EventProperties[key] = value.ToString();
        //            }
        //        }
        //    }

        //    return result;
        //}
    }
}
