// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.Exceptions
{
    public class DeviceActorAlreadyInitializedException : Exception
    {
        public DeviceActorAlreadyInitializedException()
            : base("DeviceActor object already initialized. Call 'Start()'.")
        {
        }
    }
}
