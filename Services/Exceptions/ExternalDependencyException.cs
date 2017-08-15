// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions
{
    /// <summary>
    /// This exception is thrown when the service failed to communicate with
    /// an external dependency, either because the dependency is unavailable
    /// or because it is failing with a 'retriable' error (e.g. timeout).
    ///
    /// When used for HTTP requests, a request error is retriable if:
    /// * the request timed out, or
    /// * the status code is one of 404, 408, 429
    /// </summary>
    public class ExternalDependencyException : Exception
    {
        public ExternalDependencyException() : base()
        {
        }

        public ExternalDependencyException(string message) : base(message)
        {
        }

        public ExternalDependencyException(Exception innerException)
            : base("An unexpected error happened while using an external dependency.", innerException)
        {
        }

        public ExternalDependencyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
