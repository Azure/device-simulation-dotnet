// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    // Note: singleton class
    public class Logger : ILogger
    {
        private readonly string processId;
        private readonly LogLevel logLevel;
        private readonly bool logProcessId;
        private readonly string dateFormat;
        private readonly object fileLock;

        // private readonly bool bwEnabled;
        // private readonly bool blackListEnabled;
        // private readonly bool whiteListEnabled;
        // private readonly bool bwPrefixUsed;
        // private readonly HashSet<string> blackList;
        // private readonly HashSet<string> whiteList;
        // private readonly string bwListPrefix;
        // private readonly int bwListPrefixLength;

        public Logger(string processId) :
            this(processId, new LoggingConfig())
        {
        }

        public Logger(string processId, ILoggingConfig config)
        {
            this.processId = processId;
            this.logLevel = config.LogLevel;
            this.logProcessId = config.LogProcessId;
            this.dateFormat = config.DateFormat;

            // this.blackList = config.BlackList;
            // this.whiteList = config.WhiteList;
            //
            // this.blackListEnabled = this.blackList.Count > 0;
            // this.whiteListEnabled = this.whiteList.Count > 0;
            // this.bwEnabled = this.blackListEnabled || this.whiteListEnabled;
            //
            // this.bwPrefixUsed = !string.IsNullOrEmpty(config.BwListPrefix);
            // this.bwListPrefix = config.BwListPrefix;
            // this.bwListPrefixLength = config.BwListPrefix.Length;

            this.fileLock = new object();
        }

        public LogLevel LogLevel => this.logLevel;

        public bool DebugIsEnabled => this.logLevel <= LogLevel.Debug;

        public bool InfoIsEnabled => this.logLevel <= LogLevel.Info;

        public string FormatDate(long time)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(this.dateFormat);
        }

        // The following 5 methods allow to log a message, capturing the location where the log is generated
        // Use "Write()" to write the message regardless of the log level, e.g. at startup

        public void Write(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            this.Write(LogLevel.Always, message, callerName, filePath, lineNumber);
        }

        public void Debug(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (this.logLevel > LogLevel.Debug) return;
            this.Write(LogLevel.Debug, message, callerName, filePath, lineNumber);
        }

        public void Info(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (this.logLevel > LogLevel.Info) return;
            this.Write(LogLevel.Info, message, callerName, filePath, lineNumber);
        }

        public void Warn(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (this.logLevel > LogLevel.Warn) return;
            this.Write(LogLevel.Warn, message, callerName, filePath, lineNumber);
        }

        public void Error(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (this.logLevel > LogLevel.Error) return;
            this.Write(LogLevel.Error, message, callerName, filePath, lineNumber);
        }

        // The following 5 methods allow to log a message and some data, capturing the location where the log is generated
        // Use "Write()" to write the message regardless of the log level, e.g. at startup

        public void Write(string message, Func<object> data, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(data.Invoke());

            this.Write(LogLevel.Always, message, callerName, filePath, lineNumber);
        }

        public void Debug(string message, Func<object> data, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (this.logLevel > LogLevel.Debug) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(data.Invoke());

            this.Write(LogLevel.Debug, message, callerName, filePath, lineNumber);
        }

        public void Info(string message, Func<object> data, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (this.logLevel > LogLevel.Info) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(data.Invoke());

            this.Write(LogLevel.Info, message, callerName, filePath, lineNumber);
        }

        public void Warn(string message, Func<object> data, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (this.logLevel > LogLevel.Warn) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(data.Invoke());

            this.Write(LogLevel.Warn, message, callerName, filePath, lineNumber);
        }

        public void Error(string message, Func<object> data, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (this.logLevel > LogLevel.Error) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(data.Invoke());

            this.Write(LogLevel.Error, message, callerName, filePath, lineNumber);
        }

        // The following 5 methods allow to log a message and an exception, capturing the location where the log is generated
        // Use the methods with <Func<object> data> to pass more data than just the exception
        // Use "Write()" to write the message regardless of the log level, e.g. at startup

        public void Write(string message, Exception e, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.SerializeException(e);

            this.Write(LogLevel.Always, message, callerName, filePath, lineNumber);
        }

        public void Debug(string message, Exception e, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (this.logLevel > LogLevel.Debug) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.SerializeException(e);

            this.Write(LogLevel.Debug, message, callerName, filePath, lineNumber);
        }

        public void Info(string message, Exception e, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (this.logLevel > LogLevel.Info) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.SerializeException(e);

            this.Write(LogLevel.Info, message, callerName, filePath, lineNumber);
        }

        public void Warn(string message, Exception e, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (this.logLevel > LogLevel.Warn) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.SerializeException(e);

            this.Write(LogLevel.Warn, message, callerName, filePath, lineNumber);
        }

        public void Error(string message, Exception e, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (this.logLevel > LogLevel.Error) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.SerializeException(e);

            this.Write(LogLevel.Error, message, callerName, filePath, lineNumber);
        }

        public void LogToFile(string filename, string text)
        {
            // Without the lock, some logs would be lost due to contentions
            lock (this.fileLock)
            {
                File.AppendAllText(filename, text);
            }
        }

        /// <summary>
        /// Log the message and information about the context, cleaning up
        /// and shortening the class name and method name (e.g. removing
        /// symbols specific to .NET internal implementation)
        /// </summary>
        private void Write(LogLevel level, string text, string methodName, string filePath, int lineNumber)
        {
            // Linux folder separator
            var pos = filePath.LastIndexOf('/');
            if (pos != -1)
            {
                filePath = filePath.Substring(pos + 1);
            }

            // Windows folder separator
            pos = filePath.LastIndexOf('\\');
            if (pos != -1)
            {
                filePath = filePath.Substring(pos + 1);
            }

            var methodInfo = $"{filePath}:{lineNumber}:{methodName}";

            // TODO: fix for matching filenames instead of class names
            // // Check blacklisted and whitelisted classes and methods
            // if (this.bwEnabled)
            // {
            //     var bwClass = classname;
            //     if (this.bwPrefixUsed && bwClass.StartsWith(this.bwListPrefix))
            //     {
            //         bwClass = bwClass.Substring(this.bwListPrefixLength);
            //     }
            //
            //     if (this.blackListEnabled
            //         && (this.blackList.Contains(bwClass + "." + methodname)
            //             || this.blackList.Contains(bwClass + ".*")))
            //     {
            //         return;
            //     }
            //
            //     if (this.whiteListEnabled
            //         && !this.whiteList.Contains(bwClass + "." + methodname)
            //         && !this.whiteList.Contains(bwClass + ".*"))
            //     {
            //         return;
            //     }
            // }

            var time = DateTimeOffset.UtcNow.ToString(this.dateFormat);
            var lev = level.ToString().ToUpperInvariant();
            var logEntry = this.logProcessId
                ? $"[{lev}][{time}][{this.processId}][{methodInfo}] {text}"
                : $"[{lev}][{time}][{methodInfo}] {text}";

            Console.WriteLine(logEntry);
        }
    }
}
