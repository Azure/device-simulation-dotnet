// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    public interface ILogger
    {
        LogLevel LogLevel { get; }

        string FormatDate(long time);

        bool DebugIsEnabled { get; }
        bool InfoIsEnabled { get; }

        // The following 5 methods allow to log a message, capturing the location where the log is generated
        // Use "Write()" to write the message regardless of the log level, e.g. at startup

        void Write(string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Debug(string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Info(string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Warn(string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Error(string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        // The following 5 methods allow to log data, without a message, capturing the location
        // where the log is generated. Use "Write()" to write the message regardless of the log level, e.g. at startup

        void Write(Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Debug(Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Info(Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Warn(Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Error(Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        // The following 5 methods allow to log a message and some data, capturing the location where the log is generated
        // Use "Write()" to write the message regardless of the log level, e.g. at startup

        void Write(string message, 
            Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Debug(string message, 
            Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Info(string message, 
            Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Warn(string message, 
            Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Error(string message, 
            Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        // The following 5 methods allow to log a message and an exception, capturing the location where the log is generated
        // Use the methods with <Func<object> data> to pass more data than just the exception
        // Use "Write()" to write the message regardless of the log level, e.g. at startup

        void Write(string message, 
            Exception e,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Debug(string message, 
            Exception e,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Info(string message, 
            Exception e,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Warn(string message, 
            Exception e,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void Error(string message, 
            Exception e,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        void LogToFile(string filename, string text);
    }
}
