// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics
{
    // Note: singleton class
    public class Logger : ILogger
    {
        private readonly string processId;
        private readonly LogLevel priorityThreshold;
        private readonly bool logProcessId;
        private readonly string dateFormat;
        private readonly object fileLock;

        // Optional list of sources to ignore, e.g. methods for which log statements will be discarded
        private readonly ImmutableHashSet<string> blackList;

        // Optional list of sources to include, ignoring everything else
        private readonly ImmutableHashSet<string> whiteList;

        // Flag set to True when using a white list, to know that most of the logs are discarded
        private readonly bool onlyWhiteListed;

        public Logger(string processId, ILoggingConfig config)
        {
            this.processId = processId;
            this.priorityThreshold = config.LogLevel;
            this.logProcessId = config.LogProcessId;
            this.dateFormat = config.DateFormat;
            this.fileLock = new object();

            this.blackList = config.BlackList
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s.ToLowerInvariant()).ToImmutableHashSet();

            this.whiteList = config.WhiteList
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s.ToLowerInvariant()).ToImmutableHashSet();
            this.onlyWhiteListed = this.whiteList.Count > 0;
        }

        public LogLevel LogLevel => this.priorityThreshold;

        public bool DebugIsEnabled => this.priorityThreshold <= LogLevel.Debug;

        public bool InfoIsEnabled => this.priorityThreshold <= LogLevel.Info;

        public string FormatDate(long time)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(time).ToString(this.dateFormat);
        }

        // The following 5 methods allow to log a message, capturing the location where the log is generated
        // Use "Write()" to write the message regardless of the log level, e.g. at startup

        public void Write(string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            this.Write(LogLevel.Always, message, callerName, filePath, lineNumber);
        }

        public void Debug(string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Debug) return;
            this.Write(LogLevel.Debug, message, callerName, filePath, lineNumber);
        }

        public void Info(string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Info) return;
            this.Write(LogLevel.Info, message, callerName, filePath, lineNumber);
        }

        public void Warn(string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Info) return;
            this.Write(LogLevel.Warn, message, callerName, filePath, lineNumber);
        }

        public void Error(string message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Error) return;
            this.Write(LogLevel.Error, message, callerName, filePath, lineNumber);
        }

        // The following 5 methods allow to log data, without a message, capturing the location
        // where the log is generated. Use "Write()" to write the message regardless of the log level, e.g. at startup

        public void Write(Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            var message = Serialization.Serialize(data.Invoke());
            this.Write(LogLevel.Always, message, callerName, filePath, lineNumber);
        }

        public void Debug(Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Debug) return;
            var message = Serialization.Serialize(data.Invoke());
            this.Write(LogLevel.Debug, message, callerName, filePath, lineNumber);
        }

        public void Info(Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Info) return;
            var message = Serialization.Serialize(data.Invoke());
            this.Write(LogLevel.Info, message, callerName, filePath, lineNumber);
        }

        public void Warn(Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Warn) return;
            var message = Serialization.Serialize(data.Invoke());
            this.Write(LogLevel.Warn, message, callerName, filePath, lineNumber);
        }

        public void Error(Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Error) return;
            var message = Serialization.Serialize(data.Invoke());
            this.Write(LogLevel.Error, message, callerName, filePath, lineNumber);
        }

        // The following 5 methods allow to log a message and some data, capturing the location where the log is generated
        // Use "Write()" to write the message regardless of the log level, e.g. at startup

        public void Write(string message,
            Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(data.Invoke());

            this.Write(LogLevel.Always, message, callerName, filePath, lineNumber);
        }

        public void Debug(string message,
            Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Debug) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(data.Invoke());

            this.Write(LogLevel.Debug, message, callerName, filePath, lineNumber);
        }

        public void Info(string message,
            Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Info) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(data.Invoke());

            this.Write(LogLevel.Info, message, callerName, filePath, lineNumber);
        }

        public void Warn(string message,
            Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Warn) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(data.Invoke());

            this.Write(LogLevel.Warn, message, callerName, filePath, lineNumber);
        }

        public void Error(string message,
            Func<object> data,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Error) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.Serialize(data.Invoke());

            this.Write(LogLevel.Error, message, callerName, filePath, lineNumber);
        }

        // The following 5 methods allow to log a message and an exception, capturing the location where the log is generated
        // Use the methods with <Func<object> data> to pass more data than just the exception
        // Use "Write()" to write the message regardless of the log level, e.g. at startup

        public void Write(string message,
            Exception e,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.SerializeException(e);

            this.Write(LogLevel.Always, message, callerName, filePath, lineNumber);
        }

        public void Debug(string message,
            Exception e,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Debug) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.SerializeException(e);

            this.Write(LogLevel.Debug, message, callerName, filePath, lineNumber);
        }

        public void Info(string message,
            Exception e,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Info) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.SerializeException(e);

            this.Write(LogLevel.Info, message, callerName, filePath, lineNumber);
        }

        public void Warn(string message,
            Exception e,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Warn) return;

            if (!string.IsNullOrEmpty(message)) message += ", ";
            message += Serialization.SerializeException(e);

            this.Write(LogLevel.Warn, message, callerName, filePath, lineNumber);
        }

        public void Error(string message,
            Exception e,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (this.priorityThreshold > LogLevel.Error) return;

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
        private void Write(LogLevel msgPriority, string text, string methodName, string filePath, int lineNumber)
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

            var wbKey = $"{filePath}:{methodName}".ToLowerInvariant();
            if (this.onlyWhiteListed)
            {
                // Skip non whitelisted sources
                if (!this.whiteList.Contains(wbKey)) return;
            }
            else if (this.blackList.Contains(wbKey))
            {
                // Skip blacklisted sources
                return;
            }

            var methodInfo = $"{filePath}:{lineNumber}:{methodName}";
            var time = DateTimeOffset.UtcNow.ToString(this.dateFormat);
            var lev = msgPriority.ToString().ToUpperInvariant();
            var logEntry = this.logProcessId
                ? $"[{lev}][{time}][{this.processId}][{methodInfo}] {text}"
                : $"[{lev}][{time}][{methodInfo}] {text}";

            if (msgPriority >= this.priorityThreshold)
            {
                Console.WriteLine(logEntry);
            }
        }
    }
}
