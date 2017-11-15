// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions
{
    public class InvalidSimulationSchedulingException : Exception
    {
        /// <summary>
        /// This exception is thrown by a controller when the input validation
        /// fails. The client should fix the request before retrying.
        /// </summary>
        public InvalidSimulationSchedulingException() : base()
        {
        }

        public InvalidSimulationSchedulingException(string message) : base(message)
        {
        }

        public InvalidSimulationSchedulingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
