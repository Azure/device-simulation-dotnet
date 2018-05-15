// Copyright (c) Microsoft. All rights reserved.

/* CODE TEMPORARILY COMMENTED OUT

using System;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public interface ITimer
    {
        void Setup(Action<object> action);
        void Setup(Action<object> action, object context);
        void RunOnce(int? dueTime);
        void RunOnce(double? dueTime);
        void Cancel();
    }

    public class Timer : ITimer
    {
        private readonly ILogger log;
        private bool cancelled = false;
        private System.Threading.Timer timer;

        public Timer(ILogger logger)
        {
            this.log = logger;
        }

        public void Setup(Action<object> action, object context)
        {
            this.timer = new System.Threading.Timer(
                new TimerCallback(action),
                context,
                Timeout.Infinite,
                Timeout.Infinite);
        }

        public void Setup(Action<object> action)
        {
            this.Setup(action, null);
        }

        public void RunOnce(int? dueTime)
        {
            if (!dueTime.HasValue) return;

            if (this.cancelled)
            {
                this.log.Debug("Timer has been cancelled, ignoring call to RunOnce", () => { });
            }

            if (this.timer == null)
            {
                this.log.Error("The timer is not initialized", () => { });
                throw new TimerNotInitializedException();
            }

            // Normalize negative values
            var when = Math.Max(0, dueTime.Value);
            this.timer?.Change(when, Timeout.Infinite);
        }

        public void RunOnce(double? dueTime)
        {
            if (!dueTime.HasValue) return;

            this.RunOnce((int) dueTime.Value);
        }

        public void Cancel()
        {
            try
            {
                this.cancelled = true;
                this.timer?.Change(Timeout.Infinite, Timeout.Infinite);
                this.timer?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                this.log.Debug("The timer object was already disposed", () => { });
            }
        }
    }
}
*/