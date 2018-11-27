// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    public class PropertySendException : Exception
    {
        /// <summary>
        /// This exception is thrown when unable to send device twin reported properties
        /// </summary>
        public PropertySendException(string message) : base(message)
        {
        }

        public PropertySendException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}