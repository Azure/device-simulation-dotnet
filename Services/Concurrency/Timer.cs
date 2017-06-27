// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public class TimerNotInitializedException : Exception
    {
        public TimerNotInitializedException()
            : base("Timer object not initialized. Call 'Setup()' first.")
        {
        }
    }

    public interface ITimer
    {
        Timer Start();
        Timer Stop();
        Timer Setup(Action<object> action, object context, int frequency);
    }

    public class Timer : ITimer
    {
        private readonly ILogger log;

        private System.Threading.Timer timer;
        private int frequency;

        public delegate void Action(object context);

        public Timer(ILogger logger)
        {
            this.log = logger;
            this.frequency = 0;
        }

        public Timer Setup(Action<object> action, object context, int frequency)
        {
            this.frequency = frequency;
            this.timer = new System.Threading.Timer(
                new TimerCallback(action),
                context,
                Timeout.Infinite,
                this.frequency);
            return this;
        }

        public Timer Start()
        {
            if (this.timer == null)
            {
                this.log.Error("The actor is not initialized", () => { });
                throw new TimerNotInitializedException();
            }

            this.timer.Change(0, this.frequency);
            return this;
        }

        public Timer Stop()
        {
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);
            return this;
        }
    }
}
