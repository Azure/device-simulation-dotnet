// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when unable to send device twin reported properties
    /// </summary>
    public class PropertySendException : CustomException
    {
        public PropertySendException(string message) : base(message)
        {
        }

        public PropertySendException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
