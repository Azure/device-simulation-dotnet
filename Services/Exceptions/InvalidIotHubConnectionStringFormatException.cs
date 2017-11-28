// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    public class InvalidIotHubConnectionStringFormatException : Exception
    {
        /// <summary>
        /// This exception is thrown when the IoTHub connection string provided
        /// is not properly formatted. The correct format is:
        /// HostName=[hubname];SharedAccessKeyName=[iothubowner or service];SharedAccessKey=[null or valid key]
        /// </summary>
        public InvalidIotHubConnectionStringFormatException() : base()
        {
        }

        public InvalidIotHubConnectionStringFormatException(string message) : base(message)
        {
        }

        public InvalidIotHubConnectionStringFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
