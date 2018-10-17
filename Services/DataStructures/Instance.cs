// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures
{
    public interface IInstance
    {
        // Return True if the instance has been initialized
        bool IsInitialized { get; }

        // Mark the initialization complete
        void InitComplete();

        // Fail with exception if the instance has already been initialized
        void InitOnce(
            [CallerMemberName] string callerName = "", 
            [CallerFilePath] string filePath = "", 
            [CallerLineNumber] int lineNumber = 0);

        // Fail with exception if the instance has not been initialized
        void InitRequired(
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);
    }

    /// <summary>
    /// Helper used to require object initialization.
    /// Many objects are not ready to be used after the ctor is done, and either require
    /// async logic that cannot be executed in the ctor, or some context parameters
    /// that are not automatically injected. This class helps to ensure that these classes
    /// are used as intended, i.e. initialized after the ctor, before being used.
    /// </summary>
    public class Instance : IInstance
    {
        private readonly ILogger log;

        // Return True if the instance has been initialized
        public bool IsInitialized { get; private set; }

        public Instance(ILogger log)
        {
            this.log = log;
            this.IsInitialized = false;
        }

        // Mark the initialization as complete
        public void InitComplete()
        {
            this.IsInitialized = true;
        }

        // Fail with exception if the instance has already been initialized
        public void InitOnce([CallerMemberName]
            string callerName = "", [CallerFilePath]
            string filePath = "", [CallerLineNumber]
            int lineNumber = 0)
        {
            if (!this.IsInitialized) return;

            this.log.Error("The instance has already been initialized", () => new { callerName, filePath, lineNumber });
            throw new ApplicationException($"Multiple initializations attempt ({filePath}:{lineNumber})");
        }

        // Fail with exception if the instance has not been initialized
        public void InitRequired([CallerMemberName]
            string callerName = "", [CallerFilePath]
            string filePath = "", [CallerLineNumber]
            int lineNumber = 0)
        {
            if (this.IsInitialized) return;

            this.log.Error("The instance has not been initialized", () => new { callerName, filePath, lineNumber });
            throw new ApplicationException($"Call from an instance not yet initialized ({filePath}:{lineNumber})");
        }
    }
}
