// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    public class ResourceOutOfDateException : Exception
    {
        public ResourceOutOfDateException():base()
        {
        }

        public ResourceOutOfDateException(string message):base(message)
        {
        }

        public ResourceOutOfDateException(string message, Exception innerException):base(message,innerException)
        {
        }
    }
}
