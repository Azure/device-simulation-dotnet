using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v2.Models.Helpers
{
    public static class IntervalHelper
    {
        public static void ValidateInterval(string Interval)
        {
            const string NOT_VALID = "Invalid interval";
            const string EMPTY_INTERVAL = "Empty interval";

            if (string.IsNullOrEmpty(Interval))
            {
                // Log happens upstream
                throw new InvalidIntervalException(EMPTY_INTERVAL);
            }
            else
            {
                try
                {
                    TimeSpan t = TimeSpan.Parse(Interval);

                    if (t == TimeSpan.Zero)
                    {
                        // Log happens upstream
                        throw new InvalidIntervalException(NOT_VALID);
                    }
                }
                catch (Exception)
                {
                    // Log happens upstream
                    throw new InvalidIntervalException(NOT_VALID);
                }
            }
        }
    }
}
