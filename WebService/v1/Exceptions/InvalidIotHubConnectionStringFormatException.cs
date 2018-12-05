﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions
{
    public class InvalidIotHubConnectionStringFormatException : CustomException
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
