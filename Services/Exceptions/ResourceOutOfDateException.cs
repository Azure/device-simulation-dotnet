// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when a client attempts to update a resource
    /// providing the wrong ETag value. The client should retrieve the
    /// resource again, to have the new ETag, and retry.
    /// </summary>
    public class ResourceOutOfDateException : Exception
    {
        public ResourceOutOfDateException() : base()
        {
        }

        public ResourceOutOfDateException(string message) : base(message)
        {
        }

        public ResourceOutOfDateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
