// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage
{
    public class ResourceIsLockedException : Exception
    {
        public ResourceIsLockedException() : base()
        {
        }

        public ResourceIsLockedException(string message) : base(message)
        {
        }

        public ResourceIsLockedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}