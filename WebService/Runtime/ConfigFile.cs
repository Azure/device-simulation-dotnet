// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.Runtime
{
    public static class ConfigFile
    {
        public const string DEFAULT = "appsettings.ini";
        private const string DEV_ENV_VAR = "PCS_DEV_APPSETTINGS";

        // If the system has a "PCS_DEV_APPSETTINGS" environment variable, and the
        // value references an existing file, then the app will use this file to pull in settings,
        // before looking into "appsettings.ini". Use this mechanism to override the default
        // configuration with your values during development. Do not use this approach
        // in production. Also, make sure your dev config settings are not checked into
        // the repository or the Docker image.
        public static string GetDevOnlyConfigFile()
        {
            try
            {
                string devConfigFile = Environment.GetEnvironmentVariable(DEV_ENV_VAR);
                if (!string.IsNullOrEmpty(devConfigFile) && File.Exists(devConfigFile))
                {
                    return devConfigFile;
                }
            }
            catch (Exception)
            {
                // no op
            }

            return null;
        }
    }
}
