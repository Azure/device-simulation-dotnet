// Copyright (c) Microsoft. All rights reserved.

using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers
{
    public class IotHubConnectionStringManager
    {
        private const string USE_LOCAL_IOTHUB = "pre-provisioned";
        private const string CONNECTION_STRING_FILE_PATH = @"custom_iothub_key.txt";

        /// <summary>
        /// Validates that the IoTHub Connection String is valid, stores the full
        /// string with key in a local file, then removes the sensitive key data and
        /// returns the IoTHub Connection String with and empty string for the SharedAccessKey
        /// 
        /// TODO Encryption for key & storage in documentDb instead of file
        ///      and investigate conversion to SecureString
        ///      https://github.com/Azure/device-simulation-dotnet/issues/129
        /// </summary>
        /// <param name="iotHubConnectionString"></param>
        public static string StoreAndRedact(string iotHubConnectionString)
        {
            // check if environment variable should be used
            if (iotHubConnectionString != USE_LOCAL_IOTHUB)
            {
                UseLocalIotHub();
                return USE_LOCAL_IOTHUB;
            }

            // find key and check that connection string is valid.
            var key = GetKeyFromConnString(iotHubConnectionString);

            // store default value or full connection string with key in local file
            WriteToFile(iotHubConnectionString, CONNECTION_STRING_FILE_PATH);

            // redact key from connection string and return
            return iotHubConnectionString.Replace(key, "");
        }

        /// <summary>
        /// If simulation uses the pre-provisioned IoT Hub for the service,
        /// remove sensitive hub information that is no longer needed
        /// </summary>
        public static void UseLocalIotHub()
        {
            // delete custom IoT Hub string if local hub is being used
            File.Delete(CONNECTION_STRING_FILE_PATH);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iotHubConnectionString"></param>
        /// <returns></returns>
        private static string GetKeyFromConnString(string iotHubConnectionString)
        {
            var match = Regex.Match(iotHubConnectionString,
                @"^HostName=(?<hostName>.*);SharedAccessKeyName=(?<keyName>.*);SharedAccessKey=(?<key>.*)$");

            if (!match.Success)
            {
                var message = "Invalid connection string for IoTHub";
                throw new InvalidInputException(message);
            }

            return match.Groups["key"].Value;
        }

        private static void WriteToFile(string key, string path)
        {
            if (!File.Exists(path))
            {
                // Create a file to write to. 
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(key);
                }
            }
            else
            {
                File.WriteAllText(path, key);
            }
        }
    }
}
