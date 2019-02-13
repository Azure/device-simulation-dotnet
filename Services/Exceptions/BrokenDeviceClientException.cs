// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when a device client is broken and should be recreated
    /// </summary>
    public class BrokenDeviceClientException : CustomException
    {
        public BrokenDeviceClientException(string message) : base(message)
        {
        }

        public BrokenDeviceClientException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
