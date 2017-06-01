// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions
{
    public class BadRequestException : Exception
    {
        /// <summary>
        /// This exception is thrown by a controller when the input validation
        /// fails. The client should fix the request before retrying.
        /// </summary>
        public BadRequestException() : base()
        {
        }

        public BadRequestException(string message) : base(message)
        {
        }

        public BadRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
