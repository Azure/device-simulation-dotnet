// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions
{
    public class InvalidDateFormatException : CustomException
    {
        /// <summary>
        /// This exception is thrown by a controller when a datetime input validation
        /// fails. The client should fix the request before retrying.
        /// </summary>
        public InvalidDateFormatException() : base()
        {
        }

        public InvalidDateFormatException(string message) : base(message)
        {
        }

        public InvalidDateFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
