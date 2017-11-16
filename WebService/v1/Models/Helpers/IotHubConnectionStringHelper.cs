// Copyright (c) Microsoft. All rights reserved.
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers
{
    public class IotHubConnectionStringHelper
    {

        /// <summary>
        /// Validates that the IoTHub Connection String is valid then
        /// removes the sensitive key data to be stored and returns the
        /// IoTHub Connection String with and empty string for the SharedAccessKey
        /// 
        /// TODO Encryption for key & storage in documentDb instead of file
        /// TODO Investigate conversion to SecureString
        /// 
        /// </summary>
        /// <param name="connString"></param>
        public static string RemoveAndStoreKey(string connString)
        {
            // find key
            var key = GetKeyFromConnString(connString);

            // store in local file
            WriteKeyToFile(key);

            // redact key from connection string
            return connString.Replace(key, "");
        }

        private static string GetKeyFromConnString(string connString)
        {
            var match = Regex.Match(connString,
                @"^HostName=(?<hostName>.*);SharedAccessKeyName=(?<keyName>.*);SharedAccessKey=(?<key>.*);$");

            if (!match.Success)
            {
                var message = "Invalid connection string for IoTHub";
                throw new InvalidInputException(message);
            }

            return match.Groups["key"].Value;
        }

        private static void WriteKeyToFile(string key)
        {
            string path = @"custom_iothub_key.txt";
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
