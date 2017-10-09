// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public interface ITimer
    {
        ITimer Start();
        ITimer StartIn(TimeSpan delay);
        void Stop();
        ITimer Setup(Action<object> action, object context, TimeSpan frequency);
        ITimer Setup(Action<object> action, object context, int frequency);
    }

    public class Timer : ITimer
    {
        private readonly ILogger log;

        private System.Threading.Timer timer;
        private int frequency;
        private TimeSpan delay;

        public Timer(ILogger logger)
        {
            this.log = logger;
            this.frequency = 0;
        }

        public ITimer Setup(Action<object> action, object context, TimeSpan frequency)
        {
            return this.Setup(action, context, (int) frequency.TotalMilliseconds);
        }

        public ITimer Setup(Action<object> action, object context, int frequency)
        {
            this.frequency = frequency;
            this.timer = new System.Threading.Timer(
                new TimerCallback(action),
                context,
                Timeout.Infinite,
                this.frequency);
            return this;
        }

        public ITimer Start()
        {
            return this.StartIn(TimeSpan.Zero);
        }

        public ITimer StartIn(TimeSpan delay)
        {
            if (this.timer == null)
            {
                this.log.Error("The timer is not initialized", () => { });
                throw new TimerNotInitializedException();
            }

            this.delay = delay;

            this.timer.Change((int)this.delay.TotalMilliseconds, this.frequency);
            return this;
        }

        public void Stop()
        {
            try
            {
                this.timer?.Change(Timeout.Infinite, Timeout.Infinite);
                this.timer?.Dispose();
            }
            catch (ObjectDisposedException e)
            {
                this.log.Info("The timer was already disposed.", () => new { e });
            }
        }

        public void PauseTimer()
        {
            this.timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void UnPauseTimer()
        {
            this.StartIn(this.delay);
        }
    }
}
