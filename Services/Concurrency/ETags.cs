// Copyright (c) Microsoft. All rights reserved.

/* CODE TEMPORARILY COMMENTED OUT

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public static class ETags
    {
        // A simple string generator, until we have a real storage, used for
        // optimistic concurrency on data stored on files
        public static string NewETag()
        {
            var v1 = Guid.NewGuid().ToString("N");
            var v2 = DateTime.UtcNow.Ticks % 1000000;
            return v1.Substring(0, 8) + v2;
        }
    }
}
*/