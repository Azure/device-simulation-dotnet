// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    public class ResourceIsLockedByAnotherOwnerException : CustomException
    {
        public ResourceIsLockedByAnotherOwnerException() : base()
        {
        }

        public ResourceIsLockedByAnotherOwnerException(string message) : base(message)
        {
        }

        public ResourceIsLockedByAnotherOwnerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}