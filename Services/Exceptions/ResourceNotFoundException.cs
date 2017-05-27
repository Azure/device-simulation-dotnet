// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    public class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException():base()
        {
        }

        public ResourceNotFoundException(string message):base(message)
        {
        }

        public ResourceNotFoundException(string message, Exception innerException):base(message,innerException)
        {
        }
    }
}
