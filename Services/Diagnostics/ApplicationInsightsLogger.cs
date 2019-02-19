// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.IoTSolutions.Diagnostics.Services.Models;
using System.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    class ApplicationInsightsLogger: IApplicationInsightsLogger
    {
        private readonly IInstance instance;
        private readonly ILoggingConfig loggingConfig;

        private string simulationId;
        private TelemetryClient telemetryClient;

        public ApplicationInsightsLogger(
            ILoggingConfig loggingConfig,
            IInstance instance)
        {
            this.instance = instance;
            this.loggingConfig = loggingConfig;
        }

        public void Init()
        {
            this.instance.InitOnce();

            this.telemetryClient = new TelemetryClient
            {
                InstrumentationKey = this.loggingConfig.AppInsightsInstrumentationKey
            };

            this.instance.InitComplete();
        }

        public void WaitingForConnectionTasks(string simulationId, int taskCount)
        {
            this.instance.InitRequired();

            Dictionary<string, string> eventProperties = new Dictionary<string, string>();
            eventProperties.Add("Pending connection task count", taskCount.ToString());

            // Add process stats
            this.AddProcessStats(ref eventProperties);

            var model = new AppInsightsDataModel
            {
                EventType = "Pending device-connection tasks",
                SessionId = simulationId,
                EventProperties = eventProperties,
            };

            this.TrackEvent(model);

        }

        public void DeviceConnectionLoopCompleted(string simulationid, long durationMsecs)
        {
            this.instance.InitRequired();

            Dictionary<string, string> eventProperties = new Dictionary<string, string>();
            eventProperties.Add("Duration", durationMsecs.ToString());

            // Add process stats
            this.AddProcessStats(ref eventProperties);

            var model = new AppInsightsDataModel
            {
                EventType = "Device-connection loop",
                SessionId = simulationId,
                EventProperties = eventProperties,
            };

            this.TrackEvent(model);
        }

        public void LogProcessStats(string simulationId, Process p)
        {
            this.instance.InitRequired();

            throw new NotImplementedException();
        }

        private void AddProcessStats(ref Dictionary<string, string> properties)
        {
            // log how many threads are in use
            Process p = Process.GetCurrentProcess();
            properties.Add("Thread count", p.Threads.Count.ToString());

        }

        private void TrackEvent(AppInsightsDataModel model)
        {
            // Set the simulation ID before tracking each event
            this.telemetryClient.Context.Session.Id = model.SessionId;
            this.telemetryClient.TrackEvent(model.EventType, model.EventProperties);
        }

    }
}
