// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.IoTSolutions.Diagnostics.Services.Models;
using System.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    class ApplicationInsightsLogger : IApplicationInsightsLogger
    {
        private readonly IInstance instance;
        private readonly ILoggingConfig loggingConfig;
        private readonly string nodeId;
        private string simulationId;
        private TelemetryClient telemetryClient;

        public ApplicationInsightsLogger(
           ILoggingConfig loggingConfig,
           IClusterNodes clusterNodes,
           IInstance instance)
        {
            this.instance = instance;
            this.loggingConfig = loggingConfig;
            this.nodeId = clusterNodes.GetCurrentNodeId();
            this.instance = instance;
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

        public void AddConnectionStats(string simulationid, string data)
        {
            this.instance.InitRequired();

            Dictionary<string, string> eventProperties = new Dictionary<string, string>();
            eventProperties.Add("ConnectionStats", data);

            // Add process stats
            this.AddProcessStats(ref eventProperties);

            var model = new AppInsightsDataModel
            {
                EventType = "Connection Stats",
                SessionId = simulationId,
                EventProperties = eventProperties,
            };

            this.TrackEvent(model);
        }

        public void LogProcessStats(string simulationId, Process p)
        {
            this.instance.InitRequired();

            Dictionary<string, string> properties = new Dictionary<string, string>();

            // Approximate the CPU usage of this process as the ratio of the CPU time over the
            // time that the process started.
            var procUsageSeconds = p.TotalProcessorTime.TotalSeconds;
            var procTotalSeconds = DateTime.Now.Subtract(p.StartTime).TotalSeconds;
            properties.Add("CPU utilization", (procUsageSeconds / procTotalSeconds).ToString());

            var model = new AppInsightsDataModel
            {
                EventType = "Performance (Simulation Agent)",
                SessionId = simulationId,
                EventProperties = properties,
            };

            this.TrackEvent(model);
        }

        private void AddProcessStats(ref Dictionary<string, string> properties)
        {
            // log how many threads are in use
            Process p = Process.GetCurrentProcess();
            properties.Add("Thread count", p.Threads.Count.ToString());

        }

        private void TrackEvent(AppInsightsDataModel model)
        {
            // Add the node ID to each event
            model.EventProperties.Add("Node ID", this.nodeId);
            // Set the simulation ID before tracking each event
            this.telemetryClient.Context.Session.Id = model.SessionId;
            this.telemetryClient.TrackEvent(model.EventType, model.EventProperties);
        }

    }
}