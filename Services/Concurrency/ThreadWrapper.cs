// Copyright (c) Microsoft. All rights reserved.

using System.Threading;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public interface IThreadWrapper
    {
        void Sleep(int msecs);
    }

    /// <summary>
    /// Simple Thread wrapper to remove static methods and simplify testing
    /// </summary>
    public class ThreadWrapper: IThreadWrapper
    {
        public void Sleep(int msecs)
        {
            Thread.Sleep(msecs);
        }
    }
}
