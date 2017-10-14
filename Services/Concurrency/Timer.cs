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
        void Pause();
        void Resume();
        void Stop();
        ITimer Setup(Action<object> action, TimeSpan frequency, object context = null);
        ITimer Setup(Action<object> action, int frequency, object context = null);
    }

    public class Timer : ITimer
    {
        private readonly ILogger log;

        private System.Threading.Timer timer;
        private int frequency;
        private bool stopped;

        public Timer(ILogger logger)
        {
            this.log = logger;
            this.frequency = 0;
            this.stopped = true;
        }

        public ITimer Setup(Action<object> action, TimeSpan frequency, object context = null)
        {
            return this.Setup(action, (int) frequency.TotalMilliseconds, context);
        }

        public ITimer Setup(Action<object> action, int frequency, object context = null)
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
            this.stopped = false;
            return this.StartIn(TimeSpan.Zero);
        }

        public ITimer StartIn(TimeSpan delay)
        {
            if (this.timer == null)
            {
                this.log.Error("The timer is not initialized", () => { });
                throw new TimerNotInitializedException();
            }

            this.stopped = false;
            this.timer.Change((int) delay.TotalMilliseconds, this.frequency);
            return this;
        }

        public void Pause()
        {
            if (!this.stopped)
            {
                this.timer?.Change(Timeout.Infinite, this.frequency);
            }
        }

        public void Resume()
        {
            if (!this.stopped)
            {
                this.timer?.Change(this.frequency, this.frequency);
            }
        }

        public void Stop()
        {
            try
            {
                this.stopped = true;
                this.timer?.Change(Timeout.Infinite, Timeout.Infinite);
                this.timer?.Dispose();
            }
            catch (ObjectDisposedException e)
            {
                this.log.Info("The timer was already disposed.", () => new { e });
            }
        }
    }
}
