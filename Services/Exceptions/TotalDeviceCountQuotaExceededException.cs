// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    // This exception is thrown when it's not possible to register a device due to quota limits
    public class TotalDeviceCountQuotaExceededException : CustomException
    {
        public TotalDeviceCountQuotaExceededException(string message) : base(message)
        {
        }

        public TotalDeviceCountQuotaExceededException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}