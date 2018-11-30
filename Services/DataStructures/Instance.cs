// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures
{
    public interface IInstance
    {
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
    /// Helper used to require object initialization, not used in Production.
    /// Many objects are not ready to be used after the ctor is done, and either require
    /// async logic that cannot be executed in the ctor, or some context parameters
    /// that are not automatically injected. This class helps to ensure that these classes
    /// are used as intended, i.e. initialized after the ctor, before being used.
    /// </summary>
    public class Instance : IInstance
    {
        private readonly ILogger log;

        private bool isInitialized;

        public Instance(ILogger log)
        {
            this.log = log;
            this.isInitialized = false;
        }

        // Mark the initialization as complete
        public void InitComplete()
        {
            this.isInitialized = true;
        }

        // Fail with exception if the instance has already been initialized
        public void InitOnce(
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!this.isInitialized) return;

            this.log.Error("The instance has already been initialized", () => new { callerName, filePath, lineNumber });
            throw new ApplicationException($"Multiple initializations attempt ({filePath}:{lineNumber})");
        }

        // Fail with exception if the instance has not been initialized
        public void InitRequired(
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.isInitialized) return;

            this.log.Error("The instance has not been initialized", () => new { callerName, filePath, lineNumber });
            throw new ApplicationException($"Call from an instance not yet initialized ({filePath}:{lineNumber})");
        }
    }

    // Singleton shim used to save memory in Production
    // TODO: replace this with DEBUG symbol where IInstance is used
    public class InstanceShim : IInstance
    {
        public void InitComplete()
        {
        }

        public void InitOnce(string callerName = "", string filePath = "", int lineNumber = 0)
        {
        }

        public void InitRequired(string callerName = "", string filePath = "", int lineNumber = 0)
        {
        }
    }
}
