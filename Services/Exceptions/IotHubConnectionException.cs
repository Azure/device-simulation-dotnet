﻿// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when a connection to the IoTHub with the
    /// provided connection string fails.
    /// </summary>
    public class IotHubConnectionException : CustomException
    {
        public IotHubConnectionException() : base()
        {
        }

        public IotHubConnectionException(string message) : base(message)
        {
        }

        public IotHubConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
