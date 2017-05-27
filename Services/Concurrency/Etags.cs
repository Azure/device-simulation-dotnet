// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public static class Etags
    {
        // A simple string generator, until we have a real storage
        public static string NewEtag()
        {
            var v1 = Guid.NewGuid().ToString().Replace("-", "");
            var v2 = DateTime.UtcNow.Ticks % 1000000;
            return v1.Substring(0, 8) + v2;
        }
    }
}
