// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    public interface ILogger
    {
        LogLevel LogLevel { get; }

        string FormatDate(long time);

        bool DebugIsEnabled { get; }
        bool InfoIsEnabled { get; }

        // The following 5 methods allow to log a message, capturing the context
        // (i.e. the method where the log message is generated)

        // Use "Write()" to write the message regardless of the log level, e.g. at startup
        void Write(string message, Action context);

        void Debug(string message, Action context);
        void Info(string message, Action context);
        void Warn(string message, Action context);
        void Error(string message, Action context);

        // The following 5 methods allow to log a message and some data,
        // capturing the context (i.e. the method where the log message is generated)

        // Use "Write()" to write the message regardless of the log level, e.g. at startup
        void Write(string message, Func<object> context);
        
        void Debug(string message, Func<object> context);
        void Info(string message, Func<object> context);
        void Warn(string message, Func<object> context);
        void Error(string message, Func<object> context);

        void LogToFile(string filename, string text);
    }
}