// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    public class TotalDeviceCountQuotaExceededException : Exception
    {
        // This exception is thrown when it's not possible to register a device due to quota limits
        public TotalDeviceCountQuotaExceededException(string message) : base(message)
        {
        }

        public TotalDeviceCountQuotaExceededException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}