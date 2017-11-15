// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions
{
    public class InvalidSimulationSchedulingException : Exception
    {
        /// <summary>
        /// This exception is thrown by a controller when a client is trying to set
        /// a simulation start and end time using and invalid period, e.g. start > end.
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
