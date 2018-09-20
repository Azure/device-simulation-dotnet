// Copyright (c) Microsoft. All rights reserved.

using System.Threading;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public interface IThreadWrapper
    {
        void Sleep(int msecs);
    }

    // Simple Thread wrapper to remove static methods and simplify testing
    public class ThreadWrapper: IThreadWrapper
    {
        public void Sleep(int msecs)
        {
            Thread.Sleep(msecs);
        }
    }
}
