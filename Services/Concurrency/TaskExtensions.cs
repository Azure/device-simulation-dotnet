// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public static class TaskExtensions
    {
        public static T AsyncResult<T>(this Task<T> t, int timeout)
        {
            if (t.Status == TaskStatus.Created)
            {
                try
                {
                    t.Start();
                }
                catch (Exception)
                {
                    // ignore
                }
            }

            t.Wait(timeout);
            return t.Result;
        }
    }
}
