// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Xunit.Abstractions;

namespace Services.Test.helpers
{
    /// <summary>
    /// Use this logger to capture diagnostics data emitted by the
    /// system under test (aka target)
    /// </summary>
    public class TargetLogger : ILogger
    {
        private readonly ITestOutputHelper testLogger;

        public TargetLogger(ITestOutputHelper testLogger)
        {
            this.testLogger = testLogger;
        }

        public LogLevel LogLevel { get; }
        public bool DebugIsEnabled { get; }
        public bool InfoIsEnabled { get; }

        // The following 5 methods allow to log a message, capturing the location where the log is generated
        // Use "Write()" to write the message regardless of the log level, e.g. at startup

        public void Write(string message, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Write: " + message);
        }

        public void Debug(string message, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Debug: " + message);
        }

        public void Info(string message, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Info: " + message);
        }

        public void Warn(string message, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Warn: " + message);
        }

        public void Error(string message, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Error: " + message);
        }

        // The following 5 methods allow to log data, without a message, capturing the location
        // where the log is generated. Use "Write()" to write the message regardless of the log level, e.g. at startup

        public void Write(Func<object> data, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            var message = Serialization.Serialize(data.Invoke());
            this.testLogger.WriteLine(Time() + "Target Write: " + message);
        }

        public void Debug(Func<object> data, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            var message = Serialization.Serialize(data.Invoke());
            this.testLogger.WriteLine(Time() + "Target Debug: " + message);
        }

        public void Info(Func<object> data, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            var message = Serialization.Serialize(data.Invoke());
            this.testLogger.WriteLine(Time() + "Target Info: " + message);
        }

        public void Warn(Func<object> data, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            var message = Serialization.Serialize(data.Invoke());
            this.testLogger.WriteLine(Time() + "Target Warn: " + message);
        }

        public void Error(Func<object> data, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            var message = Serialization.Serialize(data.Invoke());
            this.testLogger.WriteLine(Time() + "Target Error: " + message);
        }

        // The following 5 methods allow to log a message and some data, capturing the location where the log is generated
        // Use "Write()" to write the message regardless of the log level, e.g. at startup

        public void Write(string message, Func<object> data, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Write: " + message);
        }

        public void Debug(string message, Func<object> data, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Debug: " + message);
        }

        public void Info(string message, Func<object> data, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Info: " + message);
        }

        public void Warn(string message, Func<object> data, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Warn: " + message);
        }

        public void Error(string message, Func<object> data, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Error: " + message);
        }

        // The following 5 methods allow to log a message and an exception, capturing the location where the log is generated
        // Use the methods with <Func<object> data> to pass more data than just the exception
        // Use "Write()" to write the message regardless of the log level, e.g. at startup

        public void Write(string message, Exception e, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Write: " + message);
        }

        public void Debug(string message, Exception e, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Debug: " + message);
        }

        public void Info(string message, Exception e, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Info: " + message);
        }

        public void Warn(string message, Exception e, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Warn: " + message);
        }

        public void Error(string message, Exception e, string callerName = "", string filePath = "", int lineNumber = 0)
        {
            this.testLogger.WriteLine(Time() + "Target Error: " + message);
        }

        public string FormatDate(long time)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(time).ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        public void LogToFile(string filename, string text)
        {
            this.testLogger.WriteLine(Time() + "Target LogToFile: " + text);
        }

        public void Write(string message, Action context)
        {
            this.testLogger.WriteLine(Time() + "Target Write: " + message);
        }

        public void Debug(string message, Action context)
        {
            this.testLogger.WriteLine(Time() + "Target Debug: " + message);
        }

        public void Warn(string message, Action context)
        {
            this.testLogger.WriteLine(Time() + "Target Warn: " + message);
        }

        public void Info(string message, Action context)
        {
            this.testLogger.WriteLine(Time() + "Target Info: " + message);
        }

        public void Error(string message, Action context)
        {
            this.testLogger.WriteLine(Time() + "Target Error: " + message);
        }

        public void Write(string message, Func<object> context)
        {
            this.testLogger.WriteLine(Time() + "Target Write: " + message + "; "
                                      + Serialization.Serialize(context.Invoke()));
        }

        public void Debug(string message, Func<object> context)
        {
            this.testLogger.WriteLine(Time() + "Target Debug: " + message + "; "
                                      + Serialization.Serialize(context.Invoke()));
        }

        public void Info(string message, Func<object> context)
        {
            this.testLogger.WriteLine(Time() + "Target Info: " + message + "; "
                                      + Serialization.Serialize(context.Invoke()));
        }

        public void Warn(string message, Func<object> context)
        {
            this.testLogger.WriteLine(Time() + "Target Warn: " + message + "; "
                                      + Serialization.Serialize(context.Invoke()));
        }

        public void Error(string message, Func<object> context)
        {
            this.testLogger.WriteLine(Time() + "Target Error: " + message + "; "
                                      + Serialization.Serialize(context.Invoke()));
        }

        private static string Time()
        {
            return DateTimeOffset.UtcNow.ToString("[HH:mm:ss.fff] ");
        }
    }
}
