// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    public class ConflictingResourceException : Exception
    {
        public ConflictingResourceException():base()
        {
        }

        public ConflictingResourceException(string message):base(message)
        {
        }

        public ConflictingResourceException(string message, Exception innerException):base(message,innerException)
        {
        }
    }
}
