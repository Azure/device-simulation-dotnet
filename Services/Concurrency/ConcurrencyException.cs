// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException(string msg)
            : base(msg)
        {
        }
    }
}
