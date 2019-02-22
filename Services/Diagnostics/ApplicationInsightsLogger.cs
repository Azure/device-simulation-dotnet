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
    class ApplicationInsightsLogger: IApplicationInsightsLogger
    {
        private readonly IInstance instance;
        private readonly ILoggingConfig loggingConfig;
        private readonly string nodeId;
        private readonly bool enabled;

        private string simulationId;
        private TelemetryClient telemetryClient;

        private DateTime prevDateTime;
        private TimeSpan prevTotalCpuTimeSpan;
        private DateTime currentDateTime;
        private TimeSpan currentTotalCpuTimeSpan;

        public ApplicationInsightsLogger(
            ILoggingConfig loggingConfig,
            IClusterNodes clusterNodes,
            IInstance instance)
        {
            this.loggingConfig = loggingConfig;
            this.enabled = loggingConfig.LocalApplicationInsightsDiagnostics;
            this.nodeId = clusterNodes.GetCurrentNodeId();
            this.instance = instance;
        }

        public void Init()
        {
            this.instance.InitOnce();

            // Record the timestamp at the point this object is initialized. This will be
            // a crude approximation of the process start time, although this will need to 
            // be improved to be more accurate
            this.prevDateTime = DateTime.Now;
            this.currentTotalCpuTimeSpan = Process.GetCurrentProcess().TotalProcessorTime;

            this.telemetryClient = new TelemetryClient
            {
                InstrumentationKey = this.loggingConfig.AppInsightsInstrumentationKey
            };

            this.instance.InitComplete();
        }

        public void WaitingForConnectionTasks(string simulationId, int taskCount)
        {
            this.instance.InitRequired();

            if (!this.enabled) return;

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

            if (!this.enabled) return;

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

            if (!this.enabled) return;

            Dictionary<string, string> properties = new Dictionary<string, string>();

            // Get the current timespan and processor usage time, which will be
            // compared to the previous values from N-1 cycle.
            this.currentDateTime = DateTime.Now;
            this.currentTotalCpuTimeSpan = p.TotalProcessorTime;

            // Approximate CPU usage % as the ratio of the CPU usage over a given timespan,
            // and the actual duration of that timespan, normalized by the number of processors
            // reported:
            //
            //                     CPU milliseconds used
            // CPU utilization % = ---------------------
            //                     duration of timespan
            //                     ---------------------
            //                         # of cores
            double usage = (this.currentTotalCpuTimeSpan.TotalMilliseconds - this.prevTotalCpuTimeSpan.TotalMilliseconds)
                           / this.currentDateTime.Subtract(this.prevDateTime).TotalMilliseconds
                           / Convert.ToDouble(Environment.ProcessorCount);

            // Update the previous values with the values from this cycle.
            this.prevDateTime = this.currentDateTime;
            this.prevTotalCpuTimeSpan = this.currentTotalCpuTimeSpan;

            properties.Add("CPU utilization", usage.ToString());

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
