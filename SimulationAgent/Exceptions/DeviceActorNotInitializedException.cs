// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions
{
    public class DeviceActorNotInitializedException : Exception
    {
        public DeviceActorNotInitializedException()
            : base("DeviceActor object not initialized. Call 'Setup()' first.")
        {
        }
    }
}
