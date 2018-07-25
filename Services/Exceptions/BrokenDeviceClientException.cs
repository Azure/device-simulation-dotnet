// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    public class BrokenDeviceClientException : Exception
    {
        /// <summary>
        /// This exception is thrown when a device client is broken and should be recreated
        /// </summary>
        public BrokenDeviceClientException(string message) : base(message)
        {
        }

        public BrokenDeviceClientException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
