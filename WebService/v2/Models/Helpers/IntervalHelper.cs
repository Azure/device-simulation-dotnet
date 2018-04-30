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
        public static void ValidateInterval(string interval)
        {
            const string NOT_VALID = "Invalid interval";
            const string EMPTY_INTERVAL = "Empty interval";
            bool hasError = false;

            if (string.IsNullOrEmpty(interval))
            {
                // Log happens upstream
                throw new InvalidIntervalException(EMPTY_INTERVAL);
            }
         
            try
            {
                TimeSpan t = TimeSpan.Parse(interval);

                if (t == TimeSpan.Zero)
                {
                    hasError = true;
                }
            }
            catch (Exception)
            {
                hasError = true;
            }

            if (hasError)
            {
                // Log happens upstream
                throw new InvalidIntervalException(NOT_VALID);
            }
        }
    }
}
