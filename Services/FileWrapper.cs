// Copyright (c) Microsoft. All rights reserved. 

using System.IO;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IFileSystem
    {
        bool Exists(string path);
        string ReadAllText(string path);
    }

    public class FileWrapper : IFileSystem
    {
        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }
    }
}
