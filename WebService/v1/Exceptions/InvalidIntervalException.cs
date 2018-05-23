// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions
{
    public class InvalidIntervalException : Exception
    {
        /// <summary>
        /// This exception is thrown by a controller when an interval input validation
        /// fails. The client should fix the request before retrying.
        /// </summary>
        public InvalidIntervalException() : base()
        {
        }

        public InvalidIntervalException(string message) : base(message)
        {
        }

        public InvalidIntervalException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
