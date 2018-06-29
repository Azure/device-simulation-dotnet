// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties
{
    /// <summary>
    /// Add twin information to the new device twin
    /// </summary>
    public class SetDeviceTag : IDevicePropertiesLogic
    {
        private readonly IDevices devices;
        private readonly ILogger log;
        private string deviceId;
        private IDevicePropertiesActor context;

        public SetDeviceTag(IDevices devices, ILogger logger)
        {
            this.log = logger;
            this.devices = devices;
        }

        public void Setup(IDevicePropertiesActor context, string deviceId)
        {
            this.context = context;
            this.deviceId = deviceId;
        }

        public void Run()
        {
            this.log.Debug("Adding tag to device twin...", () => new { this.deviceId });

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                /*
                 * ContinueWith allows to easily manage the exceptions here, with the ability to change
                 * the code to synchronous or asynchronous, via TaskContinuationOptions.
                 * 
                 * Once the code successfully handle all the scenarios, with good throughput and low CPU usage
                 * we should see if the async/await syntax performs similarly/better.
                 */
                this.devices
                    .AddTagAsync(this.deviceId)
                    .ContinueWith(t =>
                    {
                        var timeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - now;

                        if (t.IsCanceled)
                        {
                            this.log.Warn("The set device tag task has been cancelled", () => new { this.deviceId, t.Exception });
                        }
                        else if (t.IsFaulted)
                        {
                            var exception = t.Exception.InnerExceptions.FirstOrDefault();
                            this.log.Error(GetLogErrorMessage(exception), () => new { this.deviceId, exception });
                            this.context.HandleEvent(DevicePropertiesActor.ActorEvents.DeviceTaggingFailed);
                        }
                        else if (t.IsCompleted)
                        {
                            this.log.Debug("Device tag set", () => new { this.deviceId, timeSpent });
                            this.context.HandleEvent(DevicePropertiesActor.ActorEvents.DeviceTagged);
                        }
                    },
                    TaskContinuationOptions.ExecuteSynchronously);
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while tagging the device twin", () => new { this.deviceId, e });
                this.context.HandleEvent(DevicePropertiesActor.ActorEvents.DeviceTaggingFailed);
            }
        }

        private static string GetLogErrorMessage(Exception e)
        {
            return e != null ? "Set device tag error: " + e.Message : string.Empty;
        }
    }
}
